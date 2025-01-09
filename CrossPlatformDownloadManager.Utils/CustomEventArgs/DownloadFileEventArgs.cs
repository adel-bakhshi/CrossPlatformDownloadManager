namespace CrossPlatformDownloadManager.Utils.CustomEventArgs;

public class DownloadFileEventArgs : EventArgs
{
    #region Properties

    public int Id { get; set; }
    public bool IsSuccess { get; set; }
    public Exception? Error { get; set; }

    #endregion
}