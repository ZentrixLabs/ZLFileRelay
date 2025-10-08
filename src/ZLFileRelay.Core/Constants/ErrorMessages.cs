namespace ZLFileRelay.Core.Constants;

/// <summary>
/// Centralized error messages
/// </summary>
public static class ErrorMessages
{
    public static class Configuration
    {
        public const string ConfigFileNotFound = "Configuration file not found: {0}";
        public const string InvalidConfiguration = "Invalid configuration: {0}";
        public const string MissingRequiredSetting = "Missing required setting: {0}";
    }
    
    public static class FileTransfer
    {
        public const string FileNotFound = "Source file not found: {0}";
        public const string TransferFailed = "File transfer failed: {0}";
        public const string ConnectionFailed = "Connection to destination failed: {0}";
        public const string AuthenticationFailed = "Authentication failed: {0}";
        public const string VerificationFailed = "File verification failed: {0}";
        public const string DestinationNotReachable = "Destination not reachable: {0}";
    }
    
    public static class FileUpload
    {
        public const string FileTooLarge = "File size exceeds maximum allowed: {0}";
        public const string InvalidFileExtension = "File extension not allowed: {0}";
        public const string UploadFailed = "File upload failed: {0}";
        public const string DestinationNotConfigured = "Upload destination not configured: {0}";
        public const string InsufficientDiskSpace = "Insufficient disk space for upload";
    }
    
    public static class Security
    {
        public const string UnauthorizedAccess = "User is not authorized to perform this action";
        public const string InvalidCredentials = "Invalid or missing credentials";
        public const string EncryptionFailed = "Failed to encrypt data: {0}";
        public const string DecryptionFailed = "Failed to decrypt data: {0}";
        public const string SshKeyNotFound = "SSH private key not found: {0}";
        public const string InvalidSshKey = "Invalid SSH key format";
    }
    
    public static class Service
    {
        public const string ServiceStartFailed = "Service failed to start: {0}";
        public const string ServiceStopFailed = "Service failed to stop: {0}";
        public const string WatchDirectoryNotFound = "Watch directory not found: {0}";
        public const string WatchDirectoryAccessDenied = "Access denied to watch directory: {0}";
    }
}
