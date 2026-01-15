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
            var transferResult = await ExecuteScpTransfer(sourceFile, finalRemotePath);

            if (!transferResult.Success)
            {
                var errorMsg = string.IsNullOrWhiteSpace(transferResult.ErrorMessage)
                    ? $"SCP transfer failed for {Path.GetFileName(sourceFile)}"
                    : $"SCP transfer failed for {Path.GetFileName(sourceFile)}: {transferResult.ErrorMessage}";
                throw new InvalidOperationException(errorMsg);
            }

            _logger.LogInformation("SCP transfer completed successfully to: {RemotePath}", finalRemotePath);

            // Verify the transfer
            result.Verified = await VerifyTransferAsync(sourceFile, finalRemotePath);

            // Archive or delete source file after successful transfer
            if (result.Verified)
            {
                if (_config.Service.ArchiveAfterTransfer)
                {
                    try
                    {
                        // Move to archive directory preserving folder structure
                        string relativePath = GetRelativePath(_config.Service.WatchDirectory, sourceFile);
                        string archivePath = Path.Combine(_config.Service.ArchiveDirectory, relativePath);
                        
                        // Ensure archive subdirectory exists
                        string? archiveDir = Path.GetDirectoryName(archivePath);
                        if (!string.IsNullOrEmpty(archiveDir))
                        {
                            Directory.CreateDirectory(archiveDir);
                        }
                        
                        // Move file to archive (handle duplicates with timestamp)
                        if (File.Exists(archivePath))
                        {
                            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(archivePath);
                            string extension = Path.GetExtension(archivePath);
                            archivePath = Path.Combine(archiveDir!, $"{fileNameWithoutExt}_{timestamp}{extension}");
                        }
                        
                        File.Move(sourceFile, archivePath);
                        _logger.LogInformation("Archived file after successful transfer: {FileName} → {ArchivePath}", 
                            result.FileName, archivePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to archive file: {FileName}", result.FileName);
                    }
                }
                else if (_config.Service.DeleteAfterTransfer)
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
        }

        private string GetDefaultRemotePath(string sourceFile)
        {
            string relativePath = GetRelativePath(_config.Service.WatchDirectory, sourceFile);
            return Path.Combine(_config.Transfer.Ssh.DestinationPath, relativePath).Replace('\\', '/');
        }

        private Task EnsureRemoteDirectory(string remotePath)
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

            return Task.CompletedTask;
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

        private async Task<(bool Success, string? ErrorMessage)> ExecuteScpTransfer(string localPath, string remotePath)
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
                    var timeoutError = $"SCP transfer timed out after {timeout} seconds";
                    _logger.LogError(timeoutError);
                    return (false, timeoutError);
                }

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                if (process.ExitCode == 0)
                {
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        _logger.LogDebug("SCP transfer output: {Output}", output);
                    }
                    return (true, null);
                }
                else
                {
                    // Extract user-friendly error message from SCP output
                    var errorMessage = ExtractUserFriendlyErrorMessage(error, process.ExitCode);
                    
                    // Log detailed error information for debugging
                    _logger.LogError("SCP transfer failed with exit code {ExitCode}. " +
                                   "Local path: {LocalPath}, Remote path: {RemotePath}. " +
                                   "Output: {Output}, Error: {Error}",
                        process.ExitCode, localPath, remotePath, output, error);
                    
                    return (false, errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SCP transfer failed for {LocalPath} to {RemotePath}", localPath, remotePath);
                return (false, ex.Message);
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
            
            // Configure host key checking based on settings
            // Use accept-new when enabled (auto-accepts new hosts but verifies on subsequent connections)
            // Use no when disabled (less secure, no verification)
            var hostKeyCheckingValue = _config.Transfer.Ssh.StrictHostKeyChecking ? "accept-new" : "no";
            args.Add($"-o StrictHostKeyChecking={hostKeyCheckingValue}");
            
            // Use service-account-specific known_hosts file location
            var knownHostsPath = Path.Combine(
                _config.Paths.ConfigDirectory ?? @"C:\ProgramData\ZLFileRelay",
                "known_hosts");
            args.Add($"-o UserKnownHostsFile=\"{knownHostsPath}\"");
            
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
            
            // Configure host key checking based on settings
            var hostKeyCheckingValue = _config.Transfer.Ssh.StrictHostKeyChecking ? "accept-new" : "no";
            args.Add($"-o StrictHostKeyChecking={hostKeyCheckingValue}");
            
            // Use service-account-specific known_hosts file location
            var knownHostsPath = Path.Combine(
                _config.Paths.ConfigDirectory ?? @"C:\ProgramData\ZLFileRelay",
                "known_hosts");
            args.Add($"-o UserKnownHostsFile=\"{knownHostsPath}\"");
            
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

            // Prevent path traversal/dangerous sequences
            if (remotePath.Contains("..") || remotePath.Contains("//"))
                throw new ArgumentException($"Potentially dangerous path detected: {remotePath}");

            // Normalize separators first
            remotePath = remotePath.Replace('\\', '/');

            // Handle Windows-style destination (e.g., F:/Transfer)
            // When using Windows OpenSSH as the server, scp expects 'F:/path' (no leading slash)
            // When using Linux server and a Windows-like path was supplied, map to '/f/path'
            if (remotePath.Length >= 2 && char.IsLetter(remotePath[0]) && remotePath[1] == ':')
            {
                char driveLetter = remotePath[0];
                string pathPart = remotePath.Substring(2); // already with forward slashes

                bool isWindowsServer = _config.Transfer.Ssh.RemoteServerType?.Equals("Windows", StringComparison.OrdinalIgnoreCase) == true;

                if (isWindowsServer)
                {
                    // Windows server: 'F:/path' (uppercase drive, no leading slash)
                    driveLetter = char.ToUpperInvariant(driveLetter);
                    // Ensure single leading slash on path part is removed
                    if (pathPart.StartsWith("/")) pathPart = pathPart.TrimStart('/');
                    return $"{driveLetter}:{pathPart}";
                }
                else
                {
                    // Linux server: map to '/f/path'
                    driveLetter = char.ToLowerInvariant(driveLetter);
                    // Ensure path part starts with '/'
                    if (!pathPart.StartsWith("/")) pathPart = "/" + pathPart;
                    return $"/{driveLetter}{pathPart}";
                }
            }

            // Non-drive paths: ensure forward slashes and leading slash once
            if (!remotePath.StartsWith("/"))
                remotePath = "/" + remotePath;

            return remotePath;
        }

        /// <summary>
        /// Extracts a user-friendly error message from SCP error output
        /// </summary>
        private static string ExtractUserFriendlyErrorMessage(string errorOutput, int exitCode)
        {
            if (string.IsNullOrWhiteSpace(errorOutput))
            {
                return $"SCP transfer failed (exit code: {exitCode})";
            }

            var error = errorOutput.Trim();

            // Common SSH/SCP error patterns - extract the most useful message
            if (error.Contains("bad permissions", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("permissions are too open", StringComparison.OrdinalIgnoreCase))
            {
                // Extract the key path if present
                var keyMatch = System.Text.RegularExpressions.Regex.Match(error, @"'([^']+\.(pem|key))'");
                var keyPath = keyMatch.Success ? keyMatch.Groups[1].Value : "SSH key";
                
                return $"SSH key permissions are too open. The key file '{keyPath}' must only be readable by the service account. Use the 'Fix Permissions' button in the Config Tool.";
            }

            if (error.Contains("Permission denied", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("permission denied", StringComparison.OrdinalIgnoreCase))
            {
                if (error.Contains("publickey", StringComparison.OrdinalIgnoreCase))
                {
                    return "SSH authentication failed. Check that:\n" +
                           "• The SSH key file has correct permissions (only service account can read)\n" +
                           "• The public key is installed on the remote server\n" +
                           "• The service account has access to the key file仁义";
                }
                return "Permission denied. Check SSH key permissions and remote server access.";
            }

            if (error.Contains("Host key verification failed", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("No ED25519 host key is known", StringComparison.OrdinalIgnoreCase))
            {
                return "Host key verification failed. The remote server's host key is not trusted. This should be automatically resolved on first connection.";
            }

            if (error.Contains("Connection refused", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("Connection closed", StringComparison.OrdinalIgnoreCase))
            {
                return "Could not connect to remote server. Check that:\n" +
                       "• The server is accessible on the network\n" +
                       "• The SSH port is correct and open\n" +
                       "• The SSH service is running on the remote server";
            }

            if (error.Contains("No such file or directory", StringComparison.OrdinalIgnoreCase))
            {
                return "Destination directory does not exist on remote server. The directory may need to be created first.";
            }

            if (error.Contains("No space left on device", StringComparison.OrdinalIgnoreCase))
            {
                return "Remote server is out of disk space. Free up space on the destination server.";
            }

            // Extract the last meaningful line (usually the actual error)
            var lines = error.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var lastMeaningfulLine = lines.LastOrDefault(line => 
                !string.IsNullOrWhiteSpace(line) && 
                !line.Contains("Load key", StringComparison.OrdinalIgnoreCase) &&
                !line.Contains("scp.exe:", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(lastMeaningfulLine))
            {
                // Clean up the message
                var cleaned = lastMeaningfulLine.Trim();
                if (cleaned.Contains(':'))
                {
                    // Take part after the colon (error message part)
                    var parts = cleaned.Split(':', 2);
                    if (parts.Length > 1)
                    {
                        cleaned = parts[1].Trim();
                    }
                }
                return cleaned;
            }

            // Fallback to full error (truncated if too long)
            if (error.Length > 200)
            {
                return error.Substring(0, 200) + "...";
            }

            return error;
        }
    }
}

