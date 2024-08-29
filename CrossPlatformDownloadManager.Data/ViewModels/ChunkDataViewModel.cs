using CrossPlatformDownloadManager.Utils;

namespace CrossPlatformDownloadManager.Data.ViewModels;

public class ChunkDataViewModel : NotifyProperty
{
    private int _chunkIndex;

    public int ChunkIndex
    {
        get => _chunkIndex;
        set
        {
            var result = SetField(ref _chunkIndex, value);
            if (result)
                RowIndex = value + 1;
        }
    }

    private long _totalSize;

    public long TotalSize
    {
        get => _totalSize;
        set => SetField(ref _totalSize, value);
    }

    private long _downloadedSize;

    public long DownloadedSize
    {
        get => _downloadedSize;
        set
        {
            var result = SetField(ref _downloadedSize, value);
            if (result)
                DownloadedSizeAsString = value.ToFileSize();
        }
    }

    private string? _info;

    public string? Info
    {
        get => _info;
        set => SetField(ref _info, value);
    }

    private int _rowIndex;

    public int RowIndex
    {
        get => _rowIndex;
        set => SetField(ref _rowIndex, value);
    }

    private string? _downloadedSizeAsString;

    public string? DownloadedSizeAsString
    {
        get => _downloadedSizeAsString;
        set => SetField(ref _downloadedSizeAsString, value);
    }
}