using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace ZLFileRelay.ConfigTool.Services;

public interface INotificationService
{
    void ShowSuccess(string message, TimeSpan? duration = null);
    void ShowWarning(string message, Action? clickAction = null);
    void ShowError(string message, Action? clickAction = null);
    void ShowInfo(string message, TimeSpan? duration = null);
    void Clear();
    ObservableCollection<ToastNotification> Notifications { get; }
}

public class NotificationService : INotificationService
{
    private readonly Dispatcher _dispatcher;
    
    public ObservableCollection<ToastNotification> Notifications { get; }

    public NotificationService()
    {
        Notifications = new ObservableCollection<ToastNotification>();
        _dispatcher = Application.Current.Dispatcher;
    }

    public void ShowSuccess(string message, TimeSpan? duration = null)
    {
        var notification = new ToastNotification
        {
            Message = message,
            Type = NotificationType.Success,
            Icon = "\uE73E", // CheckMark
            Duration = duration ?? TimeSpan.FromSeconds(3)
        };
        
        AddNotification(notification);
    }

    public void ShowWarning(string message, Action? clickAction = null)
    {
        var notification = new ToastNotification
        {
            Message = message,
            Type = NotificationType.Warning,
            Icon = "\uE7BA", // Warning
            ClickAction = clickAction,
            Duration = clickAction != null ? null : TimeSpan.FromSeconds(5)
        };
        
        AddNotification(notification);
    }

    public void ShowError(string message, Action? clickAction = null)
    {
        var notification = new ToastNotification
        {
            Message = message,
            Type = NotificationType.Error,
            Icon = "\uE783", // Error
            ClickAction = clickAction,
            Duration = null // Errors stay until dismissed
        };
        
        AddNotification(notification);
    }

    public void ShowInfo(string message, TimeSpan? duration = null)
    {
        var notification = new ToastNotification
        {
            Message = message,
            Type = NotificationType.Info,
            Icon = "\uE946", // Info
            Duration = duration ?? TimeSpan.FromSeconds(4)
        };
        
        AddNotification(notification);
    }

    public void Clear()
    {
        _dispatcher.Invoke(() => Notifications.Clear());
    }

    private void AddNotification(ToastNotification notification)
    {
        _dispatcher.Invoke(() =>
        {
            Notifications.Insert(0, notification);
            
            // Limit to 5 notifications
            while (Notifications.Count > 5)
            {
                Notifications.RemoveAt(Notifications.Count - 1);
            }
            
            // Auto-dismiss if duration is set
            if (notification.Duration.HasValue)
            {
                var timer = new DispatcherTimer
                {
                    Interval = notification.Duration.Value
                };
                
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    RemoveNotification(notification);
                };
                
                timer.Start();
            }
        });
    }

    private void RemoveNotification(ToastNotification notification)
    {
        _dispatcher.Invoke(() =>
        {
            if (Notifications.Contains(notification))
            {
                Notifications.Remove(notification);
            }
        });
    }
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}

public class ToastNotification
{
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string Icon { get; set; } = string.Empty;
    public Action? ClickAction { get; set; }
    public TimeSpan? Duration { get; set; }
    
    public System.Windows.Media.Brush BackgroundColor => Type switch
    {
        NotificationType.Success => System.Windows.Media.Brushes.Green,
        NotificationType.Warning => System.Windows.Media.Brushes.Orange,
        NotificationType.Error => System.Windows.Media.Brushes.Red,
        _ => System.Windows.Media.Brushes.DodgerBlue
    };
    
    public bool IsClickable => ClickAction != null;
}

