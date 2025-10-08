using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ZLFileRelay.ConfigTool.Services;

namespace ZLFileRelay.ConfigTool.ViewModels;

public partial class ServiceAccountViewModel : ObservableObject
{
    private readonly ServiceAccountManager _serviceAccountManager;
    private readonly PermissionManager _permissionManager;

    [ObservableProperty] private string _currentServiceAccount = "Loading...";
    [ObservableProperty] private string _profileStatus = "Checking...";
    [ObservableProperty] private string _serviceAccountUsername = @"DOMAIN\svc_account";
    [ObservableProperty] private string _serviceAccountPassword = string.Empty;
    [ObservableProperty] private ObservableCollection<string> _logMessages = new();

    public ServiceAccountViewModel(
        ServiceAccountManager serviceAccountManager,
        PermissionManager permissionManager)
    {
        _serviceAccountManager = serviceAccountManager;
        _permissionManager = permissionManager;

        _ = RefreshStatusAsync();
    }

    [RelayCommand]
    private async Task RefreshStatusAsync()
    {
        try
        {
            CurrentServiceAccount = await _serviceAccountManager.GetCurrentServiceAccountAsync() 
                ?? "Not Available";

            var profileExists = await _serviceAccountManager.CheckProfileExistsAsync(ServiceAccountUsername);
            ProfileStatus = profileExists 
                ? "✅ Profile Exists" 
                : "❌ Profile Not Found";
        }
        catch (Exception ex)
        {
            AddLog($"Error refreshing status: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SetServiceAccountAsync()
    {
        try
        {
            AddLog($"Setting service account to: {ServiceAccountUsername}");
            
            var success = await _serviceAccountManager.SetServiceAccountAsync(
                ServiceAccountUsername,
                ServiceAccountPassword);

            if (success)
            {
                AddLog("✅ Service account updated successfully");
                ServiceAccountPassword = string.Empty; // Clear password
                await RefreshStatusAsync();
            }
            else
            {
                AddLog("❌ Failed to set service account");
            }
        }
        catch (Exception ex)
        {
            AddLog($"❌ Error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task GrantLogonRightAsync()
    {
        try
        {
            AddLog($"Granting 'Logon as Service' right to: {ServiceAccountUsername}");
            
            var success = await _serviceAccountManager.GrantLogonAsServiceRightAsync(ServiceAccountUsername);

            if (success)
            {
                AddLog("✅ Logon as service right granted successfully");
            }
            else
            {
                AddLog("❌ Failed to grant logon as service right");
            }
        }
        catch (Exception ex)
        {
            AddLog($"❌ Error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task FixSourcePermissionsAsync()
    {
        try
        {
            var sourcePath = @"C:\FileRelay\uploads"; // Get from config
            AddLog($"Fixing source folder permissions: {sourcePath}");
            
            var success = await _permissionManager.FixSourceFolderPermissionsAsync(sourcePath, ServiceAccountUsername);

            if (success)
            {
                AddLog("✅ Source folder permissions fixed");
            }
            else
            {
                AddLog("❌ Failed to fix source folder permissions");
            }
        }
        catch (Exception ex)
        {
            AddLog($"❌ Error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task FixServicePermissionsAsync()
    {
        try
        {
            var servicePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..");
            AddLog($"Fixing service folder permissions: {servicePath}");
            
            var success = await _permissionManager.FixServiceFolderPermissionsAsync(servicePath, ServiceAccountUsername);

            if (success)
            {
                AddLog("✅ Service folder permissions fixed");
            }
            else
            {
                AddLog("❌ Failed to fix service folder permissions");
            }
        }
        catch (Exception ex)
        {
            AddLog($"❌ Error: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ClearLog()
    {
        LogMessages.Clear();
    }

    private void AddLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            LogMessages.Add($"[{timestamp}] {message}");
            
            while (LogMessages.Count > 100)
            {
                LogMessages.RemoveAt(0);
            }
        });
    }
}

