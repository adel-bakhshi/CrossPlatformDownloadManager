using CrossPlatformDownloadManager.Utils.PropertyChanged;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService.Models;

/// <summary>
/// Represents progress of the chunk.
/// </summary>
public class ChunkProgress : PropertyChangedBase
{
    #region Private Fields

    /// <summary>
    /// The progress id that is used to identify the progress.
    /// Equals with the chunk id.
    /// </summary>
    private string _progressId = "0";

    /// <summary>
    /// The total bytes of the chunk that received from server.
    /// </summary>
    private long _receivedBytesSize;

    /// <summary>
    /// The size of the chunk that supposed to receive.
    /// </summary>
    private long _totalBytesToReceive;

    /// <summary>
    /// Indicates whether the download of chunk is completed or not.
    /// </summary>
    private bool _isCompleted;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets a value that indicates the progress id.
    /// </summary>
    public string ProgressId
    {
        get => _progressId;
        set => SetField(ref _progressId, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the total bytes of the chunk that received from server.
    /// </summary>
    public long ReceivedBytesSize
    {
        get => _receivedBytesSize;
        set => SetField(ref _receivedBytesSize, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the size of the chunk that supposed to receive.
    /// </summary>
    public long TotalBytesToReceive
    {
        get => _totalBytesToReceive;
        set => SetField(ref _totalBytesToReceive, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates whether the download of chunk is completed or not.
    /// </summary>
    public bool IsCompleted
    {
        get => _isCompleted;
        set => SetField(ref _isCompleted, value);
    }

    #endregion
}