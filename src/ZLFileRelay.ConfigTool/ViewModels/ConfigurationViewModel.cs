using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using ZLFileRelay.ConfigTool.Services;
using ZLFileRelay.Core.Models;

namespace ZLFileRelay.ConfigTool.ViewModels;

public partial class ConfigurationViewModel : ObservableObject
{
    private readonly ConfigurationService _configurationService;
    private readonly ConnectionTester _connectionTester;
    private ZLFileRelayConfiguration _config;

    // Branding Settings
    [ObservableProperty] private string _companyName = "Your Company";
    [ObservableProperty] private string _productName = "ZL File Relay";
    [ObservableProperty] private string _siteName = "Main Site";
    [ObservableProperty] private string _supportEmail = "support@example.com";
    [ObservableProperty] private string _logoPath = string.Empty;
    [ObservableProperty] private string _primaryColor = "#0066CC";
    [ObservableProperty] private string _secondaryColor = "#003366";
    [ObservableProperty] private string _accentColor = "#FF6600";

    // Path Settings
    [ObservableProperty] private string _uploadDirectory = @"C:\FileRelay\uploads";
    [ObservableProperty] private string _transferDirectory = @"C:\FileRelay\uploads\transfer";
    [ObservableProperty] private string _logDirectory = @"C:\FileRelay\logs";
    [ObservableProperty] private string _configDirectory = @"C:\ProgramData\ZLFileRelay";
    [ObservableProperty] private string _tempDirectory = @"C:\FileRelay\temp";
    [ObservableProperty] private string _archiveDirectory = @"C:\FileRelay\archive";

    // Service Settings
    [ObservableProperty] private bool _serviceEnabled = true;
    [ObservableProperty] private string _watchDirectory = @"C:\FileRelay\uploads\transfer";
    [ObservableProperty] private string _transferMethod = "ssh";
    [ObservableProperty] private int _retryAttempts = 3;
    [ObservableProperty] private int _retryDelaySeconds = 30;
    [ObservableProperty] private int _maxConcurrentTransfers = 5;
    [ObservableProperty] private int _fileStabilitySeconds = 5;
    [ObservableProperty] private int _processingIntervalSeconds = 10;
    [ObservableProperty] private int _maxQueueSize = 10000;
    [ObservableProperty] private bool _deleteAfterTransfer = false;
    [ObservableProperty] private bool _archiveAfterTransfer = true;
    [ObservableProperty] private bool _verifyTransfer = true;
    [ObservableProperty] private string _conflictResolution = "Append";
    [ObservableProperty] private bool _checkDiskSpace = true;
    [ObservableProperty] private double _minimumFreeDiskSpaceGB = 1.0;
    [ObservableProperty] private bool _includeSubdirectories = true;

    // Security Settings
    [ObservableProperty] private bool _allowExecutableFiles = true;
    [ObservableProperty] private bool _allowHiddenFiles = false;
    [ObservableProperty] private double _maxUploadSizeGB = 5.0;
    [ObservableProperty] private bool _enableAuditLog = true;
    [ObservableProperty] private string _auditLogPath = @"C:\FileRelay\logs\audit.log";

    // Logging Settings
    [ObservableProperty] private int _logRetentionDays = 30;
    [ObservableProperty] private int _maxLogFileSizeMB = 100;
    [ObservableProperty] private bool _enableEventLog = true;
    [ObservableProperty] private bool _enableConsole = false;

    // Status
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private ObservableCollection<string> _validationErrors = new();

    public ObservableCollection<string> TransferMethods { get; } = new() { "ssh", "smb" };
    public ObservableCollection<string> ConflictResolutions { get; } = new() { "Append", "Overwrite", "Skip" };

    public ConfigurationViewModel(
        ConfigurationService configurationService,
        ConnectionTester connectionTester)
    {
        _configurationService = configurationService;
        _connectionTester = connectionTester;
        _config = configurationService.CurrentConfiguration ?? configurationService.GetDefaultConfiguration();
        
        _ = LoadConfigurationAsync();
    }

    [RelayCommand]
    private async Task LoadConfigurationAsync()
    {
        try
        {
            _config = await _configurationService.LoadAsync();
            LoadFromConfiguration();
            StatusMessage = "✅ Configuration loaded successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error loading configuration: {ex.Message}";
        }
    }

    private void LoadFromConfiguration()
    {
        // Branding
        CompanyName = _config.Branding.CompanyName;
        ProductName = _config.Branding.ProductName;
        SiteName = _config.Branding.SiteName;
        SupportEmail = _config.Branding.SupportEmail;
        LogoPath = _config.Branding.LogoPath ?? string.Empty;
        PrimaryColor = _config.Branding.Theme.PrimaryColor;
        SecondaryColor = _config.Branding.Theme.SecondaryColor;
        AccentColor = _config.Branding.Theme.AccentColor;

        // Paths
        UploadDirectory = _config.Paths.UploadDirectory;
        TransferDirectory = _config.Paths.TransferDirectory;
        LogDirectory = _config.Paths.LogDirectory;
        ConfigDirectory = _config.Paths.ConfigDirectory;
        TempDirectory = _config.Paths.TempDirectory;
        ArchiveDirectory = _config.Service.ArchiveDirectory;

        // Service
        ServiceEnabled = _config.Service.Enabled;
        WatchDirectory = _config.Service.WatchDirectory;
        TransferMethod = _config.Service.TransferMethod;
        RetryAttempts = _config.Service.RetryAttempts;
        RetryDelaySeconds = _config.Service.RetryDelaySeconds;
        MaxConcurrentTransfers = _config.Service.MaxConcurrentTransfers;
        FileStabilitySeconds = _config.Service.FileStabilitySeconds;
        ProcessingIntervalSeconds = _config.Service.ProcessingIntervalSeconds;
        MaxQueueSize = _config.Service.MaxQueueSize;
        DeleteAfterTransfer = _config.Service.DeleteAfterTransfer;
        ArchiveAfterTransfer = _config.Service.ArchiveAfterTransfer;
        VerifyTransfer = _config.Service.VerifyTransfer;
        ConflictResolution = _config.Service.ConflictResolution;
        CheckDiskSpace = _config.Service.CheckDiskSpace;
        MinimumFreeDiskSpaceGB = _config.Service.MinimumFreeDiskSpaceBytes / (1024.0 * 1024.0 * 1024.0);
        IncludeSubdirectories = _config.Service.IncludeSubdirectories;

        // Security
        AllowExecutableFiles = _config.Security.AllowExecutableFiles;
        AllowHiddenFiles = _config.Security.AllowHiddenFiles;
        MaxUploadSizeGB = _config.Security.MaxUploadSizeBytes / (1024.0 * 1024.0 * 1024.0);
        EnableAuditLog = _config.Security.EnableAuditLog;
        AuditLogPath = _config.Security.AuditLogPath;

        // Logging
        LogRetentionDays = _config.Logging.RetentionDays;
        MaxLogFileSizeMB = _config.Logging.MaxFileSizeMB;
        EnableEventLog = _config.Logging.EnableEventLog;
        EnableConsole = _config.Logging.EnableConsole;
    }

    private void UpdateConfiguration()
    {
        // Branding
        _config.Branding.CompanyName = CompanyName;
        _config.Branding.ProductName = ProductName;
        _config.Branding.SiteName = SiteName;
        _config.Branding.SupportEmail = SupportEmail;
        _config.Branding.LogoPath = string.IsNullOrWhiteSpace(LogoPath) ? null : LogoPath;
        _config.Branding.Theme.PrimaryColor = PrimaryColor;
        _config.Branding.Theme.SecondaryColor = SecondaryColor;
        _config.Branding.Theme.AccentColor = AccentColor;

        // Paths
        _config.Paths.UploadDirectory = UploadDirectory;
        _config.Paths.TransferDirectory = TransferDirectory;
        _config.Paths.LogDirectory = LogDirectory;
        _config.Paths.ConfigDirectory = ConfigDirectory;
        _config.Paths.TempDirectory = TempDirectory;
        _config.Service.ArchiveDirectory = ArchiveDirectory;

        // Service
        _config.Service.Enabled = ServiceEnabled;
        _config.Service.WatchDirectory = WatchDirectory;
        _config.Service.TransferMethod = TransferMethod;
        _config.Service.RetryAttempts = RetryAttempts;
        _config.Service.RetryDelaySeconds = RetryDelaySeconds;
        _config.Service.MaxConcurrentTransfers = MaxConcurrentTransfers;
        _config.Service.FileStabilitySeconds = FileStabilitySeconds;
        _config.Service.ProcessingIntervalSeconds = ProcessingIntervalSeconds;
        _config.Service.MaxQueueSize = MaxQueueSize;
        _config.Service.DeleteAfterTransfer = DeleteAfterTransfer;
        _config.Service.ArchiveAfterTransfer = ArchiveAfterTransfer;
        _config.Service.VerifyTransfer = VerifyTransfer;
        _config.Service.ConflictResolution = ConflictResolution;
        _config.Service.CheckDiskSpace = CheckDiskSpace;
        _config.Service.MinimumFreeDiskSpaceBytes = (long)(MinimumFreeDiskSpaceGB * 1024 * 1024 * 1024);
        _config.Service.IncludeSubdirectories = IncludeSubdirectories;

        // Security
        _config.Security.AllowExecutableFiles = AllowExecutableFiles;
        _config.Security.AllowHiddenFiles = AllowHiddenFiles;
        _config.Security.MaxUploadSizeBytes = (long)(MaxUploadSizeGB * 1024 * 1024 * 1024);
        _config.Security.EnableAuditLog = EnableAuditLog;
        _config.Security.AuditLogPath = AuditLogPath;

        // Logging
        _config.Logging.RetentionDays = LogRetentionDays;
        _config.Logging.MaxFileSizeMB = MaxLogFileSizeMB;
        _config.Logging.EnableEventLog = EnableEventLog;
        _config.Logging.EnableConsole = EnableConsole;
    }

    [RelayCommand]
    private async Task SaveConfigurationAsync()
    {
        try
        {
            UpdateConfiguration();

            // Validate before saving
            var validationResult = await _configurationService.ValidateAsync(_config);
            ValidationErrors.Clear();
            
            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    ValidationErrors.Add(error);
                }
                StatusMessage = $"❌ Validation failed: {validationResult.Errors.Count} error(s)";
                return;
            }

            var success = await _configurationService.SaveAsync(_config);
            StatusMessage = success 
                ? "✅ Configuration saved successfully"
                : "❌ Failed to save configuration";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void BrowseDirectory(string propertyName)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Select Directory",
            FileName = "Select Folder",
            Filter = "Folder|*.folder",
            CheckFileExists = false
        };

        if (dialog.ShowDialog() == true)
        {
            var directory = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
            
            switch (propertyName)
            {
                case "Upload": UploadDirectory = directory; break;
                case "Transfer": TransferDirectory = directory; break;
                case "Log": LogDirectory = directory; break;
                case "Config": ConfigDirectory = directory; break;
                case "Temp": TempDirectory = directory; break;
                case "Archive": ArchiveDirectory = directory; break;
                case "Watch": WatchDirectory = directory; break;
                case "AuditLog": AuditLogPath = Path.Combine(directory, "audit.log"); break;
            }
        }
    }

    [RelayCommand]
    private void BrowseLogo()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Logo Image",
            Filter = "Image Files (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp|All Files (*.*)|*.*",
            DefaultExt = ".png"
        };

        if (dialog.ShowDialog() == true)
        {
            LogoPath = dialog.FileName;
        }
    }

    [RelayCommand]
    private async Task CreateDirectoriesAsync()
    {
        try
        {
            var directories = new[]
            {
                UploadDirectory,
                TransferDirectory,
                LogDirectory,
                ConfigDirectory,
                TempDirectory,
                ArchiveDirectory
            };

            int created = 0;
            foreach (var dir in directories)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    created++;
                }
            }

            StatusMessage = $"✅ Created {created} director{(created == 1 ? "y" : "ies")}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error creating directories: {ex.Message}";
        }

        await Task.CompletedTask;
    }

    [RelayCommand]
    private void OpenConfigFolder()
    {
        var configPath = ConfigDirectory;
        
        if (!string.IsNullOrEmpty(configPath))
        {
            if (!Directory.Exists(configPath))
            {
                Directory.CreateDirectory(configPath);
            }
            System.Diagnostics.Process.Start("explorer.exe", configPath);
        }
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        _config = _configurationService.GetDefaultConfiguration();
        LoadFromConfiguration();
        StatusMessage = "Configuration reset to defaults (not saved)";
    }

    [RelayCommand]
    private async Task ExportConfigurationAsync()
    {
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export Configuration",
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                DefaultExt = ".json",
                FileName = $"zlrelay-config-{DateTime.Now:yyyyMMdd-HHmmss}.json"
            };

            if (dialog.ShowDialog() == true)
            {
                UpdateConfiguration();
                var json = System.Text.Json.JsonSerializer.Serialize(
                    new { ZLFileRelay = _config },
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                
                await File.WriteAllTextAsync(dialog.FileName, json);
                StatusMessage = $"✅ Configuration exported to {dialog.FileName}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error exporting configuration: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ImportConfigurationAsync()
    {
        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import Configuration",
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                DefaultExt = ".json"
            };

            if (dialog.ShowDialog() == true)
            {
                var json = await File.ReadAllTextAsync(dialog.FileName);
                var root = System.Text.Json.JsonDocument.Parse(json);
                
                if (root.RootElement.TryGetProperty("ZLFileRelay", out var zlSection))
                {
                    _config = System.Text.Json.JsonSerializer.Deserialize<ZLFileRelayConfiguration>(
                        zlSection.GetRawText(),
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? _configurationService.GetDefaultConfiguration();
                    
                    LoadFromConfiguration();
                    StatusMessage = $"✅ Configuration imported from {dialog.FileName} (not saved)";
                }
                else
                {
                    StatusMessage = "❌ Invalid configuration file format";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error importing configuration: {ex.Message}";
        }
    }
}

