using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.PropertyChanged;

namespace CrossPlatformDownloadManager.Data.ViewModels;

public class ChunkDataViewModel : PropertyChangedBase
{
    #region Private Fields

    private int _chunkIndex;
    private long _totalSize;
    private long _downloadedSize;
    private string? _info;

    #endregion

    #region Properties

    public int ChunkIndex
    {
        get => _chunkIndex;
        set
        {
            if (!SetField(ref _chunkIndex, value))
                return;
            
            OnPropertyChanged(nameof(RowIndex));
        }
    }

    public long TotalSize
    {
        get => _totalSize;
        set => SetField(ref _totalSize, value);
    }

    public long DownloadedSize
    {
        get => _downloadedSize;
        set
        {
            if (!SetField(ref _downloadedSize, value))
                return;
            
            OnPropertyChanged(nameof(DownloadedSizeAsString));
        }
    }

    public string DownloadedSizeAsString => DownloadedSize.ToFileSize();

    public string? Info
    {
        get => _info;
        set => SetField(ref _info, value);
    }

    public int RowIndex => ChunkIndex + 1;

    #endregion
}