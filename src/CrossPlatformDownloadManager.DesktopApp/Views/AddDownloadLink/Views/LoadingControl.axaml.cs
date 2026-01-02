using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace CrossPlatformDownloadManager.DesktopApp.Views.AddDownloadLink.Views;

public partial class LoadingControl : UserControl
{
    #region Properties

    public static readonly StyledProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.Register<LoadingControl, TimeSpan>(
            "Duration", defaultValue: TimeSpan.Zero);

    public TimeSpan Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    public static readonly StyledProperty<TimeSpan> DelayProperty = AvaloniaProperty.Register<LoadingControl, TimeSpan>(
        "Delay", defaultValue: TimeSpan.Zero);

    public TimeSpan Delay
    {
        get => GetValue(DelayProperty);
        set => SetValue(DelayProperty, value);
    }

    public static readonly StyledProperty<IBrush?> FillProperty = AvaloniaProperty.Register<LoadingControl, IBrush?>(
        "Fill");

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    #endregion

    public LoadingControl()
    {
        InitializeComponent();
    }
}