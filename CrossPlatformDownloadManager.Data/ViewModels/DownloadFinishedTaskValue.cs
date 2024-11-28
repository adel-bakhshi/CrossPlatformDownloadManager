namespace CrossPlatformDownloadManager.Data.ViewModels;

public class DownloadFinishedTaskValue
{
    #region Properties

    public bool UpdateDownloadFile { get; set; }
    public Exception? Exception { get; set; }

    #endregion
}