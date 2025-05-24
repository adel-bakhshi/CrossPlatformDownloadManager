namespace CrossPlatformDownloadManager.Utils.CustomEventArgs;

public class DownloadFileEventArgs : EventArgs
{
    #region Properties

    public int Id { get; }
    public bool IsSuccess { get; }
    public Exception? Error { get; }

    #endregion

    public DownloadFileEventArgs(int id)
    {
        Id = id;
    }

    public DownloadFileEventArgs(int id, bool isSuccess, Exception? error = null) : this(id)
    {
        IsSuccess = isSuccess;
        Error = error;
    }
}