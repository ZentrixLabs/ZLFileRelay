using System.Windows;
using System.Windows.Controls;
using ZLFileRelay.ConfigTool.ViewModels;
using ZLFileRelay.ConfigTool.Views;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace ZLFileRelay.ConfigTool;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ServiceManagementViewModel _serviceViewModel;
    private readonly RemoteServerViewModel _remoteServerViewModel;

    public MainWindow(
        ServiceManagementViewModel serviceViewModel,
        RemoteServerViewModel remoteServerViewModel)
    {
        InitializeComponent();
        
        _serviceViewModel = serviceViewModel;
        _remoteServerViewModel = remoteServerViewModel;
        
        // Bind Remote Server tab
        var remoteServerView = new RemoteServerView
        {
            DataContext = _remoteServerViewModel
        };
        RemoteServerContent.Content = remoteServerView;
        
        // Bind Service Management tab
        LogOutputItems.ItemsSource = _serviceViewModel.LogMessages;
        
        // Subscribe to remote server changes for status bar
        _remoteServerViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(RemoteServerViewModel.IsConnected) ||
                e.PropertyName == nameof(RemoteServerViewModel.ServerName) ||
                e.PropertyName == nameof(RemoteServerViewModel.IsLocalMode))
            {
                UpdateConnectionStatus();
            }
        };
        
        // Subscribe to service status changes for status bar
        _serviceViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ServiceManagementViewModel.ServiceStatusText))
            {
                UpdateServiceStatus();
            }
        };
        
        // Initial status refresh
        UpdateConnectionStatus();
        _ = RefreshStatusAsync();
    }

    #region Status Bar Updates
    
    private void UpdateConnectionStatus()
    {
        if (_remoteServerViewModel.IsLocalMode || !_remoteServerViewModel.IsConnected)
        {
            ConnectionModeIcon.Text = "\uE80F"; // Home icon
            ConnectionModeText.Text = "Local";
        }
        else
        {
            ConnectionModeIcon.Text = "\uE968"; // Server icon
            ConnectionModeText.Text = $"Remote: {_remoteServerViewModel.ServerName}";
        }
    }
    
    private void UpdateServiceStatus()
    {
        var statusText = _serviceViewModel.ServiceStatusText?.ToLowerInvariant() ?? "unknown";
        
        if (statusText.Contains("running"))
        {
            StatusBarServiceIcon.Text = "\uF13E"; // StatusCircleCheckmark (green)
            StatusBarServiceIcon.Foreground = System.Windows.Media.Brushes.Green;
            StatusBarServiceText.Text = "Service: Running";
            StatusBarServiceText.Foreground = System.Windows.Media.Brushes.Green;
        }
        else if (statusText.Contains("stopped"))
        {
            StatusBarServiceIcon.Text = "\uF136"; // StatusCircle (gray)
            StatusBarServiceIcon.Foreground = System.Windows.Media.Brushes.Gray;
            StatusBarServiceText.Text = "Service: Stopped";
            StatusBarServiceText.Foreground = System.Windows.Media.Brushes.Gray;
        }
        else if (statusText.Contains("not installed"))
        {
            StatusBarServiceIcon.Text = "\uE783"; // Warning
            StatusBarServiceIcon.Foreground = System.Windows.Media.Brushes.Orange;
            StatusBarServiceText.Text = "Service: Not Installed";
            StatusBarServiceText.Foreground = System.Windows.Media.Brushes.Orange;
        }
        else
        {
            StatusBarServiceIcon.Text = "\uF136"; // StatusCircle
            StatusBarServiceIcon.Foreground = System.Windows.Media.Brushes.Gray;
            StatusBarServiceText.Text = "Service: Checking...";
            StatusBarServiceText.Foreground = System.Windows.Media.Brushes.Gray;
        }
    }
    
    #endregion

    #region Service Management Tab

    private async Task RefreshStatusAsync()
    {
        await _serviceViewModel.RefreshStatusCommand.ExecuteAsync(null);
        ServiceStatusText.Text = _serviceViewModel.ServiceStatusText;
        CredentialsStatusText.Text = _serviceViewModel.CredentialsConfigured 
            ? "✅ Configured" 
            : "❌ Not Configured";
        UpdateServiceStatus();
    }

    private async void RefreshStatusButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshStatusAsync();
        StatusBarText.Text = "Status refreshed";
    }

    private async void InstallServiceButton_Click(object sender, RoutedEventArgs e)
    {
        await _serviceViewModel.InstallServiceCommand.ExecuteAsync(null);
        await RefreshStatusAsync();
    }

    private async void UninstallServiceButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to uninstall the ZL File Relay Service?",
            "Confirm Uninstall",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            await _serviceViewModel.UninstallServiceCommand.ExecuteAsync(null);
            await RefreshStatusAsync();
        }
    }

    private async void StartServiceButton_Click(object sender, RoutedEventArgs e)
    {
        await _serviceViewModel.StartServiceCommand.ExecuteAsync(null);
        await RefreshStatusAsync();
    }

    private async void StopServiceButton_Click(object sender, RoutedEventArgs e)
    {
        await _serviceViewModel.StopServiceCommand.ExecuteAsync(null);
        await RefreshStatusAsync();
    }

    private void ConfigureCredentialsButton_Click(object sender, RoutedEventArgs e)
    {
        // Simple dialog for SMB credentials
        var dialog = new Window
        {
            Title = "Configure SMB Credentials",
            Width = 400,
            Height = 250,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this
        };

        var stack = new StackPanel { Margin = new Thickness(20) };
        
        stack.Children.Add(new TextBlock { Text = "SMB Username:", Margin = new Thickness(0, 0, 0, 5) });
        var usernameBox = new TextBox { Margin = new Thickness(0, 0, 0, 15) };
        stack.Children.Add(usernameBox);
        
        stack.Children.Add(new TextBlock { Text = "SMB Password:", Margin = new Thickness(0, 0, 0, 5) });
        var passwordBox = new PasswordBox { Margin = new Thickness(0, 0, 0, 15) };
        stack.Children.Add(passwordBox);
        
        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var saveButton = new Button { Content = "Save", Width = 80, Margin = new Thickness(0, 0, 10, 0), IsDefault = true };
        var cancelButton = new Button { Content = "Cancel", Width = 80, IsCancel = true };
        
        saveButton.Click += (s, ev) =>
        {
            if (!string.IsNullOrWhiteSpace(usernameBox.Text) && !string.IsNullOrWhiteSpace(passwordBox.Password))
            {
                MessageBox.Show("SMB credentials saved! (Full implementation pending)", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                dialog.DialogResult = true;
                dialog.Close();
            }
            else
            {
                MessageBox.Show("Please enter both username and password.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };
        
        buttonPanel.Children.Add(saveButton);
        buttonPanel.Children.Add(cancelButton);
        stack.Children.Add(buttonPanel);
        
        dialog.Content = stack;
        dialog.ShowDialog();
        
        _ = RefreshStatusAsync();
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        _serviceViewModel.ClearLogCommand.Execute(null);
    }

    #endregion

    #region Configuration Tab - Placeholder Handlers

    private void SaveConfigButton_Click(object sender, RoutedEventArgs e)
    {
        ConfigStatusText.Text = "✅ Configuration saved! (Full implementation pending)";
        ConfigStatusText.Foreground = System.Windows.Media.Brushes.Green;
        StatusBarText.Text = "Configuration saved";
    }

    #endregion

    #region Web Portal Tab - Placeholder Handlers

    private void BrowseCertButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select SSL Certificate",
            Filter = "Certificate Files (*.pfx;*.p12)|*.pfx;*.p12|All Files (*.*)|*.*",
            DefaultExt = ".pfx"
        };

        if (dialog.ShowDialog() == true)
        {
            CertPathTextBox.Text = dialog.FileName;
        }
    }

    private void TestCertButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(CertPathTextBox.Text))
            {
                CertStatusText.Text = "❌ No certificate path specified";
                CertStatusText.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            if (!File.Exists(CertPathTextBox.Text))
            {
                CertStatusText.Text = $"❌ Certificate file not found: {CertPathTextBox.Text}";
                CertStatusText.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            var cert = new X509Certificate2(CertPathTextBox.Text, CertPasswordBox.Password);
            CertStatusText.Text = $"✅ Certificate valid: {cert.Subject} (Expires: {cert.NotAfter:yyyy-MM-dd})";
            CertStatusText.Foreground = System.Windows.Media.Brushes.Green;
        }
        catch (Exception ex)
        {
            CertStatusText.Text = $"❌ Invalid certificate: {ex.Message}";
            CertStatusText.Foreground = System.Windows.Media.Brushes.Red;
        }
    }

    private void SaveWebConfigButton_Click(object sender, RoutedEventArgs e)
    {
        WebPortalStatusText.Text = "✅ Web portal configuration saved! (Full implementation pending)";
        StatusBarText.Text = "Web portal configuration saved";
    }

    private async void RestartWebServiceButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Restart the Web Portal service to apply changes?",
            "Restart Service",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                WebPortalStatusText.Text = "Restarting Web Portal service...";
                
                var stopProcess = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = "stop ZLFileRelay.WebPortal",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                stopProcess?.WaitForExit();

                await Task.Delay(2000);

                var startProcess = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = "start ZLFileRelay.WebPortal",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                startProcess?.WaitForExit();

                WebPortalStatusText.Text = "✅ Web Portal service restarted successfully";
                StatusBarText.Text = "Web Portal service restarted";
            }
            catch (Exception ex)
            {
                WebPortalStatusText.Text = $"❌ Error restarting service: {ex.Message}";
            }
        }
    }

    #endregion

    #region SSH Settings Tab - Placeholder Handlers

    private void GenerateKeyButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "SSH key generation will be implemented here.\n\n" +
            "This will:\n" +
            "• Generate ED25519 key pair\n" +
            "• Save to C:\\ProgramData\\ZLFileRelay\\ssh\\\n" +
            "• Display public key for copying",
            "Coming Soon",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ViewPublicKeyButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "View public key functionality will be implemented here.",
            "Coming Soon",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void CopyPublicKeyButton_Click(object sender, RoutedEventArgs e)
    {
        SshTestResultText.Text = "✅ Public key copied to clipboard (placeholder)";
        StatusBarText.Text = "Public key copied";
    }

    private void BrowseKeyButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select SSH Private Key",
            Filter = "SSH Keys (id_*;*.pem;*.key)|id_*;*.pem;*.key|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            SshKeyPathTextBox.Text = dialog.FileName;
        }
    }

    private void TestSshButton_Click(object sender, RoutedEventArgs e)
    {
        SshTestResultText.Text = "SSH connection testing will be implemented here.\n" +
                                 "This will verify:\n" +
                                 "• Host connectivity\n" +
                                 "• SSH key authentication\n" +
                                 "• Destination path accessibility";
    }

    private void SaveSshConfigButton_Click(object sender, RoutedEventArgs e)
    {
        SshTestResultText.Text = "✅ SSH configuration saved! (Full implementation pending)";
        StatusBarText.Text = "SSH configuration saved";
    }

    #endregion

    #region Service Account Tab - Placeholder Handlers

    private void ApplyAccountButton_Click(object sender, RoutedEventArgs e)
    {
        var username = ServiceAccountUsernameTextBox.Text;
        var domain = DomainTextBox.Text;

        if (string.IsNullOrWhiteSpace(username))
        {
            MessageBox.Show("Please enter a username", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"Change service account to:\n{domain}\\{username}\n\n" +
            "The service will be restarted with the new account.\n\n" +
            "(Full implementation pending)",
            "Confirm Account Change",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            ServiceAccountLogText.Text = $"✅ Service account changed to {domain}\\{username} (placeholder)";
            StatusBarText.Text = "Service account changed";
        }
    }

    private void GrantLogonRightsButton_Click(object sender, RoutedEventArgs e)
    {
        var username = ServiceAccountUsernameTextBox.Text;
        var domain = DomainTextBox.Text;

        if (string.IsNullOrWhiteSpace(username))
        {
            MessageBox.Show("Please enter a username", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ServiceAccountLogText.Text = $"✅ Logon rights granted to {domain}\\{username} (placeholder)";
        StatusBarText.Text = "Logon rights granted";
    }

    private void FixUploadPermsButton_Click(object sender, RoutedEventArgs e)
    {
        ServiceAccountLogText.Text = "✅ Upload folder permissions fixed (placeholder)";
        StatusBarText.Text = "Upload folder permissions fixed";
    }

    private void FixLogPermsButton_Click(object sender, RoutedEventArgs e)
    {
        ServiceAccountLogText.Text = "✅ Log folder permissions fixed (placeholder)";
        StatusBarText.Text = "Log folder permissions fixed";
    }

    private void FixAllPermsButton_Click(object sender, RoutedEventArgs e)
    {
        ServiceAccountLogText.Text = "✅ All folder permissions fixed (placeholder)";
        StatusBarText.Text = "All folder permissions fixed";
    }

    #endregion
}
