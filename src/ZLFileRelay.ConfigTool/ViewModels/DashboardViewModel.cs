using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Media;
using ZLFileRelay.ConfigTool.Services;

namespace ZLFileRelay.ConfigTool.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ServiceViewModel _serviceViewModel;
    private readonly FileTransferViewModel _fileTransferViewModel;
    private readonly WebPortalViewModel _webPortalViewModel;
    private readonly ConfigurationService _configurationService;
    private DateTime? _serviceStartTime;
    private System.Threading.Timer? _uptimeUpdateTimer;
    
    public DashboardViewModel(
        ServiceViewModel serviceViewModel,
        FileTransferViewModel fileTransferViewModel,
        WebPortalViewModel webPortalViewModel,
        ConfigurationService configurationService)
    {
        _serviceViewModel = serviceViewModel;
        _fileTransferViewModel = fileTransferViewModel;
        _webPortalViewModel = webPortalViewModel;
        _configurationService = configurationService;
        
        RecentActivities = new ObservableCollection<ActivityItem>();
        
        // Subscribe to changes
        _serviceViewModel.PropertyChanged += (s, e) => UpdateHealthStatus();
        _fileTransferViewModel.PropertyChanged += (s, e) => UpdateHealthStatus();
        _webPortalViewModel.PropertyChanged += (s, e) => UpdateHealthStatus();
        
        // Initial update
        UpdateHealthStatus();
        AddActivity("Dashboard loaded", ActivityType.Info);
        
        // Start uptime update timer (every 5 seconds)
        _uptimeUpdateTimer = new System.Threading.Timer(
            _ => UpdateUptimeIfRunning(),
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(5));
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
        // Reload configuration to get latest status
        await _configurationService.LoadAsync();
        
        // Force ViewModels to reload their config
        await _fileTransferViewModel.LoadConfigurationAsync();
        
        await _serviceViewModel.RefreshStatusCommand.ExecuteAsync(null);
        UpdateHealthStatus();
        AddActivity("All status refreshed", ActivityType.Info);
    }
    
    #endregion
    
    #region Health Status Updates
    
    private void UpdateHealthStatus()
    {
        // Get current configuration (should be loaded in App.OnStartup, but reload if null)
        var config = _configurationService.CurrentConfiguration;
        
        // If config is null, the file might not have loaded properly - use defaults
        // (This shouldn't happen if App.OnStartup ran correctly, but handle gracefully)
        if (config == null)
        {
            config = _configurationService.GetDefaultConfiguration();
            // Trigger background reload for next time
            _ = Task.Run(async () =>
            {
                await _configurationService.LoadAsync();
                // Update UI on main thread after load completes
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(UpdateHealthStatus));
            });
        }
        
        // Service Health
        var serviceStatus = _serviceViewModel.ServiceStatus?.ToLowerInvariant() ?? "unknown";
        if (serviceStatus.Contains("running"))
        {
            ServiceHealthStatus = HealthStatus.Good;
            ServiceHealthText = "Running normally";
            // Update service uptime when service is running
            UpdateServiceUptime(true);
        }
        else if (serviceStatus.Contains("stopped"))
        {
            ServiceHealthStatus = HealthStatus.Warning;
            ServiceHealthText = "Service stopped";
            ServiceUptime = "Not running";
        }
        else if (serviceStatus.Contains("not") && serviceStatus.Contains("installed"))
        {
            ServiceHealthStatus = HealthStatus.Error;
            ServiceHealthText = "Not installed";
            ServiceUptime = "Not installed";
        }
        else
        {
            ServiceHealthStatus = HealthStatus.Unknown;
            ServiceHealthText = "Status unknown";
            ServiceUptime = "Unknown";
        }
        
        // Configuration Health - check persisted status first, then fallback to ViewModel
        if (config?.Status != null && config.Status.IsConfigurationComplete)
        {
            ConfigurationHealthStatus = HealthStatus.Good;
            ConfigurationHealthText = "Configuration complete";
            
            // Show last saved date if available
            if (config.Status.LastConfigurationSaved.HasValue)
            {
                var saveDate = config.Status.LastConfigurationSaved.Value;
                var timeAgo = DateTime.Now - saveDate;
                if (timeAgo.TotalHours < 1)
                {
                    ConfigurationHealthText += $" (saved {timeAgo.TotalMinutes:F0} min ago)";
                }
                else if (timeAgo.TotalDays < 1)
                {
                    ConfigurationHealthText += $" (saved {timeAgo.TotalHours:F1} hours ago)";
                }
                else
                {
                    ConfigurationHealthText += $" (saved {saveDate:yyyy-MM-dd})";
                }
            }
        }
        else
        {
            // Fallback: check if we have valid configuration in ViewModel
            var hasSshConfig = !string.IsNullOrWhiteSpace(_fileTransferViewModel.SshHost) &&
                              !string.IsNullOrWhiteSpace(_fileTransferViewModel.SshUsername) &&
                              !string.IsNullOrWhiteSpace(_fileTransferViewModel.SshKeyPath);
            var hasSmbConfig = !string.IsNullOrWhiteSpace(_fileTransferViewModel.SmbServer) &&
                              !string.IsNullOrWhiteSpace(_fileTransferViewModel.SmbSharePath);
            
            if (hasSshConfig || hasSmbConfig)
            {
                ConfigurationHealthStatus = HealthStatus.Warning;
                ConfigurationHealthText = "Configuration present, not marked complete";
            }
            else
            {
                ConfigurationHealthStatus = HealthStatus.Unknown;
                ConfigurationHealthText = "Not configured";
            }
        }
        
        // SSH Health - check persisted test status first, then fallback to ViewModel
        var sshConfigured = !string.IsNullOrWhiteSpace(_fileTransferViewModel.SshHost) &&
                           !string.IsNullOrWhiteSpace(_fileTransferViewModel.SshUsername);
        
        if (!sshConfigured)
        {
            SshHealthStatus = HealthStatus.Unknown;
            SshHealthText = "Not configured";
        }
        else if (config?.Status != null && config.Status.IsTestingComplete && 
                 config.Status.LastTestResult == "Success" && 
                 config.Status.LastTestedMethod == "ssh")
        {
            // Use persisted successful test result
            SshHealthStatus = HealthStatus.Good;
            if (config.Status.LastTestDate.HasValue)
            {
                var testDate = config.Status.LastTestDate.Value;
                var timeAgo = DateTime.Now - testDate;
                if (timeAgo.TotalHours < 24)
                {
                    SshHealthText = $"Tested successfully ({timeAgo.TotalHours:F1} hours ago)";
                }
                else
                {
                    SshHealthText = $"Tested successfully ({testDate:yyyy-MM-dd})";
                }
            }
            else
            {
                SshHealthText = "Connection tested successfully";
            }
        }
        else if (config?.Status != null && config.Status.LastTestResult == "Failed" &&
                 config.Status.LastTestedMethod == "ssh")
        {
            // Use persisted failed test result
            SshHealthStatus = HealthStatus.Error;
            SshHealthText = "Connection test failed";
        }
        else
        {
            // Fallback: check ViewModel test result
            var testResult = _fileTransferViewModel.SshTestResult?.ToLowerInvariant() ?? "";
            
            if (string.IsNullOrWhiteSpace(testResult) || 
                testResult.Contains("click 'test") || 
                testResult.Contains("not tested") ||
                testResult.Contains("click 'test ssh connection"))
            {
                SshHealthStatus = HealthStatus.Warning;
                SshHealthText = "Configured, not tested";
            }
            else if (testResult.Contains("✅") || testResult.Contains("success"))
            {
                SshHealthStatus = HealthStatus.Good;
                SshHealthText = "Connection successful";
            }
            else if (testResult.Contains("❌") || testResult.Contains("failed") || testResult.Contains("error"))
            {
                SshHealthStatus = HealthStatus.Error;
                SshHealthText = "Connection failed";
            }
            else
            {
                SshHealthStatus = HealthStatus.Warning;
                SshHealthText = "Status unknown";
            }
        }
        
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
    
    #region Service Uptime Calculation
    
    private void UpdateServiceUptime(bool isRunning)
    {
        if (isRunning)
        {
            // If we don't have a start time recorded, assume it just started
            if (_serviceStartTime == null)
            {
                _serviceStartTime = DateTime.Now;
            }
            
            var uptime = DateTime.Now - _serviceStartTime.Value;
            ServiceUptime = FormatUptime(uptime);
        }
        else
        {
            // Service is not running, reset start time
            _serviceStartTime = null;
            ServiceUptime = "Not running";
        }
    }
    
    private void UpdateUptimeIfRunning()
    {
        // Only update uptime if service is running and we have a start time
        var serviceStatus = _serviceViewModel.ServiceStatus?.ToLowerInvariant() ?? "unknown";
        if (serviceStatus.Contains("running") && _serviceStartTime.HasValue)
        {
            var uptime = DateTime.Now - _serviceStartTime.Value;
            ServiceUptime = FormatUptime(uptime);
        }
    }
    
    private static string FormatUptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1)
        {
            return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
        }
        else if (uptime.TotalHours >= 1)
        {
            return $"{uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
        }
        else if (uptime.TotalMinutes >= 1)
        {
            return $"{uptime.Minutes}m {uptime.Seconds}s";
        }
        else
        {
            return $"{uptime.Seconds}s";
        }
    }
    
    #endregion
    
    public void Dispose()
    {
        _uptimeUpdateTimer?.Dispose();
    }
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

