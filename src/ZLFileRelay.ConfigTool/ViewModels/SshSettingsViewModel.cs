using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using ZLFileRelay.ConfigTool.Services;
using ZLFileRelay.Core.Models;

namespace ZLFileRelay.ConfigTool.ViewModels;

public partial class SshSettingsViewModel : ObservableObject
{
    private readonly ConfigurationService _configurationService;
    private readonly SshKeyGenerator _sshKeyGenerator;
    private readonly ConnectionTester _connectionTester;

    // Transfer Method
    [ObservableProperty] private bool _useSsh = true;
    [ObservableProperty] private bool _useSmb = false;

    // SSH Configuration
    [ObservableProperty] private string _sshHost = string.Empty;
    [ObservableProperty] private int _sshPort = 22;
    [ObservableProperty] private string _sshUsername = string.Empty;
    [ObservableProperty] private string _remoteDestinationPath = string.Empty;
    [ObservableProperty] private bool _remoteServerIsWindows = true;
    [ObservableProperty] private bool _enableCompression = true;
    [ObservableProperty] private int _connectionTimeout = 30;
    [ObservableProperty] private int _transferTimeout = 300;
    [ObservableProperty] private bool _strictHostKeyChecking = true;

    // SSH Keys
    [ObservableProperty] private string _privateKeyPath = string.Empty;
    [ObservableProperty] private string _publicKeyPath = string.Empty;
    [ObservableProperty] private string _sshKeyStatus = "Not Configured";
    [ObservableProperty] private bool _keyIsValid = false;
    [ObservableProperty] private string? _publicKey;

    // Logs
    [ObservableProperty] private ObservableCollection<string> _sshLogMessages = new();

    public SshSettingsViewModel(
        ConfigurationService configurationService,
        SshKeyGenerator sshKeyGenerator,
        ConnectionTester connectionTester)
    {
        _configurationService = configurationService;
        _sshKeyGenerator = sshKeyGenerator;
        _connectionTester = connectionTester;

        LoadFromConfiguration();
    }

    private void LoadFromConfiguration()
    {
        var config = _configurationService.CurrentConfiguration;
        if (config == null) return;

        UseSsh = config.Service.TransferMethod.Equals("ssh", StringComparison.OrdinalIgnoreCase);
        UseSmb = !UseSsh;

        SshHost = config.Transfer.Ssh.Host ?? string.Empty;
        SshPort = config.Transfer.Ssh.Port;
        SshUsername = config.Transfer.Ssh.Username ?? string.Empty;
        RemoteDestinationPath = config.Transfer.Ssh.DestinationPath ?? string.Empty;
        PrivateKeyPath = config.Transfer.Ssh.PrivateKeyPath ?? string.Empty;
        PublicKeyPath = $"{PrivateKeyPath}.pub";
        ConnectionTimeout = config.Transfer.Ssh.ConnectionTimeout;
        EnableCompression = true; // Not in current config, default to true

        _ = CheckSshKeyStatusAsync();
    }

    [RelayCommand]
    private async Task GenerateSshKeysAsync()
    {
        try
        {
            AddSshLog("Generating SSH key pair (ED25519)...");

            var keyPath = Path.Combine(
                _configurationService.CurrentConfiguration?.Paths.ConfigDirectory 
                    ?? @"C:\ProgramData\ZLFileRelay",
                "zlrelay_key");

            // Ensure directory exists
            var directory = Path.GetDirectoryName(keyPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var keyPair = await _sshKeyGenerator.GenerateAsync(SshKeyType.ED25519, keyPath);

            PrivateKeyPath = keyPair.PrivateKeyPath;
            PublicKeyPath = keyPair.PublicKeyPath;
            PublicKey = keyPair.PublicKey;
            SshKeyStatus = "✅ Key Generated";
            KeyIsValid = true;

            AddSshLog($"✅ SSH key pair generated successfully");
            AddSshLog($"   Private key: {PrivateKeyPath}");
            AddSshLog($"   Public key: {PublicKeyPath}");
            AddSshLog("");
            AddSshLog("📋 Copy this public key to your server's ~/.ssh/authorized_keys file:");
            AddSshLog(PublicKey);
        }
        catch (Exception ex)
        {
            AddSshLog($"❌ Error generating SSH keys: {ex.Message}");
            SshKeyStatus = "❌ Generation Failed";
        }
    }

    [RelayCommand]
    private async Task TestSshConnectionAsync()
    {
        try
        {
            AddSshLog("Testing SSH connection...");

            var config = _configurationService.CurrentConfiguration;
            if (config == null)
            {
                AddSshLog("❌ Configuration not loaded");
                return;
            }

            // Update config with current UI values
            config.Transfer.Ssh.Host = SshHost;
            config.Transfer.Ssh.Port = SshPort;
            config.Transfer.Ssh.Username = SshUsername;
            config.Transfer.Ssh.PrivateKeyPath = PrivateKeyPath;
            config.Transfer.Ssh.DestinationPath = RemoteDestinationPath;

            var result = await _connectionTester.TestSshAsync(config.Transfer.Ssh);

            if (result.Success)
            {
                AddSshLog($"✅ {result.Message}");
                if (!string.IsNullOrWhiteSpace(result.Details))
                {
                    AddSshLog("Directory listing:");
                    AddSshLog(result.Details);
                }
            }
            else
            {
                AddSshLog($"❌ {result.Message}");
                if (!string.IsNullOrWhiteSpace(result.Details))
                {
                    AddSshLog(result.Details);
                }
            }
        }
        catch (Exception ex)
        {
            AddSshLog($"❌ Error: {ex.Message}");
        }
    }

    [RelayCommand]
    private void BrowsePrivateKey()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select SSH Private Key",
            Filter = "All Files (*.*)|*.*|SSH Keys (id_*)|id_*",
            DefaultExt = ""
        };

        if (dialog.ShowDialog() == true)
        {
            PrivateKeyPath = dialog.FileName;
            PublicKeyPath = $"{dialog.FileName}.pub";
            _ = CheckSshKeyStatusAsync();
        }
    }

    [RelayCommand]
    private async Task ViewPublicKeyAsync()
    {
        if (string.IsNullOrWhiteSpace(PrivateKeyPath))
        {
            AddSshLog("❌ No private key path configured");
            return;
        }

        var publicKey = await _sshKeyGenerator.GetPublicKeyAsync(PrivateKeyPath);
        if (publicKey != null)
        {
            PublicKey = publicKey;
            AddSshLog("📋 Public Key:");
            AddSshLog(publicKey);
        }
        else
        {
            AddSshLog("❌ Could not read public key");
        }
    }

    [RelayCommand]
    private void CopyPublicKey()
    {
        if (!string.IsNullOrWhiteSpace(PublicKey))
        {
            System.Windows.Clipboard.SetText(PublicKey);
            AddSshLog("✅ Public key copied to clipboard");
        }
    }

    [RelayCommand]
    private async Task SaveConfigurationAsync()
    {
        try
        {
            var config = _configurationService.CurrentConfiguration;
            if (config == null)
            {
                AddSshLog("❌ Configuration not loaded");
                return;
            }

            // Update transfer method
            config.Service.TransferMethod = UseSsh ? "ssh" : "smb";

            // Update SSH settings from UI
            config.Transfer.Ssh.Host = SshHost;
            config.Transfer.Ssh.Port = SshPort;
            config.Transfer.Ssh.Username = SshUsername;
            config.Transfer.Ssh.PrivateKeyPath = PrivateKeyPath;
            config.Transfer.Ssh.DestinationPath = RemoteDestinationPath;
            config.Transfer.Ssh.ConnectionTimeout = ConnectionTimeout;
            config.Transfer.Ssh.Compression = EnableCompression;
            config.Transfer.Ssh.StrictHostKeyChecking = StrictHostKeyChecking;
            config.Transfer.Ssh.TransferTimeout = TransferTimeout;

            var success = await _configurationService.SaveAsync(config);
            
            if (success)
            {
                AddSshLog("✅ Configuration saved successfully");
            }
            else
            {
                AddSshLog("❌ Failed to save configuration");
            }
        }
        catch (Exception ex)
        {
            AddSshLog($"❌ Error saving configuration: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ClearSshLog()
    {
        SshLogMessages.Clear();
    }

    private async Task CheckSshKeyStatusAsync()
    {
        if (string.IsNullOrWhiteSpace(PrivateKeyPath))
        {
            SshKeyStatus = "Not Configured";
            KeyIsValid = false;
            return;
        }

        if (!File.Exists(PrivateKeyPath))
        {
            SshKeyStatus = "❌ Key File Not Found";
            KeyIsValid = false;
            return;
        }

        KeyIsValid = await _sshKeyGenerator.ValidateKeyAsync(PrivateKeyPath);
        SshKeyStatus = KeyIsValid ? "✅ Valid Key" : "❌ Invalid Key";
    }

    private void AddSshLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            SshLogMessages.Add($"[{timestamp}] {message}");
            
            while (SshLogMessages.Count > 100)
            {
                SshLogMessages.RemoveAt(0);
            }
        });
    }
}

