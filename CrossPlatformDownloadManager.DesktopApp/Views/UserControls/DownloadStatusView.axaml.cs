using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace CrossPlatformDownloadManager.DesktopApp.Views.UserControls;

public partial class DownloadStatusView : UserControl
{
    #region Properties

    public static readonly StyledProperty<string?> UrlProperty = AvaloniaProperty.Register<DownloadStatusView, string?>(
        "Url");

    public string? Url
    {
        get => GetValue(UrlProperty);
        set => SetValue(UrlProperty, value);
    }

    public static readonly StyledProperty<string?> FileNameProperty =
        AvaloniaProperty.Register<DownloadStatusView, string?>(
            "FileName");

    public string? FileName
    {
        get => GetValue(FileNameProperty);
        set => SetValue(FileNameProperty, value);
    }

    public static readonly StyledProperty<string?> SaveLocationProperty =
        AvaloniaProperty.Register<DownloadStatusView, string?>(
            "SaveLocation");

    public string? SaveLocation
    {
        get => GetValue(SaveLocationProperty);
        set => SetValue(SaveLocationProperty, value);
    }

    public static readonly StyledProperty<string?> FileSizeProperty =
        AvaloniaProperty.Register<DownloadStatusView, string?>(
            "FileSize");

    public string? FileSize
    {
        get => GetValue(FileSizeProperty);
        set => SetValue(FileSizeProperty, value);
    }

    public static readonly StyledProperty<string?> DownloadedProperty =
        AvaloniaProperty.Register<DownloadStatusView, string?>(
            "Downloaded");

    public string? Downloaded
    {
        get => GetValue(DownloadedProperty);
        set => SetValue(DownloadedProperty, value);
    }

    public static readonly StyledProperty<string?> SpeedProperty =
        AvaloniaProperty.Register<DownloadStatusView, string?>(
            "Speed");

    public string? Speed
    {
        get => GetValue(SpeedProperty);
        set => SetValue(SpeedProperty, value);
    }

    public static readonly StyledProperty<string?> ElapsedTimeProperty =
        AvaloniaProperty.Register<DownloadStatusView, string?>(
            "ElapsedTime");

    public string? ElapsedTime
    {
        get => GetValue(ElapsedTimeProperty);
        set => SetValue(ElapsedTimeProperty, value);
    }

    public static readonly StyledProperty<string?> TimeLeftProperty =
        AvaloniaProperty.Register<DownloadStatusView, string?>(
            "TimeLeft");

    public string? TimeLeft
    {
        get => GetValue(TimeLeftProperty);
        set => SetValue(TimeLeftProperty, value);
    }

    public static readonly StyledProperty<string?> ResumeCapabilityProperty =
        AvaloniaProperty.Register<DownloadStatusView, string?>(
            "ResumeCapability");

    public string? ResumeCapability
    {
        get => GetValue(ResumeCapabilityProperty);
        set => SetValue(ResumeCapabilityProperty, value);
    }

    #endregion

    #region Commands

    public static readonly StyledProperty<ICommand?> OpenSaveLocationCommandProperty =
        AvaloniaProperty.Register<DownloadStatusView, ICommand?>(
            "OpenSaveLocationCommand");
    
    public ICommand? OpenSaveLocationCommand
    {
        get => GetValue(OpenSaveLocationCommandProperty);
        set => SetValue(OpenSaveLocationCommandProperty, value);
    }

    #endregion

    public DownloadStatusView()
    {
        InitializeComponent();
    }

    private void TxbSaveLocation_OnTapped(object? sender, TappedEventArgs e)
    {
        var command = this.GetValue(OpenSaveLocationCommandProperty);
        command?.Execute(null);
    }
}