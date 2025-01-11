namespace CrossPlatformDownloadManager.Data.ViewModels.Exports;

public class ExportAddedDownloadQueueDataViewModel
{
    #region Properties

    public int NewDownloadQueueId { get; set; }
    public int? OldDownloadQueueId { get; set; }

    #endregion

    public ExportAddedDownloadQueueDataViewModel(int newDownloadQueueId, int? oldDownloadQueueId)
    {
        NewDownloadQueueId = newDownloadQueueId;
        OldDownloadQueueId = oldDownloadQueueId;
    }
}