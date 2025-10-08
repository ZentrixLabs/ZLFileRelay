using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace ZLFileRelay.ConfigTool.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ServiceManagementViewModel _serviceViewModel;
    private readonly ConfigurationViewModel _configurationViewModel;
    private readonly SshSettingsViewModel _sshViewModel;
    
    public DashboardViewModel(
        ServiceManagementViewModel serviceViewModel,
        ConfigurationViewModel configurationViewModel,
        SshSettingsViewModel sshViewModel)
    {
        _serviceViewModel = serviceViewModel;
        _configurationViewModel = configurationViewModel;
        _sshViewModel = sshViewModel;
        
        RecentActivities = new ObservableCollection<ActivityItem>();
        
        // Subscribe to changes
        _serviceViewModel.PropertyChanged += (s, e) => UpdateHealthStatus();
        _configurationViewModel.PropertyChanged += (s, e) => UpdateHealthStatus();
        _sshViewModel.PropertyChanged += (s, e) => UpdateHealthStatus();
        
        // Initial update
        UpdateHealthStatus();
        AddActivity("Dashboard loaded", ActivityType.Info);
    }
    
    #region Health Status Properties
    
    [ObservableProperty]
    private HealthStatus _serviceHealthStatus = HealthStatus.Unknown;
    
    [ObservableProperty]
    private string _serviceHealthText = "Checking...";
    
    [ObservableProperty]
    private HealthStatus _configurationHealthStatus = HealthStatus.Unknown;
    
    [ObservableProperty]
    private string _configurationHealthText = "Not validated";
    
    [ObservableProperty]
    private HealthStatus _sshHealthStatus = HealthStatus.Unknown;
    
    [ObservableProperty]
    private string _sshHealthText = "Not configured";
    
    [ObservableProperty]
    private HealthStatus _webPortalHealthStatus = HealthStatus.Unknown;
    
    [ObservableProperty]
    private string _webPortalHealthText = "Not configured";
    
    [ObservableProperty]
    private HealthStatus _overallHealthStatus = HealthStatus.Unknown;
    
    [ObservableProperty]
    private string _overallHealthText = "System status unknown";
    
    #endregion
    
    #region Statistics Properties
    
    [ObservableProperty]
    private string _lastTransferTime = "Never";
    
    [ObservableProperty]
    private string _totalTransfersToday = "0";
    
    [ObservableProperty]
    private string _serviceUptime = "Not running";
    
    [ObservableProperty]
    private string _diskSpaceStatus = "Checking...";
    
    #endregion
    
    #region Recent Activity
    
    public ObservableCollection<ActivityItem> RecentActivities { get; }
    
    public void AddActivity(string message, ActivityType type)
    {
        var activity = new ActivityItem
        {
            Timestamp = DateTime.Now,
            Message = message,
            Type = type
        };
        
        RecentActivities.Insert(0, activity);
        
        // Keep only last 10 items
        while (RecentActivities.Count > 10)
        {
            RecentActivities.RemoveAt(RecentActivities.Count - 1);
        }
    }
    
    #endregion
    
    #region Quick Actions
    
    [RelayCommand]
    private async Task QuickStartServiceAsync()
    {
        if (_serviceViewModel.StartServiceCommand.CanExecute(null))
        {
            await _serviceViewModel.StartServiceCommand.ExecuteAsync(null);
            AddActivity("Service started via quick action", ActivityType.Success);
        }
    }
    
    [RelayCommand]
    private async Task QuickStopServiceAsync()
    {
        if (_serviceViewModel.StopServiceCommand.CanExecute(null))
        {
            await _serviceViewModel.StopServiceCommand.ExecuteAsync(null);
            AddActivity("Service stopped via quick action", ActivityType.Warning);
        }
    }
    
    [RelayCommand]
    private async Task RefreshAllStatusAsync()
    {
        await _serviceViewModel.RefreshStatusCommand.ExecuteAsync(null);
        UpdateHealthStatus();
        AddActivity("All status refreshed", ActivityType.Info);
    }
    
    #endregion
    
    #region Health Status Updates
    
    private void UpdateHealthStatus()
    {
        // Service Health
        var serviceStatus = _serviceViewModel.ServiceStatusText?.ToLowerInvariant() ?? "unknown";
        if (serviceStatus.Contains("running"))
        {
            ServiceHealthStatus = HealthStatus.Good;
            ServiceHealthText = "Running normally";
        }
        else if (serviceStatus.Contains("stopped"))
        {
            ServiceHealthStatus = HealthStatus.Warning;
            ServiceHealthText = "Service stopped";
        }
        else if (serviceStatus.Contains("not installed"))
        {
            ServiceHealthStatus = HealthStatus.Error;
            ServiceHealthText = "Not installed";
        }
        else
        {
            ServiceHealthStatus = HealthStatus.Unknown;
            ServiceHealthText = "Status unknown";
        }
        
        // Configuration Health
        var hasUploadDirectory = !string.IsNullOrWhiteSpace(_configurationViewModel.UploadDirectory);
        var hasWatchDirectory = !string.IsNullOrWhiteSpace(_configurationViewModel.WatchDirectory);
        
        if (hasUploadDirectory && hasWatchDirectory)
        {
            ConfigurationHealthStatus = HealthStatus.Good;
            ConfigurationHealthText = "Configuration valid";
        }
        else if (hasUploadDirectory || hasWatchDirectory)
        {
            ConfigurationHealthStatus = HealthStatus.Warning;
            ConfigurationHealthText = "Missing required settings";
        }
        else
        {
            ConfigurationHealthStatus = HealthStatus.Error;
            ConfigurationHealthText = "Configuration not loaded";
        }
        
        // SSH Health (simplified - would need actual SSH status)
        SshHealthStatus = HealthStatus.Warning;
        SshHealthText = "Not tested";
        
        // Web Portal Health (simplified)
        WebPortalHealthStatus = HealthStatus.Good;
        WebPortalHealthText = "Configuration present";
        
        // Overall Health
        UpdateOverallHealth();
    }
    
    private void UpdateOverallHealth()
    {
        var statuses = new[] 
        { 
            ServiceHealthStatus, 
            ConfigurationHealthStatus, 
            SshHealthStatus, 
            WebPortalHealthStatus 
        };
        
        if (statuses.Any(s => s == HealthStatus.Error))
        {
            OverallHealthStatus = HealthStatus.Error;
            OverallHealthText = "Action required";
        }
        else if (statuses.Any(s => s == HealthStatus.Warning))
        {
            OverallHealthStatus = HealthStatus.Warning;
            OverallHealthText = "Some issues detected";
        }
        else if (statuses.All(s => s == HealthStatus.Good))
        {
            OverallHealthStatus = HealthStatus.Good;
            OverallHealthText = "All systems operational";
        }
        else
        {
            OverallHealthStatus = HealthStatus.Unknown;
            OverallHealthText = "Checking system status...";
        }
    }
    
    #endregion
}

public enum HealthStatus
{
    Unknown,
    Good,
    Warning,
    Error
}

public enum ActivityType
{
    Info,
    Success,
    Warning,
    Error
}

public class ActivityItem
{
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = string.Empty;
    public ActivityType Type { get; set; }
    
    public string TimeText => Timestamp.ToString("HH:mm:ss");
    
    public string Icon => Type switch
    {
        ActivityType.Success => "\uE73E", // CheckMark
        ActivityType.Warning => "\uE7BA", // Warning
        ActivityType.Error => "\uE783", // Error
        _ => "\uE946" // Info
    };
    
    public Brush IconColor => Type switch
    {
        ActivityType.Success => Brushes.Green,
        ActivityType.Warning => Brushes.Orange,
        ActivityType.Error => Brushes.Red,
        _ => Brushes.Blue
    };
}

