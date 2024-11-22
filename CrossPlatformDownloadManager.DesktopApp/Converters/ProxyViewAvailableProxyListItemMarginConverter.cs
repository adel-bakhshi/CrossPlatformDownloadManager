using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data.Converters;
using CrossPlatformDownloadManager.Data.ViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Converters;

public class ProxyViewAvailableProxyListItemMarginConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count != 2)
            return new Thickness(0);
        
        if (values[0] is not ProxySettingsViewModel proxySettings || values[1] is not IEnumerable<ProxySettingsViewModel> proxiesEnumerable)
            return new Thickness(0);

        var proxies = proxiesEnumerable.ToList();
        var index = proxies.IndexOf(proxySettings);
        if (index == -1)
            return new Thickness(0);

        index++;
        var row = (int)Math.Ceiling(index / (double)3);
        var column = index - (row - 1) * 3;

        var topMargin = row == 1 ? 0 : 20;
        var rightMargin = column % 3 == 0 ? 0 : 20;
        return new Thickness(0, topMargin, rightMargin, 0);
    }
}