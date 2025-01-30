using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace CrossPlatformDownloadManager.DesktopApp.Converters;

public class IsGreaterThanZeroConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double number)
            return false;

        return number > 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool isGreaterThanZero)
            return 0d;
        
        return isGreaterThanZero ? 1d : 0d;
    }
}