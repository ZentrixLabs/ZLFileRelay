using System;
using System.Globalization;
using System.Windows.Data;

namespace ZLFileRelay.ConfigTool.Converters
{
    /// <summary>
    /// Converts boolean to "Allowed" / "Denied" text
    /// </summary>
    public class BoolToAllowedDeniedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "✅ Allowed" : "❌ Denied";
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts boolean to "Enabled" / "Disabled" text
    /// </summary>
    public class BoolToEnabledDisabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "✅ Enabled" : "⚪ Disabled";
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

