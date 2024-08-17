using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using CrossPlatformDownloadManager.Utils;

namespace CrossPlatformDownloadManager.DesktopApp.Converters;

public class StringToGeometryConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        try
        {
            var path = value as string;
            if (path.IsNullOrEmpty())
                return null;

            return StreamGeometry.Parse(path!);
        }
        catch
        {
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}