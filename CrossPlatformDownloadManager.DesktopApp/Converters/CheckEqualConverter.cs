using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace CrossPlatformDownloadManager.DesktopApp.Converters;

public class CheckEqualConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null && value.Equals(parameter);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool isEqual)
            return null;

        return isEqual ? parameter : null;
    }
}