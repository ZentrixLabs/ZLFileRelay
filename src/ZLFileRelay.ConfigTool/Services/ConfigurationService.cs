using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZLFileRelay.Core.Models;
using ZLFileRelay.ConfigTool.Interfaces;

namespace ZLFileRelay.ConfigTool.Services;

public class ConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly IRemoteServerProvider _remoteServerProvider;
    private string _configPath;
    private ZLFileRelayConfiguration? _currentConfiguration;

    public ConfigurationService(
        ILogger<ConfigurationService> logger,
        IRemoteServerProvider remoteServerProvider)
    {
        _logger = logger;
        _remoteServerProvider = remoteServerProvider;
        _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        
        // Subscribe to server changes
        _remoteServerProvider.ServerChanged += OnServerChanged;
        UpdateConfigPath();
    }

    private void OnServerChanged(object? sender, EventArgs e)
    {
        UpdateConfigPath();
        _logger.LogInformation("Server changed to {Server}, config path updated", 
            _remoteServerProvider.ServerName ?? "localhost");
    }

    private void UpdateConfigPath()
    {
        if (!_remoteServerProvider.IsRemote || string.IsNullOrWhiteSpace(_remoteServerProvider.ServerName))
        {
            // Local mode
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        }
        else
        {
            // Remote mode - try standard installation path
            var serverName = _remoteServerProvider.ServerName;
            
            // SECURITY FIX (HIGH-3): Validate server name to prevent path traversal
            if (!IsValidServerName(serverName))
            {
                _logger.LogError("Invalid server name format: {ServerName}. Server names must be valid hostnames or IP addresses without path traversal characters.", serverName);
                throw new ArgumentException($"Invalid server name format: {serverName}. Please use a valid hostname or IP address.");
            }
            
            _configPath = $@"\\{serverName}\c$\Program Files\ZLFileRelay\appsettings.json";
            
            _logger.LogInformation("Remote mode enabled, using UNC path: {Path}", _configPath);
        }
    }

    /// <summary>
    /// Validates that a server name is a valid hostname or IP address.
    /// Prevents path traversal and injection attacks.
    /// </summary>
    private static bool IsValidServerName(string serverName)
    {
        if (string.IsNullOrWhiteSpace(serverName))
            return false;

        // Check for path traversal characters
        if (serverName.Contains("..") || 
            serverName.Contains("/") || 
            serverName.Contains("\\") ||
            serverName.Contains("$"))
        {
            return false;
        }

        // Check for alternate data streams or special characters
        if (serverName.Contains(":"))
        {
            // Colons are only allowed in IPv6 addresses
            // If it contains a colon, it must be a valid IPv6 address
            if (!System.Net.IPAddress.TryParse(serverName, out var ipAddress) ||
                ipAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                return false;
            }
            return true;
        }

        // Try to parse as IPv4 address
        if (System.Net.IPAddress.TryParse(serverName, out var ipv4Address) &&
            ipv4Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return true;
        }

        // Validate as DNS hostname (including NetBIOS names)
        // Allow: letters, digits, hyphens, dots
        // Must start with alphanumeric
        // Segments between dots must be 1-63 characters
        var hostnamePattern = @"^[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?)*$";
        
        if (System.Text.RegularExpressions.Regex.IsMatch(serverName, hostnamePattern))
        {
            // Additional check: hostname should not be longer than 253 characters
            if (serverName.Length <= 253)
            {
                return true;
            }
        }

        return false;
    }

    public async Task<ZLFileRelayConfiguration> LoadAsync()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                _logger.LogWarning("Configuration file not found: {Path}. Using defaults.", _configPath);
                _currentConfiguration = GetDefaultConfiguration();
                return _currentConfiguration;
            }

            var json = await File.ReadAllTextAsync(_configPath);
            var root = JsonDocument.Parse(json);
            
            _currentConfiguration = new ZLFileRelayConfiguration();
            
            if (root.RootElement.TryGetProperty("ZLFileRelay", out var zlSection))
            {
                _currentConfiguration = JsonSerializer.Deserialize<ZLFileRelayConfiguration>(
                    zlSection.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            _logger.LogInformation("Configuration loaded successfully from {Path}", _configPath);
            return _currentConfiguration ?? GetDefaultConfiguration();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration from {Path}", _configPath);
            return GetDefaultConfiguration();
        }
    }

    public async Task<bool> SaveAsync(ZLFileRelayConfiguration configuration)
    {
        try
        {
            _logger.LogInformation("Starting configuration save to {Path}", _configPath);

            // Validate configuration first
            var validationResult = await ValidateAsync(configuration);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join(", ", validationResult.Errors);
                _logger.LogWarning("Configuration validation failed: {Errors}", errorMessage);
                throw new InvalidOperationException($"Configuration validation failed: {errorMessage}");
            }

            // Ensure directory exists
            var configDirectory = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(configDirectory) && !Directory.Exists(configDirectory))
            {
                _logger.LogInformation("Creating configuration directory: {Directory}", configDirectory);
                Directory.CreateDirectory(configDirectory);
            }

            // Backup existing config
            if (File.Exists(_configPath))
            {
                var backupPath = $"{_configPath}.{DateTime.Now:yyyyMMddHHmmss}.bak";
                _logger.LogInformation("Creating backup: {BackupPath}", backupPath);
                File.Copy(_configPath, backupPath, true);
                _logger.LogInformation("Created configuration backup: {BackupPath}", backupPath);
            }

            // Create wrapper object with ZLFileRelay section
            var wrapper = new { ZLFileRelay = configuration };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            _logger.LogDebug("Serializing configuration to JSON");
            var json = JsonSerializer.Serialize(wrapper, options);
            
            _logger.LogInformation("Writing configuration to {Path} ({Size} bytes)", _configPath, json.Length);
            await File.WriteAllTextAsync(_configPath, json);

            _currentConfiguration = configuration;
            _logger.LogInformation("Configuration saved successfully to {Path}", _configPath);
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Permission denied saving configuration to {Path}", _configPath);
            throw;
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogError(ex, "Directory not found for configuration path {Path}", _configPath);
            throw;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error saving configuration to {Path}", _configPath);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON serialization error saving configuration");
            throw new InvalidOperationException("Failed to serialize configuration to JSON", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration to {Path}", _configPath);
            throw;
        }
    }

    public Task<ValidationResult> ValidateAsync(ZLFileRelayConfiguration configuration)
    {
        var errors = new List<string>();

        // Validate paths
        if (string.IsNullOrWhiteSpace(configuration.Paths.UploadDirectory))
            errors.Add("Upload directory is required");

        if (string.IsNullOrWhiteSpace(configuration.Paths.LogDirectory))
            errors.Add("Log directory is required");

        if (string.IsNullOrWhiteSpace(configuration.Service.WatchDirectory))
            errors.Add("Watch directory is required");

        // Validate SSH settings if SSH is enabled
        if (configuration.Service.TransferMethod.Equals("ssh", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(configuration.Transfer.Ssh.Host))
                errors.Add("SSH host is required when using SSH transfer method");

            if (configuration.Transfer.Ssh.Port <= 0 || configuration.Transfer.Ssh.Port > 65535)
                errors.Add("SSH port must be between 1 and 65535");

            if (string.IsNullOrWhiteSpace(configuration.Transfer.Ssh.Username))
                errors.Add("SSH username is required");

            if (string.IsNullOrWhiteSpace(configuration.Transfer.Ssh.PrivateKeyPath))
                errors.Add("SSH private key path is required");
        }

        // Validate SMB settings if SMB is enabled
        if (configuration.Service.TransferMethod.Equals("smb", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(configuration.Transfer.Smb.SharePath))
                errors.Add("SMB share path is required when using SMB transfer method");
        }

        // Validate Web Portal settings
        if (configuration.WebPortal.Enabled)
        {
            if (configuration.WebPortal.Kestrel.HttpPort <= 0 || configuration.WebPortal.Kestrel.HttpPort > 65535)
                errors.Add("Web Portal HTTP port must be between 1 and 65535");

            if (configuration.WebPortal.Kestrel.HttpsPort <= 0 || configuration.WebPortal.Kestrel.HttpsPort > 65535)
                errors.Add("Web Portal HTTPS port must be between 1 and 65535");

            if (configuration.WebPortal.Kestrel.HttpPort == configuration.WebPortal.Kestrel.HttpsPort)
                errors.Add("Web Portal HTTP and HTTPS ports must be different");

            if (configuration.WebPortal.Kestrel.EnableHttps && string.IsNullOrWhiteSpace(configuration.WebPortal.Kestrel.CertificatePath))
                errors.Add("Certificate path is required when HTTPS is enabled for Web Portal");

            if (configuration.WebPortal.Kestrel.EnableHttps && 
                !string.IsNullOrWhiteSpace(configuration.WebPortal.Kestrel.CertificatePath) && 
                !File.Exists(configuration.WebPortal.Kestrel.CertificatePath))
                errors.Add($"Certificate file not found: {configuration.WebPortal.Kestrel.CertificatePath}");
        }

        // Validate Branding settings
        if (string.IsNullOrWhiteSpace(configuration.Branding.CompanyName))
            errors.Add("Company name is required");

        if (string.IsNullOrWhiteSpace(configuration.Branding.SiteName))
            errors.Add("Site name is required");

        // Logo is optional - don't validate file existence to avoid blocking saves from other tabs

        return Task.FromResult(new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        });
    }

    /// <summary>
    /// Validates only basic configuration integrity without requiring values from other tabs.
    /// Used for tab-specific saves to allow partial configuration.
    /// </summary>
    public Task<ValidationResult> ValidateBasicAsync(ZLFileRelayConfiguration configuration)
    {
        var errors = new List<string>();

        // Only validate basic integrity, not completeness
        // Allow partial configuration to be saved
        
        // Validate file paths that exist (if specified)
        if (!string.IsNullOrWhiteSpace(configuration.Transfer.Ssh.PrivateKeyPath) && 
            !File.Exists(configuration.Transfer.Ssh.PrivateKeyPath))
            errors.Add($"SSH private key file not found: {configuration.Transfer.Ssh.PrivateKeyPath}");

        // Note: Certificate and Logo validation removed from basic validation
        // They belong to Web Portal tab and should only validate on that tab

        // Validate port ranges if ports are specified
        if (configuration.Transfer.Ssh.Port < 0 || configuration.Transfer.Ssh.Port > 65535)
            errors.Add("SSH port must be between 0 and 65535");

        if (configuration.WebPortal.Kestrel.HttpPort < 0 || configuration.WebPortal.Kestrel.HttpPort > 65535)
            errors.Add("Web Portal HTTP port must be between 0 and 65535");

        if (configuration.WebPortal.Kestrel.HttpsPort < 0 || configuration.WebPortal.Kestrel.HttpsPort > 65535)
            errors.Add("Web Portal HTTPS port must be between 0 and 65535");

        if (configuration.WebPortal.Kestrel.HttpPort > 0 && configuration.WebPortal.Kestrel.HttpsPort > 0 &&
            configuration.WebPortal.Kestrel.HttpPort == configuration.WebPortal.Kestrel.HttpsPort)
            errors.Add("Web Portal HTTP and HTTPS ports must be different");

        return Task.FromResult(new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        });
    }

    public string GetConfigurationPath()
    {
        return _configPath;
    }

    public ZLFileRelayConfiguration GetDefaultConfiguration()
    {
        return new ZLFileRelayConfiguration
        {
            Branding = new BrandingSettings
            {
                CompanyName = "Your Company",
                ProductName = "ZL File Relay",
                SiteName = "Main Site",
                SupportEmail = "support@example.com"
            },
            Paths = new PathSettings
            {
                UploadDirectory = @"C:\FileRelay\uploads",
                TransferDirectory = @"C:\FileRelay\uploads\transfer",
                LogDirectory = @"C:\FileRelay\logs",
                ConfigDirectory = @"C:\ProgramData\ZLFileRelay"
            },
            Service = new ServiceSettings
            {
                WatchDirectory = @"C:\FileRelay\uploads\transfer",
                TransferMethod = "ssh",
                RetryAttempts = 3,
                RetryDelaySeconds = 30
            },
            Transfer = new TransferSettings
            {
                Ssh = new SshSettings
                {
                    Port = 22,
                    AuthMethod = "PublicKey"
                }
            }
        };
    }

    public ZLFileRelayConfiguration? CurrentConfiguration => _currentConfiguration;
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

