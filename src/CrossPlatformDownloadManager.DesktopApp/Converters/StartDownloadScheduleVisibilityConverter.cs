using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace CrossPlatformDownloadManager.DesktopApp.Converters;

public class StartDownloadScheduleVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string currentOption || parameter is not string expectedOption)
            return false;
        
        return currentOption.Equals(expectedOption);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool isVisible || parameter is not string expectedOption)
            return string.Empty;
        
        return isVisible ? expectedOption : string.Empty;
    }
}