using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using ZLFileRelay.ConfigTool.Services;
using ZLFileRelay.Core.Models;

namespace ZLFileRelay.ConfigTool.ViewModels;

/// <summary>
/// Unified view model for File Transfer configuration combining directory settings, 
/// SSH/SMB configuration, file handling, and security policies
/// </summary>
public partial class FileTransferViewModel : ObservableObject
{
    private readonly ConfigurationService _configurationService;
    private readonly SshKeyGenerator _sshKeyGenerator;
    private readonly ConnectionTester _connectionTester;
    private ZLFileRelayConfiguration _config;

    // Transfer Method
    [ObservableProperty] private bool _isSshMethod = true;
    [ObservableProperty] private bool _isSmbMethod = false;

    // Directories
    [ObservableProperty] private string _watchDirectory = @"C:\FileRelay\uploads\transfer";
    [ObservableProperty] private string _archiveDirectory = @"C:\FileRelay\archive";

    // SSH Configuration
    [ObservableProperty] private string _sshHost = "";
    [ObservableProperty] private int _sshPort = 22;
    [ObservableProperty] private string _sshUsername = "";
    [ObservableProperty] private string _sshDestinationPath = "/data/incoming";
    [ObservableProperty] private string _sshKeyPath = "";
    [ObservableProperty] private string? _publicKey;
    [ObservableProperty] private string _sshTestResult = "Click 'Test SSH Connection' to verify your settings";

    // SMB Configuration
    [ObservableProperty] private string _smbServer = "";
    [ObservableProperty] private string _smbSharePath = "";

    // File Handling
    [ObservableProperty] private bool _archiveAfterTransfer = true;
    [ObservableProperty] private bool _deleteAfterTransfer = false;
    [ObservableProperty] private bool _verifyTransfer = true;

    // Security Policies
    [ObservableProperty] private bool _allowExecutableFiles = false;
    [ObservableProperty] private bool _allowHiddenFiles = false;
    [ObservableProperty] private double _maxFileSizeGB = 5.0;

    // Status
    [ObservableProperty] private string _statusMessage = "";

    public FileTransferViewModel(
        ConfigurationService configurationService,
        SshKeyGenerator sshKeyGenerator,
        ConnectionTester connectionTester)
    {
        _configurationService = configurationService;
        _sshKeyGenerator = sshKeyGenerator;
        _connectionTester = connectionTester;
        _config = configurationService.CurrentConfiguration ?? configurationService.GetDefaultConfiguration();

        _ = LoadConfigurationAsync();
    }

    partial void OnIsSshMethodChanged(bool value)
    {
        if (value)
        {
            IsSmbMethod = false;
        }
    }

    partial void OnIsSmbMethodChanged(bool value)
    {
        if (value)
        {
            IsSshMethod = false;
        }
    }

    private async Task LoadConfigurationAsync()
    {
        try
        {
            _config = await _configurationService.LoadAsync();
            LoadFromConfiguration();
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error loading configuration: {ex.Message}";
        }
    }

    private void LoadFromConfiguration()
    {
        // Transfer method
        IsSshMethod = _config.Service.TransferMethod.Equals("ssh", StringComparison.OrdinalIgnoreCase);
        IsSmbMethod = !IsSshMethod;

        // Directories
        WatchDirectory = _config.Service.WatchDirectory;
        ArchiveDirectory = _config.Service.ArchiveDirectory;

        // SSH
        SshHost = _config.Transfer.Ssh.Host ?? "";
        SshPort = _config.Transfer.Ssh.Port;
        SshUsername = _config.Transfer.Ssh.Username ?? "";
        SshDestinationPath = _config.Transfer.Ssh.DestinationPath ?? "/data/incoming";
        SshKeyPath = _config.Transfer.Ssh.PrivateKeyPath ?? "";

        // SMB
        SmbServer = _config.Transfer.Smb.Server ?? "";
        SmbSharePath = _config.Transfer.Smb.SharePath ?? "";

        // File Handling
        ArchiveAfterTransfer = _config.Service.ArchiveAfterTransfer;
        DeleteAfterTransfer = _config.Service.DeleteAfterTransfer;
        VerifyTransfer = _config.Service.VerifyTransfer;

        // Security
        AllowExecutableFiles = _config.Security.AllowExecutableFiles;
        AllowHiddenFiles = _config.Security.AllowHiddenFiles;
        MaxFileSizeGB = _config.Security.MaxUploadSizeBytes / (1024.0 * 1024.0 * 1024.0);
    }

    private void UpdateConfiguration()
    {
        // Transfer method
        _config.Service.TransferMethod = IsSshMethod ? "ssh" : "smb";

        // Directories
        _config.Service.WatchDirectory = WatchDirectory;
        _config.Service.ArchiveDirectory = ArchiveDirectory;

        // SSH
        _config.Transfer.Ssh.Host = SshHost;
        _config.Transfer.Ssh.Port = SshPort;
        _config.Transfer.Ssh.Username = SshUsername;
        _config.Transfer.Ssh.DestinationPath = SshDestinationPath;
        _config.Transfer.Ssh.PrivateKeyPath = SshKeyPath;

        // SMB
        _config.Transfer.Smb.Server = SmbServer;
        _config.Transfer.Smb.SharePath = SmbSharePath;

        // File Handling
        _config.Service.ArchiveAfterTransfer = ArchiveAfterTransfer;
        _config.Service.DeleteAfterTransfer = DeleteAfterTransfer;
        _config.Service.VerifyTransfer = VerifyTransfer;

        // Security
        _config.Security.AllowExecutableFiles = AllowExecutableFiles;
        _config.Security.AllowHiddenFiles = AllowHiddenFiles;
        _config.Security.MaxUploadSizeBytes = (long)(MaxFileSizeGB * 1024 * 1024 * 1024);
    }

    #region Directory Commands

    [RelayCommand]
    private void BrowseWatchDirectory()
    {
        var path = SelectFolder("Select Watch Directory", WatchDirectory);
        if (path != null) WatchDirectory = path;
    }

    [RelayCommand]
    private void BrowseArchiveDirectory()
    {
        var path = SelectFolder("Select Archive Directory", ArchiveDirectory);
        if (path != null) ArchiveDirectory = path;
    }

    private string? SelectFolder(string title, string initialDirectory)
    {
        // Use SaveFileDialog trick for folder selection (WPF standard approach)
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = title,
            FileName = "Select Folder",
            Filter = "Folder|*.folder",
            CheckFileExists = false,
            InitialDirectory = initialDirectory
        };

        if (dialog.ShowDialog() == true)
        {
            return Path.GetDirectoryName(dialog.FileName);
        }

        return null;
    }

    #endregion

    #region SSH Commands

    [RelayCommand]
    private void BrowseSshKey()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select SSH Private Key",
            Filter = "All Files (*.*)|*.*|SSH Keys (id_*)|id_*",
            DefaultExt = ""
        };

        if (dialog.ShowDialog() == true)
        {
            SshKeyPath = dialog.FileName;
        }
    }

    [RelayCommand]
    private async Task GenerateSshKeyAsync()
    {
        try
        {
            SshTestResult = "Generating SSH key pair (ED25519)...";

            var keyPath = Path.Combine(
                _config.Paths.ConfigDirectory ?? @"C:\ProgramData\ZLFileRelay",
                "zlrelay_key");

            // Ensure directory exists
            var directory = Path.GetDirectoryName(keyPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var keyPair = await _sshKeyGenerator.GenerateAsync(SshKeyType.ED25519, keyPath);

            SshKeyPath = keyPair.PrivateKeyPath;
            PublicKey = keyPair.PublicKey;

            SshTestResult = $"✅ SSH key pair generated successfully\n\n" +
                           $"Private key: {keyPair.PrivateKeyPath}\n" +
                           $"Public key: {keyPair.PublicKeyPath}\n\n" +
                           $"📋 Copy this public key to your server's ~/.ssh/authorized_keys file:\n\n" +
                           $"{PublicKey}";
        }
        catch (Exception ex)
        {
            SshTestResult = $"❌ Error generating SSH keys: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ViewPublicKeyAsync()
    {
        if (string.IsNullOrWhiteSpace(SshKeyPath))
        {
            SshTestResult = "❌ No private key path configured";
            return;
        }

        var publicKey = await _sshKeyGenerator.GetPublicKeyAsync(SshKeyPath);
        if (publicKey != null)
        {
            PublicKey = publicKey;
            SshTestResult = $"📋 Public Key:\n\n{publicKey}\n\nCopy this to your server's ~/.ssh/authorized_keys file";
        }
        else
        {
            SshTestResult = "❌ Could not read public key. Make sure the private key file exists.";
        }
    }

    [RelayCommand]
    private void CopyPublicKey()
    {
        if (!string.IsNullOrWhiteSpace(PublicKey))
        {
            System.Windows.Clipboard.SetText(PublicKey);
            SshTestResult = "✅ Public key copied to clipboard";
        }
        else
        {
            SshTestResult = "❌ No public key available. Generate or view a key first.";
        }
    }

    [RelayCommand]
    private async Task TestSshConnectionAsync()
    {
        try
        {
            SshTestResult = "Testing SSH connection...\nPlease wait...";

            // Update config with current UI values
            _config.Transfer.Ssh.Host = SshHost;
            _config.Transfer.Ssh.Port = SshPort;
            _config.Transfer.Ssh.Username = SshUsername;
            _config.Transfer.Ssh.PrivateKeyPath = SshKeyPath;
            _config.Transfer.Ssh.DestinationPath = SshDestinationPath;

            var result = await _connectionTester.TestSshAsync(_config.Transfer.Ssh);

            if (result.Success)
            {
                SshTestResult = $"✅ {result.Message}\n\n{result.Details}";
            }
            else
            {
                SshTestResult = $"❌ {result.Message}\n\n{result.Details}";
            }
        }
        catch (Exception ex)
        {
            SshTestResult = $"❌ Error: {ex.Message}";
        }
    }

    #endregion

    #region SMB Commands

    [RelayCommand]
    private async Task TestSmbConnectionAsync()
    {
        try
        {
            StatusMessage = "Testing SMB connection...";

            // Update config with current UI values
            _config.Transfer.Smb.Server = SmbServer;
            _config.Transfer.Smb.SharePath = SmbSharePath;

            var result = await _connectionTester.TestSmbAsync(_config.Transfer.Smb);

            StatusMessage = result.Success 
                ? $"✅ {result.Message}" 
                : $"❌ {result.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error: {ex.Message}";
        }
    }

    #endregion

    #region Security Commands

    [RelayCommand]
    private void SetMaxSize(string sizeGB)
    {
        if (double.TryParse(sizeGB, out var size))
        {
            MaxFileSizeGB = size;
        }
    }

    #endregion

    #region Save Configuration

    [RelayCommand]
    private async Task SaveConfigurationAsync()
    {
        try
        {
            UpdateConfiguration();

            // Validate before saving
            var validationResult = await _configurationService.ValidateAsync(_config);
            
            if (!validationResult.IsValid)
            {
                StatusMessage = $"❌ Validation failed:\n{string.Join("\n", validationResult.Errors)}";
                return;
            }

            var success = await _configurationService.SaveAsync(_config);
            StatusMessage = success 
                ? "✅ Configuration saved successfully. Restart the service for changes to take effect."
                : "❌ Failed to save configuration";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error: {ex.Message}";
        }
    }

    #endregion
}

