using System.Windows;
using ZLFileRelay.ConfigTool.ViewModels;
using ZLFileRelay.ConfigTool.Views;
using ZLFileRelay.ConfigTool.Services;

namespace ZLFileRelay.ConfigTool;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// Simplified version with 6 tabs using clean MVVM architecture
/// </summary>
public partial class MainWindow : Window
{
    private readonly DashboardViewModel _dashboardViewModel;
    private readonly ServiceViewModel _serviceViewModel;
    private readonly FileTransferViewModel _fileTransferViewModel;
    private readonly WebPortalViewModel _webPortalViewModel;
    private readonly RemoteServerViewModel _remoteServerViewModel;
    private readonly INotificationService _notificationService;

    public MainWindow(
        DashboardViewModel dashboardViewModel,
        ServiceViewModel serviceViewModel,
        FileTransferViewModel fileTransferViewModel,
        WebPortalViewModel webPortalViewModel,
        RemoteServerViewModel remoteServerViewModel,
        INotificationService notificationService)
    {
        InitializeComponent();
        
        _dashboardViewModel = dashboardViewModel;
        _serviceViewModel = serviceViewModel;
        _fileTransferViewModel = fileTransferViewModel;
        _webPortalViewModel = webPortalViewModel;
        _remoteServerViewModel = remoteServerViewModel;
        _notificationService = notificationService;
        
        InitializeViews();
        SubscribeToEvents();
        
        // Initial status update
        UpdateConnectionStatus();
        
        // Show welcome notification
        _notificationService.ShowInfo("Welcome to ZL File Relay Configuration Tool");
    }

    /// <summary>
    /// Initialize all tab views with their respective view models
    /// </summary>
    private void InitializeViews()
    {
        // Bind NotificationHost
        NotificationHost.ItemsSource = _notificationService.Notifications;
        
        // 1. Dashboard Tab
        DashboardContent.Content = new DashboardView
        {
            DataContext = _dashboardViewModel
        };
        
        // 2. Service Tab (merged Service Management + Service Account)
        ServiceContent.Content = new ServiceView
        {
            DataContext = _serviceViewModel
        };
        
        // 3. File Transfer Tab (merged Configuration + SSH + Security)
        FileTransferContent.Content = new FileTransferView
        {
            DataContext = _fileTransferViewModel
        };
        
        // 4. Web Portal Tab
        WebPortalContent.Content = new WebPortalView
        {
            DataContext = _webPortalViewModel
        };
        
        // 5. Advanced Tab (Remote Server + Audit Logging)
        AdvancedContent.Content = new RemoteServerView
        {
            DataContext = _remoteServerViewModel
        };
        
        // 6. About Tab
        AboutContent.Content = new AboutView();
    }

    /// <summary>
    /// Subscribe to view model property changes for status bar updates
    /// </summary>
    private void SubscribeToEvents()
    {
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
            if (e.PropertyName == nameof(ServiceViewModel.ServiceStatus))
            {
                UpdateServiceStatus();
            }
        };
        
        // Initial service status check
        _ = Task.Run(async () => 
        {
            await Task.Delay(500); // Brief delay for UI to load
            Dispatcher.Invoke(() => UpdateServiceStatus());
        });
    }

    #region Status Bar Updates
    
    /// <summary>
    /// Update connection mode indicator in status bar
    /// </summary>
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
    
    /// <summary>
    /// Update service status indicator in status bar
    /// </summary>
    private void UpdateServiceStatus()
    {
        var statusText = _serviceViewModel.ServiceStatus?.ToLowerInvariant() ?? "unknown";
        
        if (statusText.Contains("running"))
        {
            StatusBarServiceIcon.Text = "\uF13E"; // StatusCircleCheckmark
            StatusBarServiceIcon.Foreground = System.Windows.Media.Brushes.Green;
            StatusBarServiceText.Text = "Service: Running";
            StatusBarServiceText.Foreground = System.Windows.Media.Brushes.Green;
        }
        else if (statusText.Contains("stopped"))
        {
            StatusBarServiceIcon.Text = "\uF136"; // StatusCircle
            StatusBarServiceIcon.Foreground = System.Windows.Media.Brushes.Gray;
            StatusBarServiceText.Text = "Service: Stopped";
            StatusBarServiceText.Foreground = System.Windows.Media.Brushes.Gray;
        }
        else if (statusText.Contains("not") || statusText.Contains("install"))
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
            StatusBarServiceText.Text = $"Service: {_serviceViewModel.ServiceStatus}";
            StatusBarServiceText.Foreground = System.Windows.Media.Brushes.Gray;
        }
    }
    
    #endregion

    /// <summary>
    /// Clean up resources when window closes
    /// </summary>
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Dispose view models if they implement IDisposable
        (_serviceViewModel as IDisposable)?.Dispose();
        
        base.OnClosing(e);
    }
}
