namespace CrossPlatformDownloadManager.Data.ViewModels;

public class DownloadQueueTaskViewModel
{
    public int Key { get; set; }
    public DownloadFileViewModel? DownloadFile { get; set; }
    public int DownloadQueueId { get; set; }
}