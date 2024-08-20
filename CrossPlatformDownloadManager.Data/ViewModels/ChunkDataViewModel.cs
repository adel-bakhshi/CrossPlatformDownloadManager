using CrossPlatformDownloadManager.Utils;

namespace CrossPlatformDownloadManager.Data.ViewModels;

public class ChunkDataViewModel
{
    public int ChunkIndex { get; set; }
    public double TotalSize { get; set; }
    public double DownloadedSize { get; set; }
    public string? Info { get; set; }

    public int RowIndex => ChunkIndex + 1;
    public string? DownloadedSizeAsFileSize => DownloadedSize.ToFileSize();
}