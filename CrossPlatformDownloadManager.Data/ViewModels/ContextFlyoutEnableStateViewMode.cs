using CrossPlatformDownloadManager.Utils.PropertyChanged;

namespace CrossPlatformDownloadManager.Data.ViewModels;

public class ContextFlyoutEnableStateViewMode : PropertyChangedBase
{
    #region Private Fields

    private bool _canSelectAllRows;
    private bool _canOpenFile;
    private bool _canOpenFolder;
    private bool _canRename;
    private bool _canChangeFolder;
    private bool _canRedownload;
    private bool _canResume;
    private bool _canStop;
    private bool _canRefreshDownloadAddress;
    private bool _canRemove;
    private bool _canAddToQueue;
    private bool _canRemoveFromQueue;

    #endregion

    #region Properties

    public bool CanSelectAllRows
    {
        get => _canSelectAllRows;
        set => SetField(ref _canSelectAllRows, value);
    }

    public bool CanOpenFile
    {
        get => _canOpenFile;
        set => SetField(ref _canOpenFile, value);
    }

    public bool CanOpenFolder
    {
        get => _canOpenFolder;
        set => SetField(ref _canOpenFolder, value);
    }

    public bool CanRename
    {
        get => _canRename;
        set => SetField(ref _canRename, value);
    }

    public bool CanChangeFolder
    {
        get => _canChangeFolder;
        set => SetField(ref _canChangeFolder, value);
    }

    public bool CanRedownload
    {
        get => _canRedownload;
        set => SetField(ref _canRedownload, value);
    }

    public bool CanResume
    {
        get => _canResume;
        set => SetField(ref _canResume, value);
    }

    public bool CanStop
    {
        get => _canStop;
        set => SetField(ref _canStop, value);
    }

    public bool CanRefreshDownloadAddress
    {
        get => _canRefreshDownloadAddress;
        set => SetField(ref _canRefreshDownloadAddress, value);
    }

    public bool CanRemove
    {
        get => _canRemove;
        set => SetField(ref _canRemove, value);
    }

    public bool CanAddToQueue
    {
        get => _canAddToQueue;
        set => SetField(ref _canAddToQueue, value);
    }

    public bool CanRemoveFromQueue
    {
        get => _canRemoveFromQueue;
        set => SetField(ref _canRemoveFromQueue, value);
    }

    #endregion

    public void ChangeAllPropertiesToFalse()
    {
        CanSelectAllRows = false;
        CanOpenFile = false;
        CanOpenFolder = false;
        CanRename = false;
        CanChangeFolder = false;
        CanRedownload = false;
        CanResume = false;
        CanStop = false;
        CanRefreshDownloadAddress = false;
        CanRemove = false;
        CanAddToQueue = false;
        CanRemoveFromQueue = false;
    }
}