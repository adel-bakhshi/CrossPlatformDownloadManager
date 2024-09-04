namespace CrossPlatformDownloadManager.Data.ViewModels;

public class ChunkProgressViewModel
{
    public string ProgressId { get; set; } = "0";
    public long ReceivedBytesSize { get; set; }
    public long TotalBytesToReceive { get; set; }
    public int CheckCount { get; set; }
}