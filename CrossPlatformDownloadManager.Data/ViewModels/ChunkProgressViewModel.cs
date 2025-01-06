using CrossPlatformDownloadManager.Utils.PropertyChanged;

namespace CrossPlatformDownloadManager.Data.ViewModels;

public class ChunkProgressViewModel : PropertyChangedBase
{
    #region Private Fields

    private string _progressId = "0";
    private long _receivedBytesSize;
    private long _totalBytesToReceive;
    private int _checkCount;
    private bool _isCompleted;
    private bool _isCompletionChecked;

    #endregion

    #region Properties

    public string ProgressId
    {
        get => _progressId;
        set => SetField(ref _progressId, value);
    }

    public long ReceivedBytesSize
    {
        get => _receivedBytesSize;
        set => SetField(ref _receivedBytesSize, value);
    }

    public long TotalBytesToReceive
    {
        get => _totalBytesToReceive;
        set => SetField(ref _totalBytesToReceive, value);
    }

    public int CheckCount
    {
        get => _checkCount;
        set => SetField(ref _checkCount, value);
    }
    
    public bool IsCompleted
    {
        get => _isCompleted;
        set => SetField(ref _isCompleted, value);
    }
    
    public bool IsCompletionChecked
    {
        get => _isCompletionChecked;
        set => SetField(ref _isCompletionChecked, value);
    }

    #endregion
}