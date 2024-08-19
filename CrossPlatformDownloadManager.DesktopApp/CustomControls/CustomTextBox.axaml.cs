using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace CrossPlatformDownloadManager.DesktopApp.CustomControls;

public class CustomTextBox : TextBox
{
    public static readonly StyledProperty<Geometry?> IconDataProperty =
        AvaloniaProperty.Register<CustomTextBox, Geometry?>(
            "IconData", defaultValue: null);

    public Geometry? IconData
    {
        get => GetValue(IconDataProperty);
        set => SetValue(IconDataProperty, value);
    }

    public static readonly StyledProperty<IBrush?> IconColorProperty =
        AvaloniaProperty.Register<CustomTextBox, IBrush?>(
            "IconColor", SolidColorBrush.Parse("#000000"));

    public IBrush? IconColor
    {
        get => GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }

    public static readonly StyledProperty<double> IconSizeProperty = AvaloniaProperty.Register<CustomTextBox, double>(
        "IconSize", defaultValue: 16);

    public double IconSize
    {
        get => GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public static readonly StyledProperty<IBrush?> OnFocusBorderColorProperty = AvaloniaProperty.Register<CustomTextBox, IBrush?>(
        "OnFocusBorderColor", defaultValue: SolidColorBrush.Parse("#727272"));

    public IBrush? OnFocusBorderColor
    {
        get => GetValue(OnFocusBorderColorProperty);
        set => SetValue(OnFocusBorderColorProperty, value);
    }

    public static readonly StyledProperty<IBrush?> OnHoverBorderColorProperty = AvaloniaProperty.Register<CustomTextBox, IBrush?>(
        "OnHoverBorderColor", defaultValue: SolidColorBrush.Parse("#727272"));

    public IBrush? OnHoverBorderColor
    {
        get => GetValue(OnHoverBorderColorProperty);
        set => SetValue(OnHoverBorderColorProperty, value);
    }

    public static readonly StyledProperty<IBrush?> OnErrorBorderColorProperty = AvaloniaProperty.Register<CustomTextBox, IBrush?>(
        "OnErrorBorderColor");

    public IBrush? OnErrorBorderColor
    {
        get => GetValue(OnErrorBorderColorProperty);
        set => SetValue(OnErrorBorderColorProperty, value);
    }

    public static readonly StyledProperty<IBrush?> WatermarkColorProperty = AvaloniaProperty.Register<CustomTextBox, IBrush?>(
        "WatermarkColor");

    public IBrush? WatermarkColor
    {
        get => GetValue(WatermarkColorProperty);
        set => SetValue(WatermarkColorProperty, value);
    }

    public static readonly StyledProperty<bool> ShowPasswordRevealButtonProperty = AvaloniaProperty.Register<CustomTextBox, bool>(
        "ShowPasswordRevealButton");

    public bool ShowPasswordRevealButton
    {
        get => GetValue(ShowPasswordRevealButtonProperty);
        set => SetValue(ShowPasswordRevealButtonProperty, value);
    }
    
    public static readonly StyledProperty<bool> ShowClearButtonProperty = AvaloniaProperty.Register<CustomTextBox, bool>(
        "ShowClearButton");

    public bool ShowClearButton
    {
        get => GetValue(ShowClearButtonProperty);
        set => SetValue(ShowClearButtonProperty, value);
    }
}