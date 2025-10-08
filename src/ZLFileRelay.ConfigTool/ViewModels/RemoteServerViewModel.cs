using System.IO;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using ZLFileRelay.ConfigTool.Interfaces;
using ZLFileRelay.ConfigTool.Services;
using Microsoft.Extensions.Logging;

namespace ZLFileRelay.ConfigTool.ViewModels;

public partial class RemoteServerViewModel : ObservableObject
{
    private readonly IRemoteServerProvider _remoteServerProvider;
    private readonly ILogger<RemoteServerViewModel> _logger;
    private readonly PowerShellRemotingService _psRemoting;

    // Connection Settings
    [ObservableProperty] private bool _isLocalMode = true;
    [ObservableProperty] private bool _isRemoteMode = false;
    [ObservableProperty] private string _serverName = string.Empty;
    [ObservableProperty] private bool _useCurrentCredentials = true;
    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _password = string.Empty;

    // Status
    [ObservableProperty] private string _connectionStatus = "Not Connected";
    [ObservableProperty] private bool _isConnected = false;
    [ObservableProperty] private bool _isTesting = false;
    [ObservableProperty] private string _statusIcon = "🔌";
    
    // Test Results
    [ObservableProperty] private ObservableCollection<string> _logMessages = new();
    [ObservableProperty] private string _detectedServerInfo = string.Empty;
    [ObservableProperty] private bool _canConnect = false;

    public RemoteServerViewModel(
        IRemoteServerProvider remoteServerProvider,
        ILogger<RemoteServerViewModel> logger,
        PowerShellRemotingService psRemoting)
    {
        _remoteServerProvider = remoteServerProvider;
        _logger = logger;
        _psRemoting = psRemoting;
    }

    partial void OnIsLocalModeChanged(bool value)
    {
        if (value)
        {
            IsRemoteMode = false;
            ServerName = "localhost";
            UpdateConnectionStatus();
        }
    }

    partial void OnIsRemoteModeChanged(bool value)
    {
        if (value)
        {
            IsLocalMode = false;
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (IsLocalMode)
        {
            AddLog("ℹ️ Local mode selected - no remote connection needed");
            return;
        }

        if (string.IsNullOrWhiteSpace(ServerName))
        {
            AddLog("❌ Server name is required");
            return;
        }

        IsTesting = true;
        CanConnect = false;
        LogMessages.Clear();
        
        try
        {
            AddLog($"🔍 Testing connection to: {ServerName}");
            AddLog("");

            // Test 1: Ping
            AddLog("1️⃣ Testing network connectivity...");
            var pingResult = await TestPingAsync(ServerName);
            if (!pingResult)
            {
                AddLog("❌ Server is not reachable via ping");
                AddLog("   Check server name and network connectivity");
                return;
            }
            AddLog("✅ Server is reachable");
            AddLog("");

            // Test 2: Administrative Share
            AddLog("2️⃣ Testing administrative share access...");
            var shareResult = await TestAdminShareAsync(ServerName);
            if (!shareResult)
            {
                AddLog("❌ Cannot access administrative share (\\\\server\\c$)");
                AddLog("   Ensure you have administrative privileges");
                AddLog("   Check Windows Firewall (SMB port 445)");
                return;
            }
            AddLog("✅ Administrative share is accessible");
            AddLog("");

            // Test 3: Service Control
            AddLog("3️⃣ Testing service control access...");
            var serviceResult = await TestServiceAccessAsync(ServerName);
            if (!serviceResult)
            {
                AddLog("⚠️ Cannot query services (may work after connect)");
            }
            else
            {
                AddLog("✅ Service control is accessible");
            }
            AddLog("");

            // Test 4: WinRM / PowerShell Remoting
            AddLog("4️⃣ Testing WinRM / PowerShell Remoting...");
            var winrmResult = await _psRemoting.TestWinRMConnectionAsync(ServerName);
            if (!winrmResult)
            {
                AddLog("⚠️ WinRM not available (some features will be limited)");
                AddLog("   For full remote management, enable WinRM:");
                AddLog("   Run on remote server: Enable-PSRemoting -Force");
            }
            else
            {
                AddLog("✅ WinRM is available and accessible");
            }
            AddLog("");

            // Test 5: Installation Check
            AddLog("5️⃣ Checking for ZL File Relay installation...");
            var installResult = await CheckInstallationAsync(ServerName);
            if (!installResult)
            {
                AddLog("ℹ️ ZL File Relay not detected (may not be installed yet)");
            }
            else
            {
                AddLog("✅ ZL File Relay installation detected");
            }
            AddLog("");

            AddLog("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            if (winrmResult)
            {
                AddLog("✅ Connection test successful! Full remote management available.");
            }
            else
            {
                AddLog("⚠️  Connection test completed with warnings.");
                AddLog("   Basic operations work, but some features require WinRM.");
            }
            AddLog("Ready to connect to remote server.");
            CanConnect = true;
        }
        catch (Exception ex)
        {
            AddLog($"❌ Error during connection test: {ex.Message}");
            _logger.LogError(ex, "Connection test failed");
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        try
        {
            if (IsLocalMode)
            {
                _remoteServerProvider.SetServer(null, false);
                IsConnected = true;
                ConnectionStatus = "✅ Connected (Local)";
                StatusIcon = "🖥️";
                AddLog("✅ Using local machine");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(ServerName))
                {
                    AddLog("❌ Server name is required");
                    return;
                }

                // Set the remote server in the provider
                _remoteServerProvider.SetServer(ServerName, true);
                
                IsConnected = true;
                ConnectionStatus = $"✅ Connected to {ServerName}";
                StatusIcon = "🌐";
                AddLog($"✅ Connected to remote server: {ServerName}");
                AddLog("All operations will now target the remote server.");
                
                // Detect server info
                await DetectServerInfoAsync();
            }
        }
        catch (Exception ex)
        {
            AddLog($"❌ Connection failed: {ex.Message}");
            _logger.LogError(ex, "Failed to connect to server");
        }

        await Task.CompletedTask;
    }

    [RelayCommand]
    private void Disconnect()
    {
        _remoteServerProvider.SetServer(null, false);
        IsConnected = false;
        ConnectionStatus = "Not Connected";
        StatusIcon = "🔌";
        IsLocalMode = true;
        ServerName = string.Empty;
        DetectedServerInfo = string.Empty;
        AddLog("🔌 Disconnected from server");
    }

    [RelayCommand]
    private void ClearLog()
    {
        LogMessages.Clear();
    }

    private async Task<bool> TestPingAsync(string serverName)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(serverName, 5000);
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> TestAdminShareAsync(string serverName)
    {
        return await Task.Run(() =>
        {
            try
            {
                var sharePath = $@"\\{serverName}\c$";
                return Directory.Exists(sharePath);
            }
            catch
            {
                return false;
            }
        });
    }

    private async Task<bool> TestServiceAccessAsync(string serverName)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Try to get services on remote machine
                var services = ServiceController.GetServices(serverName);
                return services.Length > 0;
            }
            catch
            {
                return false;
            }
        });
    }

    private async Task<bool> CheckInstallationAsync(string serverName)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Check for standard installation paths
                var paths = new[]
                {
                    $@"\\{serverName}\c$\Program Files\ZLFileRelay\ZLFileRelay.Service.exe",
                    $@"\\{serverName}\c$\Program Files\ZLFileRelay\appsettings.json"
                };

                foreach (var path in paths)
                {
                    if (File.Exists(path))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        });
    }

    private async Task DetectServerInfoAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                var info = new List<string>();
                
                // Get OS version if possible
                var sharePath = $@"\\{ServerName}\c$";
                if (Directory.Exists(sharePath))
                {
                    info.Add($"Server: {ServerName}");
                    info.Add("Administrative access: Available");
                }

                // Check for installation
                var installPath = $@"\\{ServerName}\c$\Program Files\ZLFileRelay";
                if (Directory.Exists(installPath))
                {
                    info.Add("ZL File Relay: Installed");
                }
                else
                {
                    info.Add("ZL File Relay: Not detected");
                }

                DetectedServerInfo = string.Join(" | ", info);
            }
            catch (Exception ex)
            {
                DetectedServerInfo = $"Error: {ex.Message}";
            }
        });
    }

    private void UpdateConnectionStatus()
    {
        if (IsLocalMode)
        {
            ConnectionStatus = "Local Machine";
            StatusIcon = "🖥️";
        }
        else if (IsConnected)
        {
            ConnectionStatus = $"✅ Connected to {ServerName}";
            StatusIcon = "🌐";
        }
        else
        {
            ConnectionStatus = "Not Connected";
            StatusIcon = "🔌";
        }
    }

    private void AddLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            LogMessages.Add(string.IsNullOrWhiteSpace(message) ? "" : $"[{timestamp}] {message}");
            
            // Keep only last 200 messages
            while (LogMessages.Count > 200)
            {
                LogMessages.RemoveAt(0);
            }
        });
    }
}

