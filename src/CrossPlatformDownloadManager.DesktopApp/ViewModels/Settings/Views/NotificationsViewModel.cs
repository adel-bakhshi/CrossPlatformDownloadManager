using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.Settings.Views;

public class NotificationsViewModel : ViewModelBase
{
    #region Private Fields

    private bool _downloadComplete;
    private bool _downloadStopped;
    private bool _downloadFailed;
    private bool _queueStarted;
    private bool _queueStopped;
    private bool _queueFinished;
    private bool _useSystemNotifications;

    #endregion

    #region Properties

    public bool DownloadComplete
    {
        get => _downloadComplete;
        set => this.RaiseAndSetIfChanged(ref _downloadComplete, value);
    }

    public bool DownloadStopped
    {
        get => _downloadStopped;
        set => this.RaiseAndSetIfChanged(ref _downloadStopped, value);
    }

    public bool DownloadFailed
    {
        get => _downloadFailed;
        set => this.RaiseAndSetIfChanged(ref _downloadFailed, value);
    }

    public bool QueueStarted
    {
        get => _queueStarted;
        set => this.RaiseAndSetIfChanged(ref _queueStarted, value);
    }

    public bool QueueStopped
    {
        get => _queueStopped;
        set => this.RaiseAndSetIfChanged(ref _queueStopped, value);
    }

    public bool QueueFinished
    {
        get => _queueFinished;
        set => this.RaiseAndSetIfChanged(ref _queueFinished, value);
    }

    public bool UseSystemNotifications
    {
        get => _useSystemNotifications;
        set => this.RaiseAndSetIfChanged(ref _useSystemNotifications, value);
    }

    #endregion

    public NotificationsViewModel(IAppService appService) : base(appService)
    {
        LoadViewData();
    }

    #region Helpers

    private void LoadViewData()
    {
        var settings = AppService.SettingsService.Settings;
        DownloadComplete = settings.UseDownloadCompleteSound;
        DownloadStopped = settings.UseDownloadStoppedSound;
        DownloadFailed = settings.UseDownloadFailedSound;
        QueueStarted = settings.UseQueueStartedSound;
        QueueStopped = settings.UseQueueStoppedSound;
        QueueFinished = settings.UseQueueFinishedSound;
        UseSystemNotifications = settings.UseSystemNotifications;
    }

    #endregion
}