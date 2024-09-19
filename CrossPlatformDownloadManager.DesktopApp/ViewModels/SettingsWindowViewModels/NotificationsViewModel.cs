using CrossPlatformDownloadManager.Data.Services.DownloadFileService;
using CrossPlatformDownloadManager.Data.UnitOfWork;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.SettingsWindowViewModels;

public class NotificationsViewModel : ViewModelBase
{
    #region Properties

    private bool _downloadComplete;

    public bool DownloadComplete
    {
        get => _downloadComplete;
        set => this.RaiseAndSetIfChanged(ref _downloadComplete, value);
    }

    private bool _downloadStopped;

    public bool DownloadStopped
    {
        get => _downloadStopped;
        set => this.RaiseAndSetIfChanged(ref _downloadStopped, value);
    }

    private bool _downloadFailed;

    public bool DownloadFailed
    {
        get => _downloadFailed;
        set => this.RaiseAndSetIfChanged(ref _downloadFailed, value);
    }

    private bool _queueStarted;

    public bool QueueStarted
    {
        get => _queueStarted;
        set => this.RaiseAndSetIfChanged(ref _queueStarted, value);
    }

    private bool _queueStopped;

    public bool QueueStopped
    {
        get => _queueStopped;
        set => this.RaiseAndSetIfChanged(ref _queueStopped, value);
    }

    private bool _queueFinished;

    public bool QueueFinished
    {
        get => _queueFinished;
        set => this.RaiseAndSetIfChanged(ref _queueFinished, value);
    }

    private bool _useSystemNotifications;

    public bool UseSystemNotifications
    {
        get => _useSystemNotifications;
        set => this.RaiseAndSetIfChanged(ref _useSystemNotifications, value);
    }

    #endregion
    
    public NotificationsViewModel(IUnitOfWork unitOfWork, IDownloadFileService downloadFileService) : base(unitOfWork, downloadFileService)
    {
    }
}