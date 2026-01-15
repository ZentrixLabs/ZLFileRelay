using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZLFileRelay.Core.Interfaces;
using ZLFileRelay.Core.Models;
using ZLFileRelay.Core.Services;

namespace ZLFileRelay.Service.Services
{
    /// <summary>
    /// SMB/CIFS file transfer service implementation
    /// </summary>
    public class SmbFileTransferService : IFileTransferService
    {
        private readonly ILogger<SmbFileTransferService> _logger;
        private readonly ZLFileRelayConfiguration _config;
        private readonly ICredentialProvider _credentialProvider;
        private readonly RetryPolicy _retryPolicy;
        private readonly IDiskSpaceChecker _diskSpaceChecker;

        public SmbFileTransferService(
            ILogger<SmbFileTransferService> logger,
            ZLFileRelayConfiguration config,
            ICredentialProvider credentialProvider,
            RetryPolicy retryPolicy,
            IDiskSpaceChecker diskSpaceChecker)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _credentialProvider = credentialProvider ?? throw new ArgumentNullException(nameof(credentialProvider));
            _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
            _diskSpaceChecker = diskSpaceChecker ?? throw new ArgumentNullException(nameof(diskSpaceChecker));
        }

        public string GetTransferMethod() => "SMB/CIFS";

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
                }, $"SmbTransfer({Path.GetFileName(sourceFilePath)})", cancellationToken);

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
                var smbPath = GetSmbSharePath();
                
                if (_config.Transfer.Smb.UseCredentials)
                {
                    var credentials = ((CredentialProvider)_credentialProvider).GetCredential();
                    using var connection = new NetworkConnection(smbPath, credentials);
                    return Task.FromResult(Directory.Exists(smbPath));
                }
                else
                {
                    return Task.FromResult(Directory.Exists(smbPath));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMB connection test failed");
                return Task.FromResult(false);
            }
        }

        public Task<bool> VerifyTransferAsync(string sourceFilePath, string destinationPath, 
            CancellationToken cancellationToken = default)
        {
            if (!_config.Service.VerifyTransfer)
                return Task.FromResult(true);

            try
            {
                var sourceInfo = new FileInfo(sourceFilePath);
                var destInfo = new FileInfo(destinationPath);

                if (!destInfo.Exists)
                {
                    _logger.LogError("Destination file does not exist: {DestinationPath}", destinationPath);
                    return Task.FromResult(false);
                }

                if (sourceInfo.Length != destInfo.Length)
                {
                    _logger.LogError("File size mismatch: Source={SourceSize}, Destination={DestSize}",
                        sourceInfo.Length, destInfo.Length);
                    return Task.FromResult(false);
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during file verification");
                return Task.FromResult(false);
            }
        }

        private async Task TransferFileInternal(string sourceFile, string? customDestination, TransferResult result)
        {
            // Validate source file
            PathValidator.ValidateFile(sourceFile,
                _config.Security.AllowExecutableFiles,
                _config.Security.AllowHiddenFiles,
                _config.Security.MaxUploadSizeBytes);

            var fileInfo = new FileInfo(sourceFile);
            result.FileSize = fileInfo.Length;

            // Determine destination path
            string destinationFile = customDestination ?? GetDefaultDestinationPath(sourceFile);
            result.DestinationPath = destinationFile;

            _logger.LogInformation("SMB transferring: {FileName} ({FileSize} bytes) to {DestinationPath}",
                result.FileName, result.FileSize, destinationFile);

            // Get SMB share path
            var smbSharePath = GetSmbSharePath();

            // Establish network connection if credentials are required
            NetworkConnection? networkConnection = null;
            
            try
            {
                if (_config.Transfer.Smb.UseCredentials)
                {
                    var credentials = ((CredentialProvider)_credentialProvider).GetCredential();
                    networkConnection = new NetworkConnection(smbSharePath, credentials);
                }

                // Ensure destination directory exists
                string? destinationDir = Path.GetDirectoryName(destinationFile);
                if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                    _logger.LogDebug("Created destination directory: {Directory}", destinationDir);
                }

                // Check disk space
                if (_config.Service.CheckDiskSpace)
                {
                    _diskSpaceChecker.CheckAvailableSpace(destinationFile, fileInfo.Length, 
                        _config.Service.MinimumFreeDiskSpaceBytes);
                }

                // Handle file name conflicts
                string finalDestinationFile = ResolveFileConflict(destinationFile);
                result.DestinationPath = finalDestinationFile;

                // Perform the file copy
                await Task.Run(() => File.Copy(sourceFile, finalDestinationFile, overwrite: true));

                _logger.LogInformation("SMB transfer completed successfully to: {DestinationPath}", finalDestinationFile);

                // Verify the transfer
                result.Verified = await VerifyTransferAsync(sourceFile, finalDestinationFile);

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
            finally
            {
                networkConnection?.Dispose();
            }
        }

        private string GetDefaultDestinationPath(string sourceFile)
        {
            string relativePath = GetRelativePath(_config.Service.WatchDirectory, sourceFile);
            string smbPath = Path.Combine(_config.Transfer.Smb.Server, _config.Transfer.Smb.SharePath);
            return Path.Combine(smbPath, relativePath);
        }

        private string GetSmbSharePath()
        {
            return $@"\\{_config.Transfer.Smb.Server}\{_config.Transfer.Smb.SharePath}";
        }

        private string ResolveFileConflict(string filePath)
        {
            if (!File.Exists(filePath))
                return filePath;

            var conflictResolution = _config.Service.ConflictResolution?.ToLowerInvariant() ?? "append";

            switch (conflictResolution)
            {
                case "overwrite":
                    _logger.LogInformation("File exists, will overwrite: {FilePath}", filePath);
                    return filePath;

                case "skip":
                    throw new InvalidOperationException($"File already exists: {filePath}");

                case "append":
                default:
                    string? directory = Path.GetDirectoryName(filePath);
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                    string extension = Path.GetExtension(filePath);
                    int counter = 1;
                    string newFilePath;

                    do
                    {
                        newFilePath = Path.Combine(directory!, $"{fileNameWithoutExt} ({counter}){extension}");
                        counter++;
                    }
                    while (File.Exists(newFilePath));

                    _logger.LogInformation("File exists, using alternate name: {OriginalFile} -> {NewFile}",
                        Path.GetFileName(filePath), Path.GetFileName(newFilePath));

                    return newFilePath;
            }
        }

        private static string GetRelativePath(string basePath, string targetPath)
        {
            var baseUri = new Uri(basePath + Path.DirectorySeparatorChar);
            var targetUri = new Uri(targetPath);
            var relativeUri = baseUri.MakeRelativeUri(targetUri);
            return Uri.UnescapeDataString(relativeUri.ToString().Replace('/', Path.DirectorySeparatorChar));
        }
    }
}

