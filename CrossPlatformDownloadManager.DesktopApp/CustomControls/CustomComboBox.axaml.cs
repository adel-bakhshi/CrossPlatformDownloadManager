using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace CrossPlatformDownloadManager.DesktopApp.CustomControls;

public class CustomComboBox : ComboBox
{
    public static readonly StyledProperty<Geometry?> IconDataProperty =
        AvaloniaProperty.Register<CustomComboBox, Geometry?>(
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

    public static readonly StyledProperty<IBrush?> IconColorProperty =
        AvaloniaProperty.Register<CustomComboBox, IBrush?>(
            "IconColor");

    public IBrush? IconColor
    {
        get => GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }

    public static readonly StyledProperty<double> ToggleIconSizeProperty =
        AvaloniaProperty.Register<CustomComboBox, double>(
            "ToggleIconSize", defaultValue: 16);

    public double ToggleIconSize
    {
        get => GetValue(ToggleIconSizeProperty);
        set => SetValue(ToggleIconSizeProperty, value);
    }

    public static readonly StyledProperty<IBrush?> ToggleIconColorProperty =
        AvaloniaProperty.Register<CustomComboBox, IBrush?>(
            "ToggleIconColor");

    public IBrush? ToggleIconColor
    {
        get => GetValue(ToggleIconColorProperty);
        set => SetValue(ToggleIconColorProperty, value);
    }

    public static readonly StyledProperty<IBrush?> OnHoverToggleIconColorProperty =
        AvaloniaProperty.Register<CustomComboBox, IBrush?>(
            "OnHoverToggleIconColor");

    public IBrush? OnHoverToggleIconColor
    {
        get => GetValue(OnHoverToggleIconColorProperty);
        set => SetValue(OnHoverToggleIconColorProperty, value);
    }
}