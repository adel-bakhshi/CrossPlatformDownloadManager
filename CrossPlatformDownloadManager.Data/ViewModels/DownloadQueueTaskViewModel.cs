using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.ViewModels.DbViewModels;
using CrossPlatformDownloadManager.Utils.PropertyChanged;

namespace CrossPlatformDownloadManager.Data.ViewModels;

public class DownloadQueueTaskViewModel : PropertyChangedBase
{
    #region Private Fields

    private int _downloadQueueId;
    private int _downloadFileId;
    private DownloadFileViewModel? _downloadFile;
    private DispatcherTimer? _continueDownloadQueueTimer;

    #endregion

    #region Properties

    public int DownloadQueueId
    {
        get => _downloadQueueId;
        set => SetField(ref _downloadQueueId, value);
    }

    public int DownloadFileId
    {
        get => _downloadFileId;
        set => SetField(ref _downloadFileId, value);
    }

    public DownloadFileViewModel? DownloadFile
    {
        get => _downloadFile;
        set => SetField(ref _downloadFile, value);
    }

    public DispatcherTimer? ContinueDownloadQueueTimer
    {
        get => _continueDownloadQueueTimer;
        set => SetField(ref _continueDownloadQueueTimer, value);
    }

    #endregion
}