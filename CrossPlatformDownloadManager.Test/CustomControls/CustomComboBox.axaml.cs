using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Media;

namespace CrossPlatformDownloadManager.Test.CustomControls;

public class CustomComboBox : ComboBox
{
    public static readonly StyledProperty<Geometry?> IconDataProperty = AvaloniaProperty.Register<CustomComboBox, Geometry?>(
        "IconData");

    public Geometry? IconData
    {
        get => GetValue(IconDataProperty);
        set => SetValue(IconDataProperty, value);
    }

    public static readonly StyledProperty<double> IconSizeProperty = AvaloniaProperty.Register<CustomComboBox, double>(
        "IconSize", defaultValue: 16);

    public double IconSize
    {
        get => GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public static readonly StyledProperty<IBrush?> IconColorProperty = AvaloniaProperty.Register<CustomComboBox, IBrush?>(
        "IconColor");

    public IBrush? IconColor
    {
        get => GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }
}