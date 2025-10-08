using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ZLFileRelay.ConfigTool.ViewModels;

namespace ZLFileRelay.ConfigTool.Converters;

public class HealthStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is HealthStatus status)
        {
            return status switch
            {
                HealthStatus.Good => Brushes.Green,
                HealthStatus.Warning => Brushes.Orange,
                HealthStatus.Error => Brushes.Red,
                _ => Brushes.Gray
            };
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class HealthStatusToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is HealthStatus status)
        {
            return status switch
            {
                HealthStatus.Good => "\uE73E", // CheckMark
                HealthStatus.Warning => "\uE7BA", // Warning
                HealthStatus.Error => "\uE711", // ErrorBadge
                _ => "\uE9D9" // Unknown
            };
        }
        return "\uE9D9";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

