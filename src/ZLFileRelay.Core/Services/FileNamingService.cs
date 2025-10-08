using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace ZLFileRelay.Core.Services
{
    /// <summary>
    /// Interface for file naming operations
    /// </summary>
    public interface IFileNamingService
    {
        string ResolveConflict(string filePath, string conflictResolution);
    }

    /// <summary>
    /// Handles file naming and conflict resolution
    /// </summary>
    public class FileNamingService : IFileNamingService
    {
        private readonly ILogger<FileNamingService> _logger;

        public FileNamingService(ILogger<FileNamingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string ResolveConflict(string filePath, string conflictResolution)
        {
            PathValidator.ValidatePath(filePath);

            if (!File.Exists(filePath))
                return filePath;

            switch (conflictResolution.ToLowerInvariant())
            {
                case "overwrite":
                    _logger.LogInformation("File exists, will overwrite: {FilePath}", filePath);
                    File.Delete(filePath);
                    return filePath;

                case "skip":
                    _logger.LogInformation("File exists, skipping: {FilePath}", filePath);
                    throw new IOException($"File already exists and conflict resolution is set to Skip: {filePath}");

                case "append":
                default:
                    return AppendCounterToFileName(filePath);
            }
        }

        private string AppendCounterToFileName(string filePath)
        {
            string? directory = Path.GetDirectoryName(filePath);
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);

            if (string.IsNullOrEmpty(directory))
                return filePath;

            int counter = 1;
            string newFilePath;

            do
            {
                newFilePath = Path.Combine(directory, $"{fileNameWithoutExt} ({counter}){extension}");
                counter++;
            }
            while (File.Exists(newFilePath));

            PathValidator.ValidatePath(newFilePath);

            _logger.LogInformation("File exists, appending counter: {OriginalFile} -> {NewFile}",
                Path.GetFileName(filePath), Path.GetFileName(newFilePath));

            return newFilePath;
        }
    }
}

