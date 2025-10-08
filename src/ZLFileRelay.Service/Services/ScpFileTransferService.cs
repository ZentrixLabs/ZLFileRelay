using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZLFileRelay.Core.Interfaces;
using ZLFileRelay.Core.Models;
using ZLFileRelay.Core.Services;

namespace ZLFileRelay.Service.Services
{
    /// <summary>
    /// SCP-based file transfer service implementation using Windows built-in SSH client
    /// </summary>
    public class ScpFileTransferService : IFileTransferService
    {
        private readonly ILogger<ScpFileTransferService> _logger;
        private readonly ZLFileRelayConfiguration _config;
        private readonly RetryPolicy _retryPolicy;
        private readonly IDiskSpaceChecker _diskSpaceChecker;

        public ScpFileTransferService(
            ILogger<ScpFileTransferService> logger,
            ZLFileRelayConfiguration config,
            RetryPolicy retryPolicy,
            IDiskSpaceChecker diskSpaceChecker)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
            _diskSpaceChecker = diskSpaceChecker ?? throw new ArgumentNullException(nameof(diskSpaceChecker));
        }

        public string GetTransferMethod() => "SSH/SCP";

        public async Task<TransferResult> TransferFileAsync(string sourceFilePath, string? destinationPath = null, 
            CancellationToken cancellationToken = default)
        {
            var result = new TransferResult
            {
                SourcePath = sourceFilePath,
                FileName = Path.GetFileName(sourceFilePath),
                StartTime = DateTime.Now,
                TransferMethod = GetTransferMethod()
            };

            try
            {
                await _retryPolicy.ExecuteWithRetryAsync(async () =>
                {
                    await TransferFileInternal(sourceFilePath, destinationPath, result);
                    return true;
                }, $"ScpTransfer({Path.GetFileName(sourceFilePath)})", cancellationToken);

                result.Success = true;
                result.EndTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.ErrorDetails = ex.ToString();
                result.EndTime = DateTime.Now;
                _logger.LogError(ex, "Failed to transfer file: {SourcePath}", sourceFilePath);
            }

            return result;
        }

        public Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var sshArgs = BuildSshCommand("echo 'connection test'");
                var processInfo = new ProcessStartInfo
                {
                    FileName = "ssh.exe",
                    Arguments = sshArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();

                var timeout = _config.Transfer.Ssh.ConnectionTimeout;
                bool completed = process.WaitForExit(timeout * 1000);

                if (!completed)
                {
                    process.Kill();
                    return Task.FromResult(false);
                }

                return Task.FromResult(process.ExitCode == 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection test failed");
                return Task.FromResult(false);
            }
        }

        public async Task<bool> VerifyTransferAsync(string sourceFilePath, string destinationPath, 
            CancellationToken cancellationToken = default)
        {
            if (!_config.Service.VerifyTransfer)
                return true;

            try
            {
                FileInfo sourceInfo = new FileInfo(sourceFilePath);
                long remoteFileSize = await GetRemoteFileSize(destinationPath);

                if (remoteFileSize == -1)
                {
                    _logger.LogError("Could not verify remote file size: {RemotePath}", destinationPath);
                    return false;
                }

                if (sourceInfo.Length != remoteFileSize)
                {
                    _logger.LogError("File size mismatch: Source={SourceSize}, Remote={RemoteSize}",
                        sourceInfo.Length, remoteFileSize);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during file verification");
                return false;
            }
        }

        private async Task TransferFileInternal(string sourceFile, string? customDestination, TransferResult result)
        {
            // Validate source file
            PathValidator.ValidateFile(sourceFile, 
                _config.Security.AllowExecutableFiles,
                _config.Security.AllowHiddenFiles,
                _config.Security.MaxUploadSizeBytes);

            // Determine destination path
            string remotePath = customDestination ?? GetDefaultRemotePath(sourceFile);
            result.DestinationPath = remotePath;

            var fileInfo = new FileInfo(sourceFile);
            result.FileSize = fileInfo.Length;

            _logger.LogInformation("SCP transferring: {FileName} ({FileSize} bytes) to {RemotePath}",
                result.FileName, result.FileSize, remotePath);

            // Validate SSH key exists
            if (!File.Exists(_config.Transfer.Ssh.PrivateKeyPath))
            {
                throw new FileNotFoundException($"SSH private key not found: {_config.Transfer.Ssh.PrivateKeyPath}");
            }

            // Ensure remote directory exists
            await EnsureRemoteDirectory(remotePath);

            // Handle file name conflicts if file exists
            string finalRemotePath = await ResolveRemotePathConflict(remotePath);
            result.DestinationPath = finalRemotePath;

            // Transfer the file
            bool transferSuccess = await ExecuteScpTransfer(sourceFile, finalRemotePath);

            if (!transferSuccess)
            {
                throw new InvalidOperationException($"SCP transfer failed for {Path.GetFileName(sourceFile)}");
            }

            _logger.LogInformation("SCP transfer completed successfully to: {RemotePath}", finalRemotePath);

            // Verify the transfer
            result.Verified = await VerifyTransferAsync(sourceFile, finalRemotePath);

            // Delete source file if configured
            if (_config.Service.DeleteAfterTransfer && result.Verified)
            {
                try
                {
                    File.Delete(sourceFile);
                    _logger.LogInformation("Deleted source file after successful transfer: {FileName}", result.FileName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete source file: {FileName}", result.FileName);
                }
            }
        }

        private string GetDefaultRemotePath(string sourceFile)
        {
            string relativePath = GetRelativePath(_config.Service.WatchDirectory, sourceFile);
            return Path.Combine(_config.Transfer.Ssh.DestinationPath, relativePath).Replace('\\', '/');
        }

        private async Task EnsureRemoteDirectory(string remotePath)
        {
            try
            {
                string remoteDirectory = Path.GetDirectoryName(remotePath)?.Replace('\\', '/') ?? "/";
                var sshArgs = BuildSshCommand($"mkdir -p \"{remoteDirectory}\"");

                var processInfo = new ProcessStartInfo
                {
                    FileName = "ssh.exe",
                    Arguments = sshArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();

                var timeout = _config.Transfer.Ssh.ConnectionTimeout;
                bool completed = process.WaitForExit(timeout * 1000);

                if (!completed)
                {
                    process.Kill();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to ensure remote directory: {RemotePath}", remotePath);
            }
        }

        private async Task<string> ResolveRemotePathConflict(string remotePath)
        {
            bool fileExists = await CheckRemoteFileExists(remotePath);

            if (!fileExists)
                return remotePath;

            var conflictResolution = _config.Service.ConflictResolution?.ToLowerInvariant() ?? "append";

            switch (conflictResolution)
            {
                case "overwrite":
                    _logger.LogInformation("Overwriting existing remote file: {RemotePath}", remotePath);
                    return remotePath;

                case "skip":
                    throw new InvalidOperationException($"File already exists: {remotePath}");

                case "append":
                default:
                    string directory = Path.GetDirectoryName(remotePath)?.Replace('\\', '/') ?? "/";
                    string fileName = Path.GetFileNameWithoutExtension(remotePath);
                    string extension = Path.GetExtension(remotePath);
                    string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                    string newFileName = $"{fileName}_{timestamp}{extension}";
                    string newRemotePath = Path.Combine(directory, newFileName).Replace('\\', '/');

                    _logger.LogInformation("Renaming file to avoid conflict: {Original} -> {New}", 
                        remotePath, newRemotePath);
                    return newRemotePath;
            }
        }

        private Task<bool> CheckRemoteFileExists(string remotePath)
        {
            try
            {
                var sshArgs = BuildSshCommand($"test -f \"{remotePath}\"");

                var processInfo = new ProcessStartInfo
                {
                    FileName = "ssh.exe",
                    Arguments = sshArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();

                var timeout = _config.Transfer.Ssh.ConnectionTimeout;
                bool completed = process.WaitForExit(timeout * 1000);

                if (!completed)
                {
                    process.Kill();
                    return Task.FromResult(false);
                }

                return Task.FromResult(process.ExitCode == 0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check if remote file exists: {RemotePath}", remotePath);
                return Task.FromResult(false);
            }
        }

        private async Task<bool> ExecuteScpTransfer(string localPath, string remotePath)
        {
            try
            {
                var scpArgs = BuildScpCommand(localPath, remotePath);

                var processInfo = new ProcessStartInfo
                {
                    FileName = "scp.exe",
                    Arguments = scpArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                // SECURITY FIX (MEDIUM-1): Sanitize command line to avoid exposing private key paths
                _logger.LogDebug("Executing SCP command: scp.exe {Arguments}", 
                    LoggingHelper.SanitizeCommandLine(scpArgs));

                using var process = new Process { StartInfo = processInfo };
                process.Start();

                var timeout = _config.Transfer.Ssh.TransferTimeout;
                bool completed = process.WaitForExit(timeout * 1000);

                if (!completed)
                {
                    process.Kill();
                    throw new TimeoutException($"SCP transfer timed out after {timeout} seconds");
                }

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                if (process.ExitCode == 0)
                {
                    return true;
                }
                else
                {
                    _logger.LogError("SCP transfer failed with exit code {ExitCode}. Error: {Error}",
                        process.ExitCode, error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SCP transfer failed for {LocalPath} to {RemotePath}", localPath, remotePath);
                throw;
            }
        }

        private string BuildScpCommand(string localPath, string remotePath)
        {
            var args = new List<string>();

            var safeHost = ValidateSshHost(_config.Transfer.Ssh.Host);
            var safeUsername = ValidateSshUsername(_config.Transfer.Ssh.Username);
            var safeRemotePath = SanitizeRemotePath(remotePath);

            args.Add($"-P {_config.Transfer.Ssh.Port}");
            args.Add($"-i \"{_config.Transfer.Ssh.PrivateKeyPath}\"");
            
            if (_config.Transfer.Ssh.Compression)
            {
                args.Add("-C");
            }

            args.Add("-o BatchMode=yes");
            args.Add("-o StrictHostKeyChecking=yes");
            args.Add($"\"{localPath}\"");
            args.Add($"\"{safeUsername}@{safeHost}:{safeRemotePath}\"");

            return string.Join(" ", args);
        }

        private string BuildSshCommand(string command)
        {
            var args = new List<string>();

            var safeHost = ValidateSshHost(_config.Transfer.Ssh.Host);
            var safeUsername = ValidateSshUsername(_config.Transfer.Ssh.Username);

            args.Add($"-p {_config.Transfer.Ssh.Port}");
            args.Add($"-i \"{_config.Transfer.Ssh.PrivateKeyPath}\"");
            args.Add($"-o ConnectTimeout={_config.Transfer.Ssh.ConnectionTimeout}");
            args.Add("-o StrictHostKeyChecking=yes");
            args.Add("-o PasswordAuthentication=no");
            args.Add("-o PubkeyAuthentication=yes");
            args.Add("-o BatchMode=yes");
            args.Add($"\"{safeUsername}@{safeHost}\"");
            args.Add($"\"{command}\"");

            return string.Join(" ", args);
        }

        private async Task<long> GetRemoteFileSize(string remotePath)
        {
            try
            {
                var sshArgs = BuildSshCommand($"stat -c%s \"{remotePath}\"");

                var processInfo = new ProcessStartInfo
                {
                    FileName = "ssh.exe",
                    Arguments = sshArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();

                var timeout = _config.Transfer.Ssh.ConnectionTimeout;
                bool completed = process.WaitForExit(timeout * 1000);

                if (!completed)
                {
                    process.Kill();
                    return -1;
                }

                if (process.ExitCode == 0)
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    if (long.TryParse(output.Trim(), out long fileSize))
                    {
                        return fileSize;
                    }
                }

                return -1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get remote file size: {RemotePath}", remotePath);
                return -1;
            }
        }

        private static string GetRelativePath(string basePath, string targetPath)
        {
            var baseUri = new Uri(basePath + Path.DirectorySeparatorChar);
            var targetUri = new Uri(targetPath);
            var relativeUri = baseUri.MakeRelativeUri(targetUri);
            return Uri.UnescapeDataString(relativeUri.ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        private static string ValidateSshHost(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentException("SSH host cannot be null or empty");

            host = host.Trim();

            // SECURITY FIX (MEDIUM-3): Support IP addresses in addition to hostnames
            
            // Try to parse as IP address (IPv4 or IPv6)
            if (System.Net.IPAddress.TryParse(host, out var ipAddress))
            {
                // Valid IP address
                if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ||
                    ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                {
                    return host;
                }
                throw new ArgumentException($"Invalid IP address family: {host}");
            }

            // Validate as hostname/FQDN
            // Require at least 2 characters for real-world hostnames (single char is unusual)
            if (host.Length < 2)
                throw new ArgumentException($"SSH hostname too short: {host}");

            // Valid hostname pattern: letters, digits, hyphens, dots
            // Segments must be 1-63 characters, total max 253 characters
            if (!Regex.IsMatch(host, @"^[a-zA-Z0-9]([a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$"))
                throw new ArgumentException($"Invalid SSH hostname format: {host}");

            if (host.Length > 253)
                throw new ArgumentException($"SSH hostname too long (max 253 characters): {host}");

            return host;
        }

        private static string ValidateSshUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("SSH username cannot be null or empty");

            username = username.Trim();

            if (!Regex.IsMatch(username, @"^[a-zA-Z0-9._-]+$"))
                throw new ArgumentException($"Invalid SSH username format: {username}");

            if (username.Length > 32)
                throw new ArgumentException($"SSH username too long (max 32 characters): {username}");

            return username;
        }

        private string SanitizeRemotePath(string remotePath)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
                throw new ArgumentException("Remote path cannot be null or empty");

            remotePath = remotePath.Trim();

            if (remotePath.Contains("..") || remotePath.Contains("//"))
                throw new ArgumentException($"Potentially dangerous path detected: {remotePath}");

            remotePath = remotePath.Replace('\\', '/');

            if (!remotePath.StartsWith("/"))
                remotePath = "/" + remotePath;

            return remotePath;
        }
    }
}

