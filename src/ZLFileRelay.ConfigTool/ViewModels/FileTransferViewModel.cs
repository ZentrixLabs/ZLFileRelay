using System.IO;
using System;
using System.Windows;
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
    private readonly ServiceAccountImpersonator _impersonator;
    private readonly ServiceAccountManager _serviceAccountManager;
    private ZLFileRelayConfiguration _config;
    
    // Cached service account credentials for session
    private string? _cachedServiceAccountUsername;
    private string? _cachedServiceAccountPassword;
    private bool _promptedForCredentials;

    // Transfer Method
    [ObservableProperty] private bool _isSshMethod = true;
    [ObservableProperty] private bool _isSmbMethod = false;

    // Directories
    [ObservableProperty] private string _watchDirectory = @"C:\FileRelay\uploads\transfer";
    [ObservableProperty] private string _dmzUploadDirectory = @"C:\FileRelay\uploads\dmz";
    [ObservableProperty] private string _archiveDirectory = @"C:\FileRelay\archive";

    // SSH Configuration
    [ObservableProperty] private string _sshHost = "";
    [ObservableProperty] private int _sshPort = 22;
    [ObservableProperty] private string _sshUsername = "";
    [ObservableProperty] private string _sshDestinationPath = "/data/incoming";
    [ObservableProperty] private string _sshKeyPath = "";
    [ObservableProperty] private string? _publicKey;
    [ObservableProperty] private string _sshTestResult = "Click 'Test SSH Connection' to verify your settings";
    
    // SSH Advanced Settings
    [ObservableProperty] private bool _isRemoteServerWindows = true;
    [ObservableProperty] private bool _isRemoteServerLinux = false;
    [ObservableProperty] private bool _sshCompression = true;
    [ObservableProperty] private int _sshConnectionTimeout = 30;
    [ObservableProperty] private int _sshTransferTimeout = 300;
    [ObservableProperty] private bool _sshStrictHostKeyChecking = true;

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
        ConnectionTester connectionTester,
        ServiceAccountImpersonator impersonator,
        ServiceAccountManager serviceAccountManager)
    {
        _configurationService = configurationService;
        _sshKeyGenerator = sshKeyGenerator;
        _connectionTester = connectionTester;
        _impersonator = impersonator;
        _serviceAccountManager = serviceAccountManager;
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

    partial void OnIsRemoteServerWindowsChanged(bool value)
    {
        if (value)
        {
            IsRemoteServerLinux = false;
        }
    }

    partial void OnIsRemoteServerLinuxChanged(bool value)
    {
        if (value)
        {
            IsRemoteServerWindows = false;
        }
    }

    internal async Task LoadConfigurationAsync()
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
        DmzUploadDirectory = _config.WebPortal.DmzUploadDirectory ?? @"C:\FileRelay\uploads\dmz";
        ArchiveDirectory = _config.Service.ArchiveDirectory;

        // SSH
        SshHost = _config.Transfer.Ssh.Host ?? "";
        SshPort = _config.Transfer.Ssh.Port;
        SshUsername = _config.Transfer.Ssh.Username ?? "";
        SshDestinationPath = _config.Transfer.Ssh.DestinationPath ?? "/data/incoming";
        SshKeyPath = _config.Transfer.Ssh.PrivateKeyPath ?? "";
        
        // SSH Advanced
        IsRemoteServerWindows = _config.Transfer.Ssh.RemoteServerType?.Equals("Windows", StringComparison.OrdinalIgnoreCase) ?? true;
        IsRemoteServerLinux = _config.Transfer.Ssh.RemoteServerType?.Equals("Linux", StringComparison.OrdinalIgnoreCase) ?? false;
        SshCompression = _config.Transfer.Ssh.Compression;
        SshConnectionTimeout = _config.Transfer.Ssh.ConnectionTimeout;
        SshTransferTimeout = _config.Transfer.Ssh.TransferTimeout;
        SshStrictHostKeyChecking = _config.Transfer.Ssh.StrictHostKeyChecking;

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
        _config.WebPortal.DmzUploadDirectory = DmzUploadDirectory;
        _config.Service.ArchiveDirectory = ArchiveDirectory;

        // SSH
        _config.Transfer.Ssh.Host = SshHost;
        _config.Transfer.Ssh.Port = SshPort;
        _config.Transfer.Ssh.Username = SshUsername;
        _config.Transfer.Ssh.DestinationPath = SshDestinationPath;
        _config.Transfer.Ssh.PrivateKeyPath = SshKeyPath;
        
        // SSH Advanced
        _config.Transfer.Ssh.RemoteServerType = IsRemoteServerWindows ? "Windows" : "Linux";
        _config.Transfer.Ssh.Compression = SshCompression;
        _config.Transfer.Ssh.ConnectionTimeout = SshConnectionTimeout;
        _config.Transfer.Ssh.TransferTimeout = SshTransferTimeout;
        _config.Transfer.Ssh.StrictHostKeyChecking = SshStrictHostKeyChecking;

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
    private void BrowseDmzDirectory()
    {
        var path = SelectFolder("Select DMZ Upload Directory", DmzUploadDirectory);
        if (path != null) DmzUploadDirectory = path;
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

    /// <summary>
    /// Gets service account credentials, prompting the user if not cached.
    /// </summary>
    private async Task<(string? Username, string? Password)> GetServiceAccountCredentialsAsync()
    {
        // Return cached credentials if available
        if (_promptedForCredentials && 
            !string.IsNullOrWhiteSpace(_cachedServiceAccountUsername) && 
            !string.IsNullOrWhiteSpace(_cachedServiceAccountPassword))
        {
            return (_cachedServiceAccountUsername, _cachedServiceAccountPassword);
        }

        // Get current service account
        var currentServiceAccount = await _serviceAccountManager.GetCurrentServiceAccountAsync();
        if (string.IsNullOrWhiteSpace(currentServiceAccount) || 
            currentServiceAccount.Contains("LocalSystem") ||
            _impersonator.IsSystemAccount(currentServiceAccount))
        {
            // System accounts don't support impersonation
            return (null, null);
        }

        // Show credential dialog
        var dialog = new Views.ServiceAccountCredentialDialog(currentServiceAccount, currentServiceAccount);
        dialog.Owner = System.Windows.Application.Current.MainWindow;
        
        if (dialog.ShowDialog() == true)
        {
            _cachedServiceAccountUsername = dialog.Username;
            _cachedServiceAccountPassword = dialog.Password;
            _promptedForCredentials = true;
            
            // Only cache if remember checkbox was checked
            if (!dialog.RememberForSession)
            {
                _promptedForCredentials = false;
            }
            
            return (_cachedServiceAccountUsername, _cachedServiceAccountPassword);
        }

        return (null, null);
    }

    [RelayCommand]
    private async Task BrowseSshKey()
    {
        try
        {
            // Open file browser as admin (can browse anywhere)
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select SSH Private Key (NOT the .pub file)",
                Filter = "SSH Private Keys (*.key;*.pem;id_*;*_key)|*.key;*.pem;id_*;*_key|" +
                        "Key Files (*.key)|*.key|" +
                        "PEM Files (*.pem)|*.pem|" +
                        "All Files (*.*)|*.*",
                DefaultExt = "",
                FilterIndex = 1,
                CheckFileExists = true
            };

            if (dialog.ShowDialog() != true)
            {
                return; // User cancelled
            }

            var selectedFile = dialog.FileName;
            if (string.IsNullOrWhiteSpace(selectedFile))
            {
                return;
            }
                
            // Warn if they selected a .pub file by mistake
            if (selectedFile.EndsWith(".pub", StringComparison.OrdinalIgnoreCase))
            {
                SshTestResult = "⚠️ Warning: You selected a public key (.pub) file. " +
                              "Please select the PRIVATE key file (without .pub extension) instead.";
                return;
            }

            // Copy the key into the application config directory and secure it
            try
            {
                var currentServiceAccount = await _serviceAccountManager.GetCurrentServiceAccountAsync();
                var serviceAccountName = (currentServiceAccount != null && !_impersonator.IsSystemAccount(currentServiceAccount))
                    ? currentServiceAccount
                    : null;

                // Determine destination folder for managed keys
                var destDir = Path.Combine(_config.Paths.ConfigDirectory ?? @"C:\ProgramData\ZLFileRelay", "ssh_keys");
                Directory.CreateDirectory(destDir);

                // Preserve filename; ensure no overwrite by incrementing if needed
                var fileName = Path.GetFileName(selectedFile);
                var destPath = Path.Combine(destDir, fileName);
                if (File.Exists(destPath))
                {
                    var name = Path.GetFileNameWithoutExtension(fileName);
                    var ext = Path.GetExtension(fileName);
                    var i = 1;
                    while (File.Exists(destPath))
                    {
                        destPath = Path.Combine(destDir, $"{name}-{i}{ext}");
                        i++;
                    }
                }

                // If we have service account credentials, impersonate to ensure correct file ownership
                var (saUsername, saPassword) = await GetServiceAccountCredentialsAsync();
                if (!string.IsNullOrWhiteSpace(saUsername) && !string.IsNullOrWhiteSpace(saPassword) &&
                    !_impersonator.IsSystemAccount(saUsername))
                {
                    await _impersonator.ImpersonateAsync<object>(saUsername, saPassword, async () =>
                    {
                        File.Copy(selectedFile, destPath);
                        // Secure under impersonated context (owner will be service account)
                        await _sshKeyGenerator.SetSecureKeyPermissionsAsync(destPath, saUsername);
                        return new object();
                    });
                }
                else
                {
                    // Copy without impersonation, then set owner and ACL explicitly
                    File.Copy(selectedFile, destPath);
                    await _sshKeyGenerator.SetSecureKeyPermissionsAsync(destPath, serviceAccountName);
                }

                // Point configuration at the managed copy
                SshKeyPath = destPath;

                SshTestResult = $"✅ SSH private key imported to application directory:\n{destPath}\n\n" +
                                $"✅ Secure permissions configured (SYSTEM & Administrators: Full; {(serviceAccountName != null ? serviceAccountName + ": Read & Execute" : "")}).";

                // Show confirmation dialog so the action feels tangible
                var details =
                    $"The SSH private key has been imported and secured.\n\n" +
                    $"Imported to:\n{destPath}\n\n" +
                    $"Permissions:\n- SYSTEM: Full Control\n- Administrators: Full Control" +
                    (serviceAccountName != null ? $"\n- {serviceAccountName}: Read & Execute" : string.Empty);

                MessageBox.Show(
                    details,
                    "SSH Key Imported",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (UnauthorizedAccessException ex)
            {
                SshTestResult = $"❌ Permission denied importing SSH key: {ex.Message}\n\nRun the Config Tool as Administrator and try again.";
            }
            catch (Exception ex)
            {
                SshTestResult = $"❌ Error importing SSH key: {ex.Message}";
            }
            
            return;
        }
        catch (Exception ex)
        {
            SshTestResult = $"❌ Error browsing for SSH key: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task FixSshKeyPermissionsAsync()
    {
        if (string.IsNullOrWhiteSpace(SshKeyPath))
        {
            SshTestResult = "❌ No SSH private key path configured. Please select a key file first.";
            return;
        }

        if (!File.Exists(SshKeyPath))
        {
            SshTestResult = $"❌ SSH private key file not found: {SshKeyPath}";
            return;
        }

        try
        {
            SshTestResult = "Fixing SSH key permissions...";
            
            // Get current service account
            var currentServiceAccount = await _serviceAccountManager.GetCurrentServiceAccountAsync();
            var serviceAccountName = (currentServiceAccount != null && !_impersonator.IsSystemAccount(currentServiceAccount))
                ? currentServiceAccount
                : null;
            
            var success = await _sshKeyGenerator.FixKeyPermissionsAsync(SshKeyPath, serviceAccountName);
            
            if (success)
            {
                SshTestResult = $"✅ SSH key permissions fixed successfully!\n\n" +
                              $"Key file: {SshKeyPath}\n\n" +
                              $"The key now has secure permissions set:\n" +
                              $"• SYSTEM: Full Control\n" +
                              $"• Administrators: Full Control\n" +
                              $"{(serviceAccountName != null ? $"• {serviceAccountName}: Read & Execute\n" : "")}" +
                              $"• All other access removed\n\n" +
                              $"SSH should now accept this key file.";
            }
            else
            {
                SshTestResult = $"❌ Failed to fix SSH key permissions.\n\n" +
                              $"Key file: {SshKeyPath}\n\n" +
                              $"Please ensure:\n" +
                              $"• You are running as Administrator\n" +
                              $"• The file is not locked by another process\n" +
                              $"• You have permissions to modify the file\n\n" +
                              $"You can also fix permissions manually using PowerShell:\n" +
                              $"  icacls \"{SshKeyPath}\" /inheritance:r\n" +
                              $"  icacls \"{SshKeyPath}\" /grant \"SYSTEM:F\"\n" +
                              $"  icacls \"{SshKeyPath}\" /grant \"Administrators:F\"\n" +
                              $"{(serviceAccountName != null ? $"  icacls \"{SshKeyPath}\" /grant \"{serviceAccountName}:R\"\n" : "")}";
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            SshTestResult = $"❌ Permission denied fixing SSH key permissions.\n\n" +
                          $"Error: {ex.Message}\n\n" +
                          $"Please run the Config Tool as Administrator and try again.";
        }
        catch (Exception ex)
        {
            SshTestResult = $"❌ Error fixing SSH key permissions: {ex.Message}";
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

            // Get service account to grant it access to the key
            var currentServiceAccount = await _serviceAccountManager.GetCurrentServiceAccountAsync();
            var serviceAccountName = (currentServiceAccount != null && !_impersonator.IsSystemAccount(currentServiceAccount))
                ? currentServiceAccount
                : null;

            var keyPair = await _sshKeyGenerator.GenerateAsync(SshKeyType.ED25519, keyPath, null, serviceAccountName);

            SshKeyPath = keyPair.PrivateKeyPath;
            PublicKey = keyPair.PublicKey;

            // Note: Secure permissions are automatically set by SshKeyGenerator.GenerateAsync
            var serviceAccountInfo = (serviceAccountName != null)
                ? $"\n• Service account ({serviceAccountName}) has been granted read access"
                : "\n• Note: If using a service account, you may need to grant it read access";

            SshTestResult = $"✅ SSH key pair generated successfully (ED25519 format)\n\n" +
                           $"🔒 Private key (keep secure): {keyPair.PrivateKeyPath}\n" +
                           $"📤 Public key (deploy to remote): {keyPair.PublicKeyPath}\n\n" +
                           $"✅ Secure permissions have been automatically configured:\n" +
                           $"• SYSTEM: Full Control\n" +
                           $"• Administrators: Full Control{serviceAccountInfo}\n" +
                           $"• All other access removed (SSH requirement)\n\n" +
                           $"Next Steps:\n" +
                           $"1. The private key path has been automatically filled in above\n" +
                           $"2. Click 'View Public Key' or 'Copy to Clipboard' to get the public key\n" +
                           $"3. Add the public key to your remote server's ~/.ssh/authorized_keys file\n" +
                           $"4. Test the connection\n\n" +
                           $"⚠️ Important: Never share the private key file! Only deploy the public key (.pub) to remote servers.";
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
            var publicKeyPath = $"{SshKeyPath}.pub";
            SshTestResult = $"📋 Public Key (from {publicKeyPath}):\n\n{publicKey}\n\n" +
                           $"✅ Copy this entire text to your remote server's ~/.ssh/authorized_keys file\n\n" +
                           $"Tip: Use the 'Copy to Clipboard' button to copy this key.";
        }
        else
        {
            var publicKeyPath = $"{SshKeyPath}.pub";
            SshTestResult = $"❌ Could not read public key file: {publicKeyPath}\n\n" +
                           $"Make sure the private key file exists and the public key (.pub) was generated alongside it.";
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

            // Try to get service account credentials for impersonated testing
            var (saUsername, saPassword) = await GetServiceAccountCredentialsAsync();

            var result = await _connectionTester.TestSshAsync(_config.Transfer.Ssh, saUsername, saPassword);

            if (result.Success)
            {
                SshTestResult = $"✅ {result.Message}\n\n{result.Details}";
                
                // Save test result to configuration
                _config.Status.LastTestDate = DateTime.Now;
                _config.Status.LastTestResult = "Success";
                _config.Status.LastTestedMethod = "ssh";
                _config.Status.IsTestingComplete = true;
                
                // Auto-save test result status
                await _configurationService.SaveAsync(_config);
            }
            else
            {
                SshTestResult = $"❌ {result.Message}\n\n{result.Details}";
                
                // Save failed test result to configuration
                _config.Status.LastTestDate = DateTime.Now;
                _config.Status.LastTestResult = "Failed";
                _config.Status.LastTestedMethod = "ssh";
                _config.Status.IsTestingComplete = false;
                
                // Auto-save test result status
                await _configurationService.SaveAsync(_config);
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

            if (result.Success)
            {
                StatusMessage = $"✅ {result.Message}";
                
                // Save test result to configuration
                _config.Status.LastTestDate = DateTime.Now;
                _config.Status.LastTestResult = "Success";
                _config.Status.LastTestedMethod = "smb";
                _config.Status.IsTestingComplete = true;
                
                // Auto-save test result status
                await _configurationService.SaveAsync(_config);
            }
            else
            {
                StatusMessage = $"❌ {result.Message}";
                
                // Save failed test result to configuration
                _config.Status.LastTestDate = DateTime.Now;
                _config.Status.LastTestResult = "Failed";
                _config.Status.LastTestedMethod = "smb";
                _config.Status.IsTestingComplete = false;
                
                // Auto-save test result status
                await _configurationService.SaveAsync(_config);
            }
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

            // Validate only basic integrity (allow partial saves from this tab)
            var validationResult = await _configurationService.ValidateBasicAsync(_config);
            
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

