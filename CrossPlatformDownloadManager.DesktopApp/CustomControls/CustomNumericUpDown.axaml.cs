using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace CrossPlatformDownloadManager.DesktopApp.CustomControls;

public class CustomNumericUpDown : NumericUpDown
{
    #region Properties

    public static readonly StyledProperty<IBrush?> WatermarkForegroundProperty =
        AvaloniaProperty.Register<CustomNumericUpDown, IBrush?>("WatermarkForeground");

    public IBrush? WatermarkForeground
    {
        get => GetValue(WatermarkForegroundProperty);
        set => SetValue(WatermarkForegroundProperty, value);
    }

    public static readonly StyledProperty<Geometry?> IconDataProperty =
        AvaloniaProperty.Register<CustomNumericUpDown, Geometry?>("IconData", defaultValue: null);

    public Geometry? IconData
    {
        get => GetValue(IconDataProperty);
        set => SetValue(IconDataProperty, value);
    }

    public static readonly StyledProperty<IBrush?> IconColorProperty =
        AvaloniaProperty.Register<CustomNumericUpDown, IBrush?>("IconColor", SolidColorBrush.Parse("#000000"));

    public IBrush? IconColor
    {
        get => GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }

    public static readonly StyledProperty<double> IconSizeProperty = AvaloniaProperty.Register<CustomNumericUpDown, double>("IconSize", defaultValue: 16);

    public double IconSize
    {
        get => GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    #endregion

    protected override void OnTextChanged(string? oldValue, string? newValue)
    {
        base.OnTextChanged(oldValue, newValue);

        if (!decimal.TryParse(newValue, out var result))
            Text = oldValue;
    }
}