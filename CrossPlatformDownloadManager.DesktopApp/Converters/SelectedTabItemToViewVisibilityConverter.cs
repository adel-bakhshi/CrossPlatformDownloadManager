using System;
using System.Globalization;
using Avalonia.Data.Converters;
using CrossPlatformDownloadManager.Utils;

namespace CrossPlatformDownloadManager.DesktopApp.Converters;

public class SelectedTabItemToViewVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var currentTab = value?.ToString();
        var expectedTab = parameter?.ToString();
        if (currentTab.IsNullOrEmpty() || expectedTab.IsNullOrEmpty())
            return false;

        return currentTab!.Equals(expectedTab!);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool isVisible)
            return string.Empty;

        var expectedTab = parameter?.ToString();
        if (expectedTab.IsNullOrEmpty())
            return string.Empty;

        return isVisible ? expectedTab! : string.Empty;
    }
}