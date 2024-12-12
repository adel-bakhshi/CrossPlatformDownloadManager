namespace CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;

public class DownloadFileErrorEventArgs : EventArgs
{
    #region Properties

    public int Id { get; set; }
    public Exception? Error { get; set; }

    #endregion
}