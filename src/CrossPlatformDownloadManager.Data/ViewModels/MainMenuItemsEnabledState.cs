using CrossPlatformDownloadManager.Utils.PropertyChanged;

namespace CrossPlatformDownloadManager.Data.ViewModels;

public class MainMenuItemsEnabledState : PropertyChangedBase
{
    #region Private Fields

    private bool _isStartAllDownloadsEnabled;
    private bool _isStopAllDownloadsEnabled;
    private bool _isPauseAllDownloadsEnabled;
    private bool _isResumeAllDownloadsEnabled;
    private bool _isStartDownloadsEnabled;
    private bool _isStopDownloadsEnabled;
    private bool _isPauseDownloadsEnabled;
    private bool _isResumeDownloadsEnabled;
    private bool _isRedownloadEnabled;
    private bool _isDeleteDownloadsEnabled;
    private bool _isDeleteAllCompletedEnabled;

    #endregion

    #region Properties

    public bool IsStartAllDownloadsEnabled
    {
        get => _isStartAllDownloadsEnabled;
        set => SetField(ref _isStartAllDownloadsEnabled, value);
    }

    public bool IsStopAllDownloadsEnabled
    {
        get => _isStopAllDownloadsEnabled;
        set => SetField(ref _isStopAllDownloadsEnabled, value);
    }

    public bool IsPauseAllDownloadsEnabled
    {
        get => _isPauseAllDownloadsEnabled;
        set => SetField(ref _isPauseAllDownloadsEnabled, value);
    }

    public bool IsResumeAllDownloadsEnabled
    {
        get => _isResumeAllDownloadsEnabled;
        set => SetField(ref _isResumeAllDownloadsEnabled, value);
    }

    public bool IsStartDownloadsEnabled
    {
        get => _isStartDownloadsEnabled;
        set => SetField(ref _isStartDownloadsEnabled, value);
    }

    public bool IsStopDownloadsEnabled
    {
        get => _isStopDownloadsEnabled;
        set => SetField(ref _isStopDownloadsEnabled, value);
    }

    public bool IsPauseDownloadsEnabled
    {
        get => _isPauseDownloadsEnabled;
        set => SetField(ref _isPauseDownloadsEnabled, value);
    }

    public bool IsResumeDownloadsEnabled
    {
        get => _isResumeDownloadsEnabled;
        set => SetField(ref _isResumeDownloadsEnabled, value);
    }

    public bool IsRedownloadEnabled
    {
        get => _isRedownloadEnabled;
        set => SetField(ref _isRedownloadEnabled, value);
    }

    public bool IsDeleteDownloadsEnabled
    {
        get => _isDeleteDownloadsEnabled;
        set => SetField(ref _isDeleteDownloadsEnabled, value);
    }
    
    public bool IsDeleteAllCompletedEnabled
    {
        get => _isDeleteAllCompletedEnabled;
        set => SetField(ref _isDeleteAllCompletedEnabled, value);
    }

    #endregion

    public MainMenuItemsEnabledState()
    {
        IsStartAllDownloadsEnabled = true;
        IsStopAllDownloadsEnabled = true;
        IsPauseAllDownloadsEnabled = true;
        IsStartDownloadsEnabled = true;
        IsStopDownloadsEnabled = true;
        IsPauseDownloadsEnabled = true;
        IsRedownloadEnabled = true;
        IsDeleteDownloadsEnabled = true;
        IsDeleteAllCompletedEnabled = true;
    }
}