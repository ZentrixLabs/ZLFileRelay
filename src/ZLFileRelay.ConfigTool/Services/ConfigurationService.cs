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
            _configPath = $@"\\{serverName}\c$\Program Files\ZLFileRelay\appsettings.json";
            
            _logger.LogInformation("Remote mode enabled, using UNC path: {Path}", _configPath);
        }
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
            // Validate configuration first
            var validationResult = await ValidateAsync(configuration);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Configuration validation failed: {Errors}", 
                    string.Join(", ", validationResult.Errors));
                return false;
            }

            // Backup existing config
            if (File.Exists(_configPath))
            {
                var backupPath = $"{_configPath}.{DateTime.Now:yyyyMMddHHmmss}.bak";
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

            var json = JsonSerializer.Serialize(wrapper, options);
            await File.WriteAllTextAsync(_configPath, json);

            _currentConfiguration = configuration;
            _logger.LogInformation("Configuration saved successfully to {Path}", _configPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration to {Path}", _configPath);
            return false;
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

        return Task.FromResult(new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        });
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

