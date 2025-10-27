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
            // Warn if they selected a .crt or .cer file (certificate, not private key)
            else if (selectedFile.EndsWith(".crt", StringComparison.OrdinalIgnoreCase) ||
                     selectedFile.EndsWith(".cer", StringComparison.OrdinalIgnoreCase))
            {
                SshTestResult = "⚠️ Warning: You selected a certificate file (.crt/.cer). " +
                              "Please select the PRIVATE key file (usually .key or .pem) instead.";
                return;
            }

            // Get target location for service account
            var serviceAccountConfigDir = Path.Combine(
                _config.Paths.ConfigDirectory ?? @"C:\ProgramData\ZLFileRelay",
                "ssh_keys");

            Directory.CreateDirectory(serviceAccountConfigDir);

            // Copy the file to the service account's config directory
            var fileName = Path.GetFileName(selectedFile);
            var targetPath = Path.Combine(serviceAccountConfigDir, fileName);

            try
            {
                // If file already exists, use incremental naming (key-1, key-2, etc) to avoid permission issues
                if (File.Exists(targetPath))
                {
                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    var extension = Path.GetExtension(fileName);
                    var counter = 1;
                    
                    do
                    {
                        fileName = $"{fileNameWithoutExt}-{counter}{extension}";
                        targetPath = Path.Combine(serviceAccountConfigDir, fileName);
                        counter++;
                    } while (File.Exists(targetPath));
                    
                    SshTestResult = $"ℹ️ Existing key file found, using incremental name: {fileName}";
                }

                // Copy the file to the target location
                File.Copy(selectedFile, targetPath);

                // Set permissions: Service account read, admin full control
                var fileInfo = new FileInfo(targetPath);
                var fileSecurity = fileInfo.GetAccessControl();

                // Add service account with read access
                var currentServiceAccount = await _serviceAccountManager.GetCurrentServiceAccountAsync();
                if (!string.IsNullOrWhiteSpace(currentServiceAccount) && 
                    !_impersonator.IsSystemAccount(currentServiceAccount))
                {
                    var serviceAccountIdentity = currentServiceAccount.Replace(".\\", "");
                    if (currentServiceAccount.Contains("\\"))
                    {
                        // Domain account
                        var account = new System.Security.Principal.NTAccount(currentServiceAccount);
                        var sid = (System.Security.Principal.SecurityIdentifier)account.Translate(typeof(System.Security.Principal.SecurityIdentifier));
                        fileSecurity.SetAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
                            sid,
                            System.Security.AccessControl.FileSystemRights.Read,
                            System.Security.AccessControl.AccessControlType.Allow));
                    }
                    else
                    {
                        // Local account
                        fileSecurity.SetAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
                            serviceAccountIdentity,
                            System.Security.AccessControl.FileSystemRights.Read,
                            System.Security.AccessControl.AccessControlType.Allow));
                    }
                }

                // Grant admin full control
                var adminAccount = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                fileSecurity.SetAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
                    adminAccount,
                    System.Security.AccessControl.FileSystemRights.FullControl,
                    System.Security.AccessControl.AccessControlType.Allow));

                // Remove inherited permissions and keep only explicit ones
                fileSecurity.SetAccessRuleProtection(true, false);

                fileInfo.SetAccessControl(fileSecurity);

                // Set the path to the copied file
                SshKeyPath = targetPath;
                
                SshTestResult = $"✅ SSH key copied to service account location:\n{targetPath}\n\n" +
                              $"Permissions configured for service account access.";
            }
            catch (Exception ex)
            {
                SshTestResult = $"❌ Error copying SSH key: {ex.Message}\n\n" +
                              $"Make sure you have permissions to copy the file and the service account exists.";
            }
        }
        catch (Exception ex)
        {
            SshTestResult = $"❌ Error browsing for SSH key: {ex.Message}";
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

            SshTestResult = $"✅ SSH key pair generated successfully (ED25519 format)\n\n" +
                           $"🔒 Private key (keep secure): {keyPair.PrivateKeyPath}\n" +
                           $"📤 Public key (deploy to remote): {keyPair.PublicKeyPath}\n\n" +
                           $"Next Steps:\n" +
                           $"1. The private key path has been automatically filled in above\n" +
                           $"2. Click 'View Public Key' or 'Copy to Clipboard' to get the public key\n" +
                           $"3. Add the public key to your remote server's ~/.ssh/authorized_keys file\n" +
                           $"4. Grant the service account read access to: {keyPair.PrivateKeyPath}\n" +
                           $"5. Test the connection\n\n" +
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

