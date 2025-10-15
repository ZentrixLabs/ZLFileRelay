using System.IO;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ZLFileRelay.ConfigTool.Services;
using ZLFileRelay.Core.Interfaces;

namespace ZLFileRelay.ConfigTool.ViewModels;

/// <summary>
/// Unified view model for Service Management combining service controls and service account management
/// </summary>
public partial class ServiceViewModel : ObservableObject
{
    private readonly ServiceManager _serviceManager;
    private readonly ServiceAccountManager _serviceAccountManager;
    private readonly PermissionManager _permissionManager;
    private readonly ICredentialProvider _credentialProvider;
    private System.Threading.Timer? _statusRefreshTimer;

    // Service Status Properties
    [ObservableProperty]
    private string _serviceStatus = "Unknown";

    [ObservableProperty]
    private bool _isRunningAsAdmin;

    // Service Account Properties
    [ObservableProperty]
    private string _currentServiceAccount = "Loading...";

    [ObservableProperty]
    private string _serviceAccountStatus = "Checking...";

    [ObservableProperty]
    private string _domain = ".";

    [ObservableProperty]
    private string _serviceAccountUsername = "";

    // SMB Credentials Status
    [ObservableProperty]
    private string _smbCredentialsStatus = "Not Configured";

    // Activity Log
    [ObservableProperty]
    private ObservableCollection<string> _activityLog = new();

    public ServiceViewModel(
        ServiceManager serviceManager,
        ServiceAccountManager serviceAccountManager,
        PermissionManager permissionManager,
        ICredentialProvider credentialProvider)
    {
        _serviceManager = serviceManager;
        _serviceAccountManager = serviceAccountManager;
        _permissionManager = permissionManager;
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
            var status = await _serviceManager.GetStatusAsync();
            ServiceStatus = status.ToString();
            
            // Refresh service account info
            CurrentServiceAccount = await _serviceAccountManager.GetCurrentServiceAccountAsync() 
                ?? "Not Available";

            // Check if profile exists for the current account
            if (!string.IsNullOrWhiteSpace(CurrentServiceAccount) && 
                CurrentServiceAccount != "Not Available" &&
                !CurrentServiceAccount.Contains("LocalSystem"))
            {
                var profileExists = await _serviceAccountManager.CheckProfileExistsAsync(CurrentServiceAccount);
                ServiceAccountStatus = profileExists 
                    ? "✅ Profile exists" 
                    : "⚠️ Profile not found";
            }
            else
            {
                ServiceAccountStatus = "System account";
            }
            
            // Check SMB credentials
            try
            {
                var cred = _credentialProvider.GetCredential("smb_username");
                SmbCredentialsStatus = cred != null ? "✅ Configured" : "Not Configured";
            }
            catch
            {
                SmbCredentialsStatus = "Not Configured";
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"Error refreshing status: {ex.Message}");
        }
    }

    #region Service Control Commands

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

    #endregion

    #region Service Account Commands

    [RelayCommand]
    private async Task ApplyAccountChangeAsync(PasswordBox passwordBox)
    {
        if (!IsRunningAsAdmin)
        {
            AddLogMessage("ERROR: Administrator rights required to change service account");
            return;
        }

        try
        {
            var password = passwordBox.Password;
            var fullUsername = string.IsNullOrWhiteSpace(Domain) || Domain == "."
                ? $@".\{ServiceAccountUsername}"
                : $@"{Domain}\{ServiceAccountUsername}";

            AddLogMessage($"Setting service account to: {fullUsername}");
            
            var success = await _serviceAccountManager.SetServiceAccountAsync(fullUsername, password);

            if (success)
            {
                AddLogMessage("✅ Service account updated successfully");
                passwordBox.Clear();
                await RefreshStatusAsync();
            }
            else
            {
                AddLogMessage("❌ Failed to set service account");
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"❌ Error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task GrantLogonRightsAsync()
    {
        if (!IsRunningAsAdmin)
        {
            AddLogMessage("ERROR: Administrator rights required to grant logon rights");
            return;
        }

        try
        {
            var fullUsername = string.IsNullOrWhiteSpace(Domain) || Domain == "."
                ? $@".\{ServiceAccountUsername}"
                : $@"{Domain}\{ServiceAccountUsername}";

            AddLogMessage($"Granting 'Logon as Service' right to: {fullUsername}");
            
            var success = await _serviceAccountManager.GrantLogonAsServiceRightAsync(fullUsername);

            if (success)
            {
                AddLogMessage("✅ Logon as service right granted successfully");
                await RefreshStatusAsync();
            }
            else
            {
                AddLogMessage("❌ Failed to grant logon as service right");
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"❌ Error: {ex.Message}");
        }
    }

    #endregion

    #region Permission Management Commands

    [RelayCommand]
    private async Task FixUploadPermissionsAsync()
    {
        if (!IsRunningAsAdmin)
        {
            AddLogMessage("ERROR: Administrator rights required to modify permissions");
            return;
        }

        try
        {
            var uploadPath = @"C:\FileRelay\uploads"; // TODO: Get from config
            AddLogMessage($"Fixing upload folder permissions: {uploadPath}");
            
            var success = await _permissionManager.FixSourceFolderPermissionsAsync(uploadPath, CurrentServiceAccount);

            if (success)
            {
                AddLogMessage("✅ Upload folder permissions fixed");
            }
            else
            {
                AddLogMessage("❌ Failed to fix upload folder permissions");
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"❌ Error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task FixLogPermissionsAsync()
    {
        if (!IsRunningAsAdmin)
        {
            AddLogMessage("ERROR: Administrator rights required to modify permissions");
            return;
        }

        try
        {
            var logPath = @"C:\FileRelay\logs"; // TODO: Get from config
            AddLogMessage($"Fixing log folder permissions: {logPath}");
            
            var success = await _permissionManager.FixSourceFolderPermissionsAsync(logPath, CurrentServiceAccount);

            if (success)
            {
                AddLogMessage("✅ Log folder permissions fixed");
            }
            else
            {
                AddLogMessage("❌ Failed to fix log folder permissions");
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"❌ Error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task FixAllPermissionsAsync()
    {
        if (!IsRunningAsAdmin)
        {
            AddLogMessage("ERROR: Administrator rights required to modify permissions");
            return;
        }

        await FixUploadPermissionsAsync();
        await FixLogPermissionsAsync();
        
        var servicePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..");
        AddLogMessage($"Fixing service folder permissions: {servicePath}");
        
        try
        {
            var success = await _permissionManager.FixServiceFolderPermissionsAsync(servicePath, CurrentServiceAccount);
            if (success)
            {
                AddLogMessage("✅ Service folder permissions fixed");
            }
            else
            {
                AddLogMessage("❌ Failed to fix service folder permissions");
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"❌ Error: {ex.Message}");
        }
    }

    #endregion

    #region SMB Credentials

    [RelayCommand]
    private async Task ConfigureSmbCredentialsAsync()
    {
        try
        {
            // Create simple inline dialog for SMB credentials
            var dialog = new Window
            {
                Title = "Configure SMB Credentials",
                Width = 400,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            var stack = new StackPanel { Margin = new Thickness(20) };
            
            stack.Children.Add(new TextBlock { Text = "SMB Username:", Margin = new Thickness(0, 0, 0, 5) });
            var usernameBox = new TextBox { Margin = new Thickness(0, 0, 0, 15) };
            stack.Children.Add(usernameBox);
            
            stack.Children.Add(new TextBlock { Text = "SMB Password:", Margin = new Thickness(0, 0, 0, 5) });
            var passwordBox = new PasswordBox { Margin = new Thickness(0, 0, 0, 15) };
            stack.Children.Add(passwordBox);
            
            var buttonPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                HorizontalAlignment = HorizontalAlignment.Right 
            };
            var saveButton = new Button 
            { 
                Content = "Save", 
                Width = 80, 
                Margin = new Thickness(0, 0, 10, 0), 
                IsDefault = true 
            };
            var cancelButton = new Button 
            { 
                Content = "Cancel", 
                Width = 80, 
                IsCancel = true 
            };
            
            saveButton.Click += (s, ev) =>
            {
                if (!string.IsNullOrWhiteSpace(usernameBox.Text) && !string.IsNullOrWhiteSpace(passwordBox.Password))
                {
                    // TODO: Save credentials using ICredentialProvider
                    // _credentialProvider.SaveCredential("smb_username", usernameBox.Text);
                    // _credentialProvider.SaveCredential("smb_password", passwordBox.Password);
                    
                    AddLogMessage("✅ SMB credentials configured successfully (placeholder)");
                    dialog.DialogResult = true;
                    dialog.Close();
                }
                else
                {
                    MessageBox.Show("Please enter both username and password.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };
            
            buttonPanel.Children.Add(saveButton);
            buttonPanel.Children.Add(cancelButton);
            stack.Children.Add(buttonPanel);
            
            dialog.Content = stack;
            
            if (dialog.ShowDialog() == true)
            {
                await RefreshStatusAsync();
            }
        }
        catch (Exception ex)
        {
            AddLogMessage($"❌ Error configuring credentials: {ex.Message}");
        }
    }

    #endregion

    [RelayCommand]
    private void ClearLog()
    {
        ActivityLog.Clear();
    }

    private void AddLogMessage(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            ActivityLog.Add($"[{timestamp}] {message}");
            
            // Keep only last 100 messages
            while (ActivityLog.Count > 100)
            {
                ActivityLog.RemoveAt(0);
            }
        });
    }

    public void Dispose()
    {
        _statusRefreshTimer?.Dispose();
    }
}

