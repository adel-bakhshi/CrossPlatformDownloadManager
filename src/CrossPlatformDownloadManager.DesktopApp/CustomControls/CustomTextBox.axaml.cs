using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace CrossPlatformDownloadManager.DesktopApp.CustomControls;

public class CustomTextBox : TextBox
{
    #region Properties

    public static readonly StyledProperty<Geometry?> IconDataProperty = AvaloniaProperty
        .Register<CustomTextBox, Geometry?>(nameof(IconData), defaultValue: null);

    public Geometry? IconData
    {
        get => GetValue(IconDataProperty);
        set => SetValue(IconDataProperty, value);
    }

    public static readonly StyledProperty<IBrush?> IconColorProperty = AvaloniaProperty
        .Register<CustomTextBox, IBrush?>(nameof(IconColor), SolidColorBrush.Parse("#000000"));

    public IBrush? IconColor
    {
        get => GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }

    public static readonly StyledProperty<double> IconSizeProperty = AvaloniaProperty
        .Register<CustomTextBox, double>(nameof(IconSize), defaultValue: 16);

    public double IconSize
    {
        get => GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public static readonly StyledProperty<VerticalAlignment> VerticalIconAlignmentProperty = AvaloniaProperty.Register<CustomTextBox, VerticalAlignment>(
        nameof(VerticalIconAlignment));

    public VerticalAlignment VerticalIconAlignment
    {
        get => GetValue(VerticalIconAlignmentProperty);
        set => SetValue(VerticalIconAlignmentProperty, value);
    }

    public static readonly StyledProperty<Thickness> IconMarginProperty = AvaloniaProperty.Register<CustomTextBox, Thickness>(
        nameof(IconMargin), defaultValue: new Thickness(0, 0, 10, 0));

    public Thickness IconMargin
    {
        get => GetValue(IconMarginProperty);
        set => SetValue(IconMarginProperty, value);
    }

    public static readonly StyledProperty<IBrush?> OnFocusBorderColorProperty = AvaloniaProperty
        .Register<CustomTextBox, IBrush?>(nameof(OnFocusBorderColor), defaultValue: SolidColorBrush.Parse("#727272"));

    public IBrush? OnFocusBorderColor
    {
        get => GetValue(OnFocusBorderColorProperty);
        set => SetValue(OnFocusBorderColorProperty, value);
    }

    public static readonly StyledProperty<IBrush?> OnHoverBorderColorProperty = AvaloniaProperty
        .Register<CustomTextBox, IBrush?>(nameof(OnHoverBorderColor), defaultValue: SolidColorBrush.Parse("#727272"));

    public IBrush? OnHoverBorderColor
    {
        get => GetValue(OnHoverBorderColorProperty);
        set => SetValue(OnHoverBorderColorProperty, value);
    }

    public static readonly StyledProperty<IBrush?> OnErrorBorderColorProperty = AvaloniaProperty
        .Register<CustomTextBox, IBrush?>(nameof(OnErrorBorderColor));

    public IBrush? OnErrorBorderColor
    {
        get => GetValue(OnErrorBorderColorProperty);
        set => SetValue(OnErrorBorderColorProperty, value);
    }

    public static readonly StyledProperty<IBrush?> WatermarkForegroundProperty = AvaloniaProperty
        .Register<CustomTextBox, IBrush?>(nameof(WatermarkForeground));

    public IBrush? WatermarkForeground
    {
        get => GetValue(WatermarkForegroundProperty);
        set => SetValue(WatermarkForegroundProperty, value);
    }

    public static readonly StyledProperty<bool> ShowPasswordRevealButtonProperty = AvaloniaProperty
        .Register<CustomTextBox, bool>(nameof(ShowPasswordRevealButton));

    public bool ShowPasswordRevealButton
    {
        get => GetValue(ShowPasswordRevealButtonProperty);
        set => SetValue(ShowPasswordRevealButtonProperty, value);
    }

    public static readonly StyledProperty<bool> ShowClearButtonProperty = AvaloniaProperty
        .Register<CustomTextBox, bool>(nameof(ShowClearButton));

    public bool ShowClearButton
    {
        get => GetValue(ShowClearButtonProperty);
        set => SetValue(ShowClearButtonProperty, value);
    }

    #endregion
}