using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ZLFileRelay.Core.Interfaces;
using ZLFileRelay.Core.Models;
using ZLFileRelay.Core.Services;

namespace ZLFileRelay.WebPortal.Services
{
    /// <summary>
    /// Service for handling file uploads through the web portal
    /// </summary>
    public class FileUploadService : IFileUploadService
    {
        private readonly ZLFileRelayConfiguration _config;
        private readonly ILogger<FileUploadService> _logger;

        public FileUploadService(
            ZLFileRelayConfiguration config,
            ILogger<FileUploadService> logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // IFileUploadService implementation
        public async Task<UploadResult> UploadFileAsync(Stream fileStream, string fileName, 
            string destination, string uploadedBy, bool requiresTransfer = false, 
            string? notes = null, CancellationToken cancellationToken = default)
        {
            var result = new UploadResult
            {
                FileName = fileName,
                FileSize = fileStream.Length,
                UploadTime = DateTime.Now,
                UploadedBy = uploadedBy
            };

            try
            {
                _logger.LogInformation("Processing file: {FileName} ({FileSize} bytes) for user: {Username}", 
                    fileName, fileStream.Length, uploadedBy);

                // Validate file size
                if (fileStream.Length > _config.Security.MaxUploadSizeBytes)
                {
                    throw new InvalidOperationException(
                        $"File size ({fileStream.Length} bytes) exceeds maximum allowed size ({_config.Security.MaxUploadSizeBytes} bytes)");
                }

                // Validate file extension
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                if (_config.WebPortal.BlockedFileExtensions.Contains(extension))
                {
                    throw new InvalidOperationException($"File extension '{extension}' is blocked for security reasons");
                }

                // Create user-specific subdirectory
                var userFolderName = SanitizeUsername(uploadedBy);
                var userUploadPath = Path.Combine(destination, userFolderName);

                // Create directory if needed
                if (!Directory.Exists(userUploadPath))
                {
                    _logger.LogInformation("Creating user directory: {Directory}", userUploadPath);
                    Directory.CreateDirectory(userUploadPath);
                }

                // Build file path
                var safeFileName = Path.GetFileName(fileName);
                var filePath = Path.Combine(userUploadPath, safeFileName);

                // Handle file conflicts
                if (File.Exists(filePath))
                {
                    _logger.LogInformation("Overwriting existing file: {FilePath}", filePath);
                }

                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(stream, cancellationToken);
                }

                _logger.LogInformation("File saved successfully: {FilePath}", filePath);

                result.Success = true;
                result.FilePath = filePath;
                result.Destination = destination;
                result.RequiresTransfer = requiresTransfer;
                result.Notes = notes;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file: {FileName} for user: {Username}", fileName, uploadedBy);
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        public async Task<List<UploadResult>> UploadFilesAsync(
            IEnumerable<(Stream stream, string fileName)> files, 
            string destination, 
            string uploadedBy, 
            bool requiresTransfer = false, 
            string? notes = null, 
            CancellationToken cancellationToken = default)
        {
            var results = new List<UploadResult>();

            _logger.LogInformation("Starting batch upload for {Username}: {FileCount} files", 
                uploadedBy, files.Count());

            foreach (var (stream, fileName) in files)
            {
                var result = await UploadFileAsync(stream, fileName, destination, uploadedBy, 
                    requiresTransfer, notes, cancellationToken);
                results.Add(result);
            }

            var successCount = results.Count(r => r.Success);
            _logger.LogInformation("Batch upload completed: {SuccessCount}/{TotalCount} files successful", 
                successCount, files.Count());

            return results;
        }

        public Dictionary<string, string> GetUploadDestinations()
        {
            var destinations = new Dictionary<string, string>();

            if (_config.WebPortal.UploadLocations != null)
            {
                foreach (var kvp in _config.WebPortal.UploadLocations)
                {
                    destinations[kvp.Key] = kvp.Value;
                }
            }

            // Add default destinations
            if (!destinations.Any())
            {
                destinations["default"] = _config.Paths.UploadDirectory;
                if (_config.WebPortal.EnableUploadToTransfer)
                {
                    destinations["transfer"] = _config.Service.WatchDirectory;
                }
            }

            return destinations;
        }

        public (bool isValid, string? errorMessage) ValidateFile(string fileName, long fileSize)
        {
            // Check file size
            if (fileSize > _config.Security.MaxUploadSizeBytes)
            {
                return (false, $"File size ({fileSize} bytes) exceeds maximum ({_config.Security.MaxUploadSizeBytes} bytes)");
            }

            // Check file extension
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (_config.WebPortal.BlockedFileExtensions.Contains(extension))
            {
                return (false, $"File extension '{extension}' is not allowed");
            }

            if (_config.WebPortal.AllowedFileExtensions.Any() && 
                !_config.WebPortal.AllowedFileExtensions.Contains(extension))
            {
                return (false, $"File extension '{extension}' is not in the allowed list");
            }

            return (true, null);
        }

        // Helper method for IFormFile (ASP.NET Core convenience)
        public async Task<UploadResult> UploadFormFileAsync(IFormFile file, string uploadedBy, 
            string? destination = null, bool requiresTransfer = false, 
            string? notes = null, CancellationToken cancellationToken = default)
        {
            var dest = destination ?? (_config.WebPortal.EnableUploadToTransfer 
                ? _config.Service.WatchDirectory 
                : _config.Paths.UploadDirectory);

            using var stream = file.OpenReadStream();
            return await UploadFileAsync(stream, file.FileName, dest, uploadedBy, 
                requiresTransfer, notes, cancellationToken);
        }

        public async Task<List<UploadResult>> UploadFormFilesAsync(List<IFormFile> files, string uploadedBy, 
            string? destination = null, bool requiresTransfer = false, 
            string? notes = null, CancellationToken cancellationToken = default)
        {
            var results = new List<UploadResult>();

            _logger.LogInformation("Starting batch upload for {Username}: {FileCount} files", uploadedBy, files.Count);

            foreach (var file in files)
            {
                var result = await UploadFormFileAsync(file, uploadedBy, destination, requiresTransfer, notes, cancellationToken);
                results.Add(result);
            }

            var successCount = results.Count(r => r.Success);
            _logger.LogInformation("Batch upload completed: {SuccessCount}/{TotalCount} files successful", 
                successCount, files.Count);

            return results;
        }

        private static string SanitizeUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return "unknown_user";

            // Remove domain prefix (DOMAIN\user or user@domain.com)
            var cleanUsername = username;
            if (username.Contains('\\'))
            {
                cleanUsername = username.Split('\\').Last();
            }
            else if (username.Contains('@'))
            {
                cleanUsername = username.Split('@').First();
            }

            // Remove invalid filename characters
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var invalidChar in invalidChars)
            {
                cleanUsername = cleanUsername.Replace(invalidChar, '_');
            }

            // Replace spaces with underscores and convert to lowercase
            cleanUsername = cleanUsername.Replace(' ', '_').ToLowerInvariant();

            // Ensure it's not empty
            if (string.IsNullOrWhiteSpace(cleanUsername))
                return "unknown_user";

            return cleanUsername;
        }
    }
}

