using System.Windows;
using ZLFileRelay.ConfigTool.ViewModels;

namespace ZLFileRelay.ConfigTool;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ServiceManagementViewModel _serviceViewModel;

    public MainWindow(ServiceManagementViewModel serviceViewModel)
    {
        InitializeComponent();
        
        _serviceViewModel = serviceViewModel;
        
        // Bind log output
        LogOutputItems.ItemsSource = _serviceViewModel.LogMessages;
        
        // Initial status refresh
        _ = RefreshStatusAsync();
    }

    private async Task RefreshStatusAsync()
    {
        await _serviceViewModel.RefreshStatusCommand.ExecuteAsync(null);
        ServiceStatusText.Text = _serviceViewModel.ServiceStatusText;
        CredentialsStatusText.Text = _serviceViewModel.CredentialsConfigured 
            ? "✅ Configured" 
            : "❌ Not Configured";
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
        MessageBox.Show(
            "SMB Credentials dialog will be implemented in the next phase.",
            "Coming Soon",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        _serviceViewModel.ClearLogCommand.Execute(null);
    }
}
