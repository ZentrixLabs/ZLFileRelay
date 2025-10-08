using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ZLFileRelay.ConfigTool.Services;
using ZLFileRelay.Core.Interfaces;

namespace ZLFileRelay.ConfigTool.ViewModels;

public partial class ServiceManagementViewModel : ObservableObject
{
    private readonly ServiceManager _serviceManager;
    private readonly ICredentialProvider _credentialProvider;
    private System.Threading.Timer? _statusRefreshTimer;

    [ObservableProperty]
    private ServiceStatus _serviceStatus = ServiceStatus.Unknown;

    [ObservableProperty]
    private string _serviceStatusText = "Unknown";

    [ObservableProperty]
    private bool _isRunningAsAdmin;

    [ObservableProperty]
    private bool _credentialsConfigured;

    [ObservableProperty]
    private ObservableCollection<string> _logMessages = new();

    public ServiceManagementViewModel(
        ServiceManager serviceManager,
        ICredentialProvider credentialProvider)
    {
        _serviceManager = serviceManager;
        _credentialProvider = credentialProvider;
        _isRunningAsAdmin = _serviceManager.IsRunningAsAdministrator();

        // Start auto-refresh timer
        _statusRefreshTimer = new System.Threading.Timer(
            async _ => await RefreshStatusAsync(),
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(5));
    }

    [RelayCommand]
    private async Task RefreshStatusAsync()
    {
        try
        {
            ServiceStatus = await _serviceManager.GetStatusAsync();
            ServiceStatusText = ServiceStatus.ToString();
            
            // Check credentials
            try
            {
                var cred = _credentialProvider.GetCredential("smb_username");
                CredentialsConfigured = cred != null;
            }
            catch
            {
                CredentialsConfigured = false;
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"Error refreshing status: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task InstallServiceAsync()
    {
        if (!IsRunningAsAdmin)
        {
            AddLogMessage("ERROR: Administrator rights required to install service");
            return;
        }

        try
        {
            var servicePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, 
                "..", "..", "ZLFileRelay.Service", 
                "ZLFileRelay.Service.exe");

            var fullPath = Path.GetFullPath(servicePath);
            
            if (!File.Exists(fullPath))
            {
                AddLogMessage($"ERROR: Service executable not found: {fullPath}");
                return;
            }

            AddLogMessage($"Installing service from: {fullPath}");
            var success = await _serviceManager.InstallAsync(fullPath);
            
            if (success)
            {
                AddLogMessage("✅ Service installed successfully");
                await RefreshStatusAsync();
            }
            else
            {
                AddLogMessage("❌ Failed to install service");
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"ERROR: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task UninstallServiceAsync()
    {
        if (!IsRunningAsAdmin)
        {
            AddLogMessage("ERROR: Administrator rights required to uninstall service");
            return;
        }

        try
        {
            AddLogMessage("Uninstalling service...");
            var success = await _serviceManager.UninstallAsync();
            
            if (success)
            {
                AddLogMessage("✅ Service uninstalled successfully");
                await RefreshStatusAsync();
            }
            else
            {
                AddLogMessage("❌ Failed to uninstall service");
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"ERROR: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task StartServiceAsync()
    {
        try
        {
            AddLogMessage("Starting service...");
            var success = await _serviceManager.StartAsync();
            
            if (success)
            {
                AddLogMessage("✅ Service started successfully");
                await RefreshStatusAsync();
            }
            else
            {
                AddLogMessage("❌ Failed to start service");
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"ERROR: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task StopServiceAsync()
    {
        try
        {
            AddLogMessage("Stopping service...");
            var success = await _serviceManager.StopAsync();
            
            if (success)
            {
                AddLogMessage("✅ Service stopped successfully");
                await RefreshStatusAsync();
            }
            else
            {
                AddLogMessage("❌ Failed to stop service");
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"ERROR: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ConfigureCredentialsAsync()
    {
        // This will be handled by showing a dialog
        AddLogMessage("Opening credentials dialog...");
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void ClearLog()
    {
        LogMessages.Clear();
    }

    private void AddLogMessage(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            LogMessages.Add($"[{timestamp}] {message}");
            
            // Keep only last 100 messages
            while (LogMessages.Count > 100)
            {
                LogMessages.RemoveAt(0);
            }
        });
    }
}

