using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace CrossPlatformDownloadManager.DesktopApp.CustomControls;

public class CustomTextBox : TemplatedControl
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

    public static readonly StyledProperty<string> PlaceholderProperty =
        AvaloniaProperty.Register<CustomTextBox, string>(
            "Placeholder", defaultValue: string.Empty);

    public string Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public static readonly StyledProperty<IBrush?> PlaceholderColorProperty =
        AvaloniaProperty.Register<CustomTextBox, IBrush?>(
            "PlaceholderColor", defaultValue: SolidColorBrush.Parse("#727272"));

    public IBrush? PlaceholderColor
    {
        get => GetValue(PlaceholderColorProperty);
        set => SetValue(PlaceholderColorProperty, value);
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
}