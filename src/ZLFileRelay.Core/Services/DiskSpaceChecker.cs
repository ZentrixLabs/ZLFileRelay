using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace ZLFileRelay.Core.Services
{
    /// <summary>
    /// Interface for disk space checking
    /// </summary>
    public interface IDiskSpaceChecker
    {
        void CheckAvailableSpace(string destinationPath, long fileSize, long minimumFreeSpace);
    }

    /// <summary>
    /// Checks available disk space before file operations
    /// </summary>
    public class DiskSpaceChecker : IDiskSpaceChecker
    {
        private readonly ILogger<DiskSpaceChecker> _logger;

        public DiskSpaceChecker(ILogger<DiskSpaceChecker> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void CheckAvailableSpace(string destinationPath, long fileSize, long minimumFreeSpace)
        {
            try
            {
                string? rootPath = Path.GetPathRoot(destinationPath);
                
                if (string.IsNullOrEmpty(rootPath))
                {
                    _logger.LogWarning("Could not determine root path for destination: {DestinationPath}", destinationPath);
                    return;
                }

                DriveInfo drive = new DriveInfo(rootPath);

                long availableSpace = drive.AvailableFreeSpace;
                long requiredSpace = fileSize + minimumFreeSpace;

                _logger.LogDebug("Disk space check - Available: {AvailableSpace:N0} bytes, Required: {RequiredSpace:N0} bytes", 
                    availableSpace, requiredSpace);

                if (availableSpace < requiredSpace)
                {
                    throw new IOException(
                        $"Insufficient disk space. Available: {FormatBytes(availableSpace)}, " +
                        $"Required: {FormatBytes(requiredSpace)} (file: {FormatBytes(fileSize)}, minimum free: {FormatBytes(minimumFreeSpace)})");
                }

                _logger.LogDebug("Disk space check passed. Available: {AvailableSpace}", FormatBytes(availableSpace));
            }
            catch (IOException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not check disk space for destination: {DestinationPath}. Proceeding with transfer.", destinationPath);
            }
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}

