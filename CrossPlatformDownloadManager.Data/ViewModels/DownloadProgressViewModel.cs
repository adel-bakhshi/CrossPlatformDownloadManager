namespace CrossPlatformDownloadManager.Data.ViewModels;

public class DownloadProgressViewModel
{
    public string ProgressId { get; set; } = "";
    public double ProgressPercentage { get; set; }
    public long ReceivedBytesSize { get; set; }
    public long TotalBytesToReceive { get; set; }
    public double BytesPerSecondSpeed { get; set; }
    public double AverageBytesPerSecondSpeed { get; set; }
    public long ProgressedByteSize { get; set; }
    public byte[]? ReceivedBytes { get; set; }
    public int ActiveChunks { get; set; }
}