using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly IOptionsMonitor<ZLFileRelayConfiguration> _configMonitor;
        private readonly ILogger<FileUploadService> _logger;
        private readonly ITransferStatusService? _transferStatusService;

        private ZLFileRelayConfiguration Config => _configMonitor.CurrentValue;

        public FileUploadService(
            IOptionsMonitor<ZLFileRelayConfiguration> configMonitor,
            ILogger<FileUploadService> logger,
            ITransferStatusService? transferStatusService = null)
        {
            _configMonitor = configMonitor ?? throw new ArgumentNullException(nameof(configMonitor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _transferStatusService = transferStatusService; // Optional for backward compatibility
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

                // SECURITY FIX (MEDIUM-2): Validate file extension FIRST (cheaper check, faster rejection)
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                if (IsExtensionBlocked(extension))
                {
                    throw new InvalidOperationException($"File extension '{extension}' is blocked for security reasons");
                }

                // Validate file size (after extension check)
                if (fileStream.Length > Config.Security.MaxUploadSizeBytes)
                {
                    throw new InvalidOperationException(
                        $"File size ({fileStream.Length} bytes) exceeds maximum allowed size ({Config.Security.MaxUploadSizeBytes} bytes)");
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

                // Register transfer for status tracking if required
                if (requiresTransfer && _transferStatusService != null)
                {
                    try
                    {
                        var transferId = await _transferStatusService.RegisterTransferAsync(
                            filePath, fileName, fileStream.Length, uploadedBy);
                        _logger.LogDebug("Registered transfer tracking: {TransferId}", transferId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to register transfer tracking for {FileName}", fileName);
                    }
                }

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

            if (Config.WebPortal.UploadLocations != null)
            {
                foreach (var kvp in Config.WebPortal.UploadLocations)
                {
                    destinations[kvp.Key] = kvp.Value;
                }
            }

            // Add default destinations
            if (!destinations.Any())
            {
                destinations["default"] = Config.Paths.UploadDirectory;
                if (Config.WebPortal.EnableUploadToTransfer)
                {
                    destinations["transfer"] = Config.Service.WatchDirectory;
                }
            }

            return destinations;
        }

        public (bool isValid, string? errorMessage) ValidateFile(string fileName, long fileSize)
        {
            // Check file size
            if (fileSize > Config.Security.MaxUploadSizeBytes)
            {
                return (false, $"File size ({fileSize} bytes) exceeds maximum ({Config.Security.MaxUploadSizeBytes} bytes)");
            }

            // SECURITY FIX (MEDIUM-5): Enhanced file extension validation
            var extensionValidation = ValidateFileExtension(fileName);
            if (!extensionValidation.isValid)
            {
                return extensionValidation;
            }

            return (true, null);
        }

        /// <summary>
        /// Enhanced file extension validation to prevent bypass attacks.
        /// Checks for double extensions, alternate data streams, and null bytes.
        /// </summary>
        private (bool isValid, string? errorMessage) ValidateFileExtension(string fileName)
        {
            // Check for null bytes (directory traversal attempt)
            if (fileName.Contains('\0'))
            {
                _logger.LogWarning("Blocked file upload with null byte in filename: {FileName}", fileName);
                return (false, "Invalid file name: contains null byte");
            }

            // Check for NTFS alternate data streams
            if (fileName.Contains("::") || fileName.Contains(":$DATA"))
            {
                _logger.LogWarning("Blocked file upload with alternate data stream: {FileName}", fileName);
                return (false, "Invalid file name: alternate data streams are not allowed");
            }

            // Get the primary extension
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            
            // Check for double extension attack (e.g., malicious.txt.exe)
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            if (nameWithoutExtension.Contains('.'))
            {
                // Extract all extensions
                var allExtensions = fileName.Split('.')
                    .Skip(1)  // Skip the actual filename part
                    .Select(e => "." + e.ToLowerInvariant())
                    .ToList();

                // Check if any extension in the chain is blocked
                foreach (var ext in allExtensions)
                {
                    if (IsExtensionBlocked(ext))
                    {
                        _logger.LogWarning("Blocked file upload with multiple extensions, found blocked extension '{Extension}' in: {FileName}", 
                            ext, fileName);
                        return (false, $"File extension '{ext}' is not allowed (detected in multi-extension filename)");
                    }
                }

                // If allowed list is configured, all extensions must be allowed
                if (Config.WebPortal.AllowedFileExtensions.Any())
                {
                    foreach (var ext in allExtensions)
                    {
                        if (!Config.WebPortal.AllowedFileExtensions.Contains(ext))
                        {
                            _logger.LogWarning("File with multiple extensions contains non-allowed extension '{Extension}': {FileName}", 
                                ext, fileName);
                            return (false, $"File extension '{ext}' is not in the allowed list");
                        }
                    }
                }
            }

            // Standard extension validation
            if (IsExtensionBlocked(extension))
            {
                _logger.LogWarning("Blocked file upload with blocked extension '{Extension}': {FileName}", extension, fileName);
                return (false, $"File extension '{extension}' is not allowed");
            }

            // If allowed list is configured, ensure extension is in it
            if (Config.WebPortal.AllowedFileExtensions.Any() && 
                !Config.WebPortal.AllowedFileExtensions.Contains(extension))
            {
                _logger.LogWarning("File upload rejected, extension '{Extension}' not in allowed list: {FileName}", extension, fileName);
                return (false, $"File extension '{extension}' is not in the allowed list");
            }

            return (true, null);
        }

        // Helper method for IFormFile (ASP.NET Core convenience)
        public async Task<UploadResult> UploadFormFileAsync(IFormFile file, string uploadedBy, 
            string? destination = null, bool requiresTransfer = false, 
            string? notes = null, CancellationToken cancellationToken = default)
        {
            // Determine destination based on whether SCADA transfer is required
            string dest;
            if (destination != null)
            {
                // Explicit destination provided
                dest = destination;
            }
            else if (requiresTransfer)
            {
                // Files that need to go to SCADA → Watch directory for transfer
                dest = Config.Service.WatchDirectory;
            }
            else
            {
                // Files that stay in DMZ → Use configurable DMZ directory
                dest = !string.IsNullOrEmpty(Config.WebPortal.DmzUploadDirectory) 
                    ? Config.WebPortal.DmzUploadDirectory 
                    : Config.Paths.UploadDirectory;
                    
                _logger.LogDebug("Upload destination for DMZ file: {Destination}", dest);
            }

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

        /// <summary>
        /// Checks if an extension is blocked, respecting the AllowExecutableFiles setting.
        /// </summary>
        private bool IsExtensionBlocked(string extension)
        {
            // Define executable file extensions
            var executableExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".exe", ".dll", ".bat", ".cmd", ".ps1", ".vbs", ".com", ".scr", ".msi", ".jar"
            };

            bool isExecutable = executableExtensions.Contains(extension);
            bool allowExecutables = Config.Security.AllowExecutableFiles;
            bool inBlockedList = Config.WebPortal.BlockedFileExtensions.Contains(extension);
            
            // If executables are allowed and this is an executable extension, don't block it
            if (allowExecutables && isExecutable)
            {
                _logger.LogDebug("Extension {Extension} allowed: is executable and AllowExecutableFiles=true", extension);
                return false;
            }

            // Otherwise, check the standard blocked extensions list
            bool isBlocked = inBlockedList;
            
            if (isBlocked)
            {
                _logger.LogInformation("Extension {Extension} blocked: AllowExecutableFiles={AllowExec}, IsExecutable={IsExec}, InBlockedList={Blocked}", 
                    extension, allowExecutables, isExecutable, inBlockedList);
            }
            
            return isBlocked;
        }
    }
}

