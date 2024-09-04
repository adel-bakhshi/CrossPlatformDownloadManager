using CrossPlatformDownloadManager.Utils;
using PropertyChanged;

namespace CrossPlatformDownloadManager.Data.ViewModels;

[AddINotifyPropertyChangedInterface]
public class ChunkDataViewModel
{
    public int ChunkIndex { get; set; }
    public long TotalSize { get; set; }
    public long DownloadedSize { get; set; }
    public string? DownloadedSizeAsString => DownloadedSize.ToFileSize();
    public string? Info { get; set; }
    public int RowIndex => ChunkIndex + 1;
}