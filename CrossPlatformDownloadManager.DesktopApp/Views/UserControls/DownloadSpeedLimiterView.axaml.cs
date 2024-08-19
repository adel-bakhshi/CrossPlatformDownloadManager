using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CrossPlatformDownloadManager.DesktopApp.Views.UserControls;

public partial class DownloadSpeedLimiterView : UserControl
{
    #region Properties

    public static readonly StyledProperty<bool> SpeedLimiterEnabledProperty = AvaloniaProperty.Register<DownloadSpeedLimiterView, bool>(
        "SpeedLimiterEnabled", defaultValue: false);

    public bool SpeedLimiterEnabled
    {
        get => GetValue(SpeedLimiterEnabledProperty);
        set => SetValue(SpeedLimiterEnabledProperty, value);
    }

    #endregion
    
    public DownloadSpeedLimiterView()
    {
        InitializeComponent();
    }
}