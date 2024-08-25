using System;
using System.Globalization;
using Avalonia.Data.Converters;
using CrossPlatformDownloadManager.Utils;

namespace CrossPlatformDownloadManager.DesktopApp.Converters;

public class ToFileSizeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var size = value as double?;
        if (size == null)
            return string.Empty;

        return size.ToFileSize();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}