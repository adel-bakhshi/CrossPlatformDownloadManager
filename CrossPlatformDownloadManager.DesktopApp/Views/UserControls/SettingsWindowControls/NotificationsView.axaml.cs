using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace CrossPlatformDownloadManager.DesktopApp.Views.UserControls.SettingsWindowControls;

public partial class NotificationsView : UserControl
{
    #region Properties

    public static readonly StyledProperty<bool> DownloadCompleteProperty =
        AvaloniaProperty.Register<NotificationsView, bool>(
            name: nameof(DownloadComplete), defaultValue: false, defaultBindingMode: BindingMode.TwoWay);

    public bool DownloadComplete
    {
        get => GetValue(DownloadCompleteProperty);
        set => SetValue(DownloadCompleteProperty, value);
    }

    public static readonly StyledProperty<bool> DownloadStoppedProperty =
        AvaloniaProperty.Register<NotificationsView, bool>(
            name: nameof(DownloadStopped), defaultValue: false, defaultBindingMode: BindingMode.TwoWay);

    public bool DownloadStopped
    {
        get => GetValue(DownloadStoppedProperty);
        set => SetValue(DownloadStoppedProperty, value);
    }

    public static readonly StyledProperty<bool> DownloadFailedProperty =
        AvaloniaProperty.Register<NotificationsView, bool>(
            name: nameof(DownloadFailed), defaultValue: false, defaultBindingMode: BindingMode.TwoWay);

    public bool DownloadFailed
    {
        get => GetValue(DownloadFailedProperty);
        set => SetValue(DownloadFailedProperty, value);
    }
    
    public static readonly StyledProperty<bool> QueueStartedProperty =
        AvaloniaProperty.Register<NotificationsView, bool>(
            name: nameof(QueueStarted), defaultValue: false, defaultBindingMode: BindingMode.TwoWay);

    public bool QueueStarted
    {
        get => GetValue(QueueStartedProperty);
        set => SetValue(QueueStartedProperty, value);
    }

    public static readonly StyledProperty<bool> QueueStoppedProperty =
        AvaloniaProperty.Register<NotificationsView, bool>(
            name: nameof(QueueStopped), defaultValue: false, defaultBindingMode: BindingMode.TwoWay);

    public bool QueueStopped
    {
        get => GetValue(QueueStoppedProperty);
        set => SetValue(QueueStoppedProperty, value);
    }

    public static readonly StyledProperty<bool> QueueFinishedProperty =
        AvaloniaProperty.Register<NotificationsView, bool>(
            name: nameof(QueueFinished), defaultValue: false, defaultBindingMode: BindingMode.TwoWay);

    public bool QueueFinished
    {
        get => GetValue(QueueFinishedProperty);
        set => SetValue(QueueFinishedProperty, value);
    }

    public static readonly StyledProperty<bool> UseSystemNotificationsProperty =
        AvaloniaProperty.Register<NotificationsView, bool>(
            name: nameof(UseSystemNotifications), defaultValue: false, defaultBindingMode: BindingMode.TwoWay);

    public bool UseSystemNotifications
    {
        get => GetValue(UseSystemNotificationsProperty);
        set => SetValue(UseSystemNotificationsProperty, value);
    }

    #endregion

    public NotificationsView()
    {
        InitializeComponent();
    }
}