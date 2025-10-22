namespace ZLFileRelay.Core.Models;

/// <summary>
/// Root configuration model for ZL File Relay
/// </summary>
public class ZLFileRelayConfiguration
{
    public BrandingSettings Branding { get; set; } = new();
    public PathSettings Paths { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
    public ServiceSettings Service { get; set; } = new();
    public WebPortalSettings WebPortal { get; set; } = new();
    public TransferSettings Transfer { get; set; } = new();
    public SecuritySettings Security { get; set; } = new();
}

/// <summary>
/// Branding and customization settings
/// </summary>
public class BrandingSettings
{
    public string CompanyName { get; set; } = "Your Company";
    public string ProductName { get; set; } = "ZL File Relay";
    public string SiteName { get; set; } = "Site Name";
    public string SupportEmail { get; set; } = "support@example.com";
    public string? LogoPath { get; set; }
    public ThemeSettings Theme { get; set; } = new();
}

public class ThemeSettings
{
    public string PrimaryColor { get; set; } = "#0066CC";
    public string SecondaryColor { get; set; } = "#003366";
    public string AccentColor { get; set; } = "#FF6600";
}

/// <summary>
/// Directory path settings
/// </summary>
public class PathSettings
{
    public string UploadDirectory { get; set; } = @"C:\FileRelay\uploads";
    public string TransferDirectory { get; set; } = @"C:\FileRelay\uploads\transfer";
    public string LogDirectory { get; set; } = @"C:\FileRelay\logs";
    public string ConfigDirectory { get; set; } = @"C:\ProgramData\ZLFileRelay";
    public string TempDirectory { get; set; } = @"C:\FileRelay\temp";
}

/// <summary>
/// Logging configuration
/// </summary>
public class LoggingSettings
{
    public int RetentionDays { get; set; } = 30;
    public int MaxFileSizeMB { get; set; } = 100;
    public bool EnableEventLog { get; set; } = true;
    public bool EnableConsole { get; set; } = false;
}

/// <summary>
/// File transfer service settings
/// </summary>
public class ServiceSettings
{
    public bool Enabled { get; set; } = true;
    public string ServiceName { get; set; } = "ZLFileRelay";
    public string DisplayName { get; set; } = "ZL File Relay Service";
    public string Description { get; set; } = "Automated file transfer from DMZ to SCADA networks";
    public string WatchDirectory { get; set; } = @"C:\FileRelay\uploads\transfer";
    public string TransferMethod { get; set; } = "ssh"; // "ssh" or "smb"
    public int RetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 30;
    public double RetryBackoffMultiplier { get; set; } = 2.0;
    public int MaxConcurrentTransfers { get; set; } = 5;
    public string FileFilter { get; set; } = "*.*";
    public bool DeleteAfterTransfer { get; set; } = false;
    public bool ArchiveAfterTransfer { get; set; } = true;
    public string ArchiveDirectory { get; set; } = @"C:\FileRelay\archive";
    public bool VerifyTransfer { get; set; } = true;
    public string ConflictResolution { get; set; } = "Append"; // "Append", "Overwrite", "Skip"
    public bool CheckDiskSpace { get; set; } = true;
    public long MinimumFreeDiskSpaceBytes { get; set; } = 1L * 1024 * 1024 * 1024; // 1GB
    public int FileStabilitySeconds { get; set; } = 5;
    public int ProcessingIntervalSeconds { get; set; } = 10;
    public int MaxQueueSize { get; set; } = 10000;
    public bool IncludeSubdirectories { get; set; } = true;
}

/// <summary>
/// Web portal settings
/// </summary>
public class WebPortalSettings
{
    public bool Enabled { get; set; } = true;
    public bool RequireAuthentication { get; set; } = true;
    public string AuthenticationType { get; set; } = "Windows";
    public List<string> AllowedGroups { get; set; } = new();
    public List<string> AllowedUsers { get; set; } = new();
    public long MaxFileSizeBytes { get; set; } = 4294967295; // ~4GB
    public int MaxConcurrentUploads { get; set; } = 10;
    public List<string> AllowedFileExtensions { get; set; } = new();
    public List<string> BlockedFileExtensions { get; set; } = new() { ".exe", ".dll", ".bat", ".cmd", ".ps1" };
    public bool EnableUploadToTransfer { get; set; } = true;
    public Dictionary<string, string> UploadLocations { get; set; } = new();
    public bool EnableNotifications { get; set; } = false;
    public string? NotificationEmail { get; set; }
    
    // Kestrel Server Settings (for Windows Service deployment)
    public KestrelSettings Kestrel { get; set; } = new();
}

/// <summary>
/// Kestrel web server settings (when running as Windows Service)
/// </summary>
public class KestrelSettings
{
    public int HttpPort { get; set; } = 8080;
    public int HttpsPort { get; set; } = 8443;
    public bool EnableHttps { get; set; } = false;
    public string? CertificatePath { get; set; }
    public string? CertificatePassword { get; set; }
    public bool UseWindowsAuth { get; set; } = true;
}

/// <summary>
/// Transfer destination settings
/// </summary>
public class TransferSettings
{
    public SshSettings Ssh { get; set; } = new();
    public SmbSettings Smb { get; set; } = new();
}

/// <summary>
/// SSH/SCP transfer settings
/// </summary>
public class SshSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 22;
    public string Username { get; set; } = string.Empty;
    public string AuthMethod { get; set; } = "PublicKey"; // "PublicKey" or "Password"
    public string? PrivateKeyPath { get; set; }
    public string? PrivateKeyPassphrase { get; set; }
    public string? PasswordEncrypted { get; set; }
    public string DestinationPath { get; set; } = string.Empty;
    public string RemoteServerType { get; set; } = "Windows"; // "Windows" or "Linux" - affects path format
    public bool CreateDestinationDirectory { get; set; } = true;
    public bool PreserveTimestamps { get; set; } = true;
    public int ConnectionTimeout { get; set; } = 30;
    public int OperationTimeout { get; set; } = 300;
    public int TransferTimeout { get; set; } = 300;
    public int KeepAliveInterval { get; set; } = 30;
    public bool Compression { get; set; } = true;
    public bool StrictHostKeyChecking { get; set; } = true;
}

/// <summary>
/// SMB transfer settings
/// </summary>
public class SmbSettings
{
    public string Server { get; set; } = string.Empty;
    public string SharePath { get; set; } = string.Empty;
    public bool UseCredentials { get; set; } = false;
    public string? Username { get; set; }
    public string? PasswordEncrypted { get; set; }
    public string? Domain { get; set; }
    public int Timeout { get; set; } = 300;
    public int BufferSize { get; set; } = 8192;
}

/// <summary>
/// Security settings
/// </summary>
public class SecuritySettings
{
    public bool EncryptCredentials { get; set; } = true;
    public bool RequireSecureTransfer { get; set; } = true;
    public List<string> AllowedCipherSuites { get; set; } = new() { "aes256-gcm", "aes128-gcm" };
    public string MinTlsVersion { get; set; } = "1.2";
    public bool EnableAuditLog { get; set; } = true;
    public string AuditLogPath { get; set; } = @"C:\FileRelay\logs\audit.log";
    public bool SensitiveDataMasking { get; set; } = true;
    public bool AllowExecutableFiles { get; set; } = true;
    public bool AllowHiddenFiles { get; set; } = false;
    public long MaxUploadSizeBytes { get; set; } = 5L * 1024 * 1024 * 1024; // 5GB
}
