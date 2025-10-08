using System;
using System.IO;
using System.Security;

namespace ZLFileRelay.Core.Services
{
    /// <summary>
    /// Provides validation for file paths to prevent security vulnerabilities
    /// </summary>
    public static class PathValidator
    {
        private const int MaxPathLength = 260; // Windows MAX_PATH
        private static readonly string[] DangerousExtensions = { ".ps1", ".vbs", ".js", ".scr", ".jar" };

        /// <summary>
        /// Validates a file path for security and correctness
        /// </summary>
        public static void ValidatePath(string path, bool mustExist = false)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            if (path.Length > MaxPathLength)
                throw new ArgumentException($"Path exceeds maximum length of {MaxPathLength} characters", nameof(path));

            // Get the canonicalized path first
            string canonicalizedPath;
            try
            {
                canonicalizedPath = Path.GetFullPath(path);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid path format: {ex.Message}", nameof(path), ex);
            }

            // Check for path traversal attempts
            if (ContainsPathTraversal(canonicalizedPath))
                throw new SecurityException($"Path traversal detected in path: {path}");

            // Check for dangerous characters
            if (ContainsDangerousCharacters(path))
                throw new SecurityException("Invalid characters detected in path");

            // Check for UNC path injection
            if (path.StartsWith("\\\\") && ContainsUncTraversal(path))
                throw new SecurityException("UNC path traversal detected");

            // Additional security checks
            ValidateCanonicalizedPath(canonicalizedPath);

            // Check if path must exist
            if (mustExist && !Directory.Exists(path) && !File.Exists(path))
                throw new FileNotFoundException($"Path does not exist: {path}");
        }

        /// <summary>
        /// Validates a file for security and size limits
        /// </summary>
        public static void ValidateFile(string filePath, bool allowExecutables = true, 
            bool allowHiddenFiles = false, long maxFileSize = 5L * 1024 * 1024 * 1024)
        {
            ValidatePath(filePath, mustExist: true);

            var fileInfo = new FileInfo(filePath);
            
            // Check file size
            if (fileInfo.Length > maxFileSize)
                throw new ArgumentException(
                    $"File size ({fileInfo.Length} bytes) exceeds maximum allowed size ({maxFileSize} bytes)");

            // Check for dangerous file extensions
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (Array.Exists(DangerousExtensions, ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase)))
                throw new SecurityException($"File extension '{extension}' is not allowed for security reasons");

            // Check for hidden files if not allowed
            if (!allowHiddenFiles && fileInfo.Attributes.HasFlag(FileAttributes.Hidden))
                throw new SecurityException("Hidden files are not allowed");
        }

        /// <summary>
        /// Validates a directory path
        /// </summary>
        public static void ValidateDirectory(string directoryPath, bool mustExist = false)
        {
            ValidatePath(directoryPath, mustExist);

            if (mustExist)
            {
                var dirInfo = new DirectoryInfo(directoryPath);
                if (!dirInfo.Exists)
                    throw new DirectoryNotFoundException($"Directory does not exist: {directoryPath}");
            }
        }

        /// <summary>
        /// Sanitizes a filename by removing dangerous characters
        /// </summary>
        public static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Filename cannot be null or empty", nameof(fileName));

            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = fileName;

            foreach (var invalidChar in invalidChars)
            {
                sanitized = sanitized.Replace(invalidChar, '_');
            }

            sanitized = sanitized.Replace("..", "_").Replace("~", "_");

            return sanitized.Trim();
        }

        /// <summary>
        /// Checks if a path is within the allowed base directory
        /// </summary>
        public static bool IsPathWithinBase(string basePath, string targetPath)
        {
            try
            {
                var fullBasePath = Path.GetFullPath(basePath).TrimEnd('\\', '/');
                var fullTargetPath = Path.GetFullPath(targetPath);

                return fullTargetPath.StartsWith(fullBasePath, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static bool ContainsPathTraversal(string path)
        {
            var traversalPatterns = new[]
            {
                "..\\", "../", "\\..\\", "/../", "\\..", "/..", "~\\", "~/",
                "%2e%2e%5c", "%2e%2e%2f", "%252e%252e%255c", "%252e%252e%252f"
            };

            foreach (var pattern in traversalPatterns)
            {
                if (path.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static bool ContainsDangerousCharacters(string path)
        {
            var dangerousChars = new[] { '<', '>', '|', '?', '*', '"', '\0' };
            return path.IndexOfAny(dangerousChars) >= 0;
        }

        private static bool ContainsUncTraversal(string path)
        {
            var uncStart = path.IndexOf("\\\\", StringComparison.Ordinal);
            if (uncStart >= 0)
            {
                var pathAfterUnc = path.Substring(uncStart + 2);
                return pathAfterUnc.Contains("..");
            }
            return false;
        }

        private static void ValidateCanonicalizedPath(string canonicalizedPath)
        {
            var fileName = Path.GetFileName(canonicalizedPath);
            if (!string.IsNullOrEmpty(fileName))
            {
                var reservedNames = new[]
                {
                    "CON", "PRN", "AUX", "NUL",
                    "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
                    "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
                };

                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName).ToUpperInvariant();
                if (Array.Exists(reservedNames, name => name.Equals(fileNameWithoutExt, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new SecurityException($"Reserved Windows name detected: {fileName}");
                }
            }

            if (canonicalizedPath.Contains("$MFT") || canonicalizedPath.Contains("$LogFile") || 
                canonicalizedPath.Contains("$Volume") || canonicalizedPath.Contains("$AttrDef") ||
                canonicalizedPath.Contains("$Bitmap") || canonicalizedPath.Contains("$Boot"))
            {
                throw new SecurityException("Access to system files detected");
            }
        }
    }
}

