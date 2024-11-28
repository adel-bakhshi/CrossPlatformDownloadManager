using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace CrossPlatformDownloadManager.DesktopApp.Converters;

public class PillCornerRadiusConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double relativeValue)
            return new CornerRadius(0);

        var cornerRadius = relativeValue / 2;
        return new CornerRadius(cornerRadius);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not CornerRadius cornerRadius)
            return 0;

        return cornerRadius.TopLeft * 2;
    }
}