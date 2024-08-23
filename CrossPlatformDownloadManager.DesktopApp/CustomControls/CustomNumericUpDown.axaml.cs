using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace CrossPlatformDownloadManager.DesktopApp.CustomControls;

public class CustomNumericUpDown : NumericUpDown
{
    #region Properties

    public static readonly StyledProperty<IBrush?> WatermarkForegroundProperty =
        AvaloniaProperty.Register<CustomNumericUpDown, IBrush?>(
            "WatermarkForeground");

    public IBrush? WatermarkForeground
    {
        get => GetValue(WatermarkForegroundProperty);
        set => SetValue(WatermarkForegroundProperty, value);
    }

    #endregion

    protected override void OnTextChanged(string? oldValue, string? newValue)
    {
        base.OnTextChanged(oldValue, newValue);

        if (!decimal.TryParse(newValue, out var result))
            Text = oldValue;
    }
}