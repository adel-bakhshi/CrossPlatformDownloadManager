using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;

namespace CrossPlatformDownloadManager.DesktopApp.CustomControls;

public class CustomToggleSwitch : ToggleButton
{
    #region Properties

    public static readonly StyledProperty<double> ToggleCircleSizeProperty = AvaloniaProperty
        .Register<CustomToggleSwitch, double>(nameof(ToggleCircleSize));

    public double ToggleCircleSize
    {
        get => GetValue(ToggleCircleSizeProperty);
        set => SetValue(ToggleCircleSizeProperty, value);
    }

    public static readonly StyledProperty<double> ToggleCircleOffsetProperty = AvaloniaProperty
        .Register<CustomToggleSwitch, double>(nameof(ToggleCircleOffset));

    public double ToggleCircleOffset
    {
        get => GetValue(ToggleCircleOffsetProperty);
        set => SetValue(ToggleCircleOffsetProperty, value);
    }

    private double _toggleCircleLeftCanvas;

    public static readonly DirectProperty<CustomToggleSwitch, double> ToggleCircleLeftCanvasProperty = AvaloniaProperty
        .RegisterDirect<CustomToggleSwitch, double>(nameof(ToggleCircleLeftCanvas),
            o => o.ToggleCircleLeftCanvas,
            (o, v) => o.ToggleCircleLeftCanvas = v);

    public double ToggleCircleLeftCanvas
    {
        get => _toggleCircleLeftCanvas;
        set => SetAndRaise(ToggleCircleLeftCanvasProperty, ref _toggleCircleLeftCanvas, value);
    }

    private double _toggleCircleTopCanvas;

    public static readonly DirectProperty<CustomToggleSwitch, double> ToggleCircleTopCanvasProperty = AvaloniaProperty.RegisterDirect<CustomToggleSwitch, double>(
        nameof(ToggleCircleTopCanvas), o => o.ToggleCircleTopCanvas, (o, v) => o.ToggleCircleTopCanvas = v);

    public double ToggleCircleTopCanvas
    {
        get => _toggleCircleTopCanvas;
        set => SetAndRaise(ToggleCircleTopCanvasProperty, ref _toggleCircleTopCanvas, value);
    }

    #endregion

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == WidthProperty ||
            change.Property == HeightProperty ||
            change.Property == PaddingProperty ||
            change.Property == ToggleCircleSizeProperty ||
            change.Property == ToggleCircleOffsetProperty ||
            change.Property == IsCheckedProperty)
        {
            UpdateTogglePosition();
        }
    }

    private void UpdateTogglePosition()
    {
        var paddingX = Padding.Left + Padding.Right;
        var paddingY = Padding.Top + Padding.Bottom;
        var translateX = Width - ToggleCircleSize - paddingX + ToggleCircleOffset;
        ToggleCircleLeftCanvas = IsChecked == true ? translateX : -ToggleCircleOffset;
        ToggleCircleTopCanvas = (Height - paddingY - ToggleCircleSize) / 2;
    }
}