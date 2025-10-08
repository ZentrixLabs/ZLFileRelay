using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZLFileRelay.ConfigTool.Services;
using ZLFileRelay.Core.Models;

namespace ZLFileRelay.ConfigTool.ViewModels;

public partial class ConfigurationViewModel : ObservableObject
{
    private readonly ConfigurationService _configurationService;
    private readonly ConnectionTester _connectionTester;
    private ZLFileRelayConfiguration _config;

    // Basic Settings
    [ObservableProperty] private string _sourcePath = string.Empty;
    [ObservableProperty] private string _destinationPath = string.Empty;
    [ObservableProperty] private int _fileStabilitySeconds = 5;
    [ObservableProperty] private int _processingIntervalSeconds = 10;
    [ObservableProperty] private bool _deleteSourceAfterTransfer = true;
    [ObservableProperty] private int _maxQueueSize = 10000;
    [ObservableProperty] private double _minFreeDiskSpaceGB = 1.0;
    [ObservableProperty] private bool _checkDiskSpace = true;

    // Advanced Settings
    [ObservableProperty] private bool _verifyFileSize = true;
    [ObservableProperty] private bool _createDestinationDirectories = true;
    [ObservableProperty] private bool _includeSubdirectories = true;
    [ObservableProperty] private string _conflictResolution = "Append";
    
    // Security Settings
    [ObservableProperty] private bool _allowExecutableFiles = true;
    [ObservableProperty] private bool _allowHiddenFiles = false;
    [ObservableProperty] private double _maxFileSizeGB = 5.0;

    // Status
    [ObservableProperty] private string _connectionTestResult = string.Empty;

    public ConfigurationViewModel(
        ConfigurationService configurationService,
        ConnectionTester connectionTester)
    {
        _configurationService = configurationService;
        _connectionTester = connectionTester;
        _config = configurationService.CurrentConfiguration ?? configurationService.GetDefaultConfiguration();
        
        LoadFromConfiguration();
    }

    private void LoadFromConfiguration()
    {
        // Load values from configuration
        SourcePath = _config.Service.WatchDirectory;
        // Note: Destination will depend on transfer method (SSH or SMB)
        
        // For now, basic loading - we'll expand this
    }

    [RelayCommand]
    private async Task SaveConfigurationAsync()
    {
        try
        {
            // Update configuration from UI values
            _config.Service.WatchDirectory = SourcePath;
            // ... update other values

            var success = await _configurationService.SaveAsync(_config);
            ConnectionTestResult = success 
                ? "✅ Configuration saved successfully"
                : "❌ Failed to save configuration";
        }
        catch (Exception ex)
        {
            ConnectionTestResult = $"❌ Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        try
        {
            ConnectionTestResult = "Testing connection...";
            
            if (_config.Service.TransferMethod.Equals("ssh", StringComparison.OrdinalIgnoreCase))
            {
                var result = await _connectionTester.TestSshAsync(_config.Transfer.Ssh);
                ConnectionTestResult = result.Success 
                    ? $"✅ {result.Message}" 
                    : $"❌ {result.Message}";
            }
            else
            {
                var result = await _connectionTester.TestSmbAsync(_config.Transfer.Smb);
                ConnectionTestResult = result.Success 
                    ? $"✅ {result.Message}" 
                    : $"❌ {result.Message}";
            }
        }
        catch (Exception ex)
        {
            ConnectionTestResult = $"❌ Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenConfigFolder()
    {
        var configPath = Path.GetDirectoryName(_configurationService.CurrentConfiguration?.Paths.ConfigDirectory 
            ?? @"C:\ProgramData\ZLFileRelay");
        
        if (!string.IsNullOrEmpty(configPath) && Directory.Exists(configPath))
        {
            System.Diagnostics.Process.Start("explorer.exe", configPath);
        }
    }
}

