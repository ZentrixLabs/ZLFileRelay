using System.IO;

namespace ZLFileRelay.Core.Constants;

/// <summary>
/// Application-wide constants
/// </summary>
public static class ApplicationConstants
{
    public const string ApplicationName = "ZLFileRelay";
    public const string DisplayName = "ZL File Relay";
    public const string CompanyName = "ZentrixLabs";
    public const string Version = "2.0.0";
    
    public static class Configuration
    {
        public const string SectionName = "ZLFileRelay";
        public const string DefaultConfigFileName = "appsettings.json";
        public const string DefaultConfigDirectory = @"C:\ProgramData\ZLFileRelay";
        public static string SharedConfigPath => Path.Combine(DefaultConfigDirectory, DefaultConfigFileName);
    }
    
    public static class Paths
    {
        public const string DefaultInstallPath = @"C:\Program Files\ZLFileRelay";
        public const string DefaultUploadDirectory = @"C:\FileRelay\uploads";
        public const string DefaultLogDirectory = @"C:\FileRelay\logs";
        public const string DefaultArchiveDirectory = @"C:\FileRelay\archive";
        public const string DefaultTempDirectory = @"C:\FileRelay\temp";
    }
    
    public static class Service
    {
        public const string ServiceName = "ZLFileRelay";
        public const string DisplayName = "ZL File Relay Service";
        public const string Description = "Automated file transfer from DMZ to SCADA networks";
        public const string EventLogSource = "ZLFileRelay";
    }
    
    public static class WebPortal
    {
        public const string ApplicationPoolName = "ZLFileRelay";
        public const string SiteName = "ZLFileRelay";
        public const int DefaultHttpsPort = 443;
    }
    
    public static class Transfer
    {
        public const string MethodSsh = "ssh";
        public const string MethodSmb = "smb";
        public const int DefaultSshPort = 22;
        public const int DefaultConnectionTimeout = 30;
        public const int DefaultOperationTimeout = 300;
    }
    
    public static class Security
    {
        public const string CredentialStoreName = "credentials";
        public const string SshKeyDirectory = "ssh";
        public const string DefaultPrivateKeyName = "id_ed25519";
        public const string DefaultPublicKeyName = "id_ed25519.pub";
    }
    
    public static class Logging
    {
        public const string DefaultLogFileName = "log-.txt";
        public const string AuditLogFileName = "audit-.txt";
        public const int DefaultRetentionDays = 30;
        public const int DefaultMaxFileSizeMB = 100;
    }
}
