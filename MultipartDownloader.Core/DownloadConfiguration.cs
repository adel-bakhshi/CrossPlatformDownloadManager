using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MultipartDownloader.Core;

/// <summary>
/// Represents the configuration settings for a download operation.
/// </summary>
public class DownloadConfiguration : ICloneable, INotifyPropertyChanged
{
    private int _activeChunks = 1; // number of active chunks
    private int _bufferBlockSize = 1024; // usually, hosts support max to 8000 bytes
    private int _chunkCount = 1; // file parts to download
    private long _maximumBytesPerSecond = ThrottledStream.Infinite; // No-limitation in download speed
    private int _maximumTryAgainOnFailure = int.MaxValue; // the maximum number of times to fail.
    private long _maximumMemoryBufferBytes; // The maximum memory buffer length before write data to disk
    private bool _checkDiskSizeBeforeDownload = true; // check disk size for temp and file path
    private bool _parallelDownload; // download parts of file as parallel or not
    private int _parallelCount; // number of parallel downloads
    private int _timeout = 1000; // timeout (millisecond) per stream block reader
    private bool _clearPackageOnCompletionWithFailure; // Clear package and downloaded data when download completed with failure
    private long _minimumSizeOfChunking = 512; // minimum size of chunking to download a file in multiple parts
    private bool _reserveStorageSpaceBeforeStartingDownload; // Before starting the download, reserve the storage space of the file as file size.
    private bool _enableLiveStreaming; // Get on demand downloaded data with ReceivedBytes on downloadProgressChanged event
    private string _chunkFilesOutputDirectory = string.Empty; // Directory to store downloaded data for each chunk for merging at the end of the download
    private int _maximumRestartWithoutClearTempFile = int.MaxValue; // The maximum number of times to restart the download without clearing the temporary files.
    private long _maximumBytesPerSecondForMerge = ThrottledStream.Infinite; // The maximum bytes per second speed for merging the final file.

    /// <summary>
    /// To bind view models to fire changes in MVVM pattern
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged = delegate { };

    /// <summary>
    /// Notify every change of configuration properties.
    /// </summary>
    protected virtual void OnPropertyChanged<T>(ref T field, T newValue, [CallerMemberName] string? name = null)
    {
        if (field?.Equals(newValue) == true)
            return;

        field = newValue;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Gets the number of active chunks.
    /// </summary>
    public int ActiveChunks
    {
        get => _activeChunks;
        internal set => OnPropertyChanged(ref _activeChunks, value);
    }

    /// <summary>
    /// Gets or sets the stream buffer size which is used for the size of blocks.
    /// </summary>
    public int BufferBlockSize
    {
        get => (int)Math.Min(MaximumSpeedPerChunk, _bufferBlockSize);
        set
        {
            if (value is < 1 or > 1048576) // 1MB = 1024 * 1024 bytes
                throw new ArgumentOutOfRangeException(nameof(BufferBlockSize), "Buffer block size must be between 1 byte and 1024 KB.");

            OnPropertyChanged(ref _bufferBlockSize, value);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to check the disk available size for the download file before starting the download.
    /// </summary>
    public bool CheckDiskSizeBeforeDownload
    {
        get => _checkDiskSizeBeforeDownload;
        set => OnPropertyChanged(ref _checkDiskSizeBeforeDownload, value);
    }

    /// <summary>
    /// Gets or sets the file chunking parts count.
    /// </summary>
    public int ChunkCount
    {
        get => _chunkCount;
        set
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(ChunkCount), "Chunk count must be greater than 0.");

            OnPropertyChanged(ref _chunkCount, Math.Max(1, value));
        }
    }

    /// <summary>
    /// Gets or sets the maximum bytes per second that can be transferred through the base stream.
    /// </summary>
    public long MaximumBytesPerSecond
    {
        get => _maximumBytesPerSecond;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(MaximumBytesPerSecond), "Maximum bytes per second cannot be negative.");

            OnPropertyChanged(ref _maximumBytesPerSecond, value <= 0 ? long.MaxValue : value);
        }
    }

    /// <summary>
    /// Gets the maximum bytes per second that can be transferred through the base stream at each chunk downloader.
    /// This property is read-only.
    /// </summary>
    public long MaximumSpeedPerChunk => MaximumBytesPerSecond / Math.Max(Math.Min(Math.Min(ChunkCount, ParallelCount), ActiveChunks), 1);

    /// <summary>
    /// Gets or sets the maximum number of times to try again to download on failure.
    /// </summary>
    public int MaxTryAgainOnFailure
    {
        get => _maximumTryAgainOnFailure;
        set => OnPropertyChanged(ref _maximumTryAgainOnFailure, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to download file chunks in parallel or serially.
    /// </summary>
    public bool ParallelDownload
    {
        get => _parallelDownload;
        set => OnPropertyChanged(ref _parallelDownload, value);
    }

    /// <summary>
    /// Gets or sets the count of chunks to download in parallel.
    /// If ParallelCount is less than or equal to 0, then ParallelCount is equal to ChunkCount.
    /// </summary>
    public int ParallelCount
    {
        get => _parallelCount <= 0 ? ChunkCount : _parallelCount;
        set => OnPropertyChanged(ref _parallelCount, value);
    }

    /// <summary>
    /// Gets or sets the custom body of your requests.
    /// </summary>
    public RequestConfiguration RequestConfiguration { get; set; } = new(); // default requests configuration

    /// <summary>
    /// Gets or sets the download timeout per stream file blocks.
    /// </summary>
    public int Timeout
    {
        get => _timeout;
        set
        {
            if (value < 100)
                throw new ArgumentOutOfRangeException(nameof(Timeout), "Timeout must be at least 100 milliseconds.");

            OnPropertyChanged(ref _timeout, value);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to clear the package and downloaded data when the download completes with failure.
    /// </summary>
    public bool ClearPackageOnCompletionWithFailure
    {
        get => _clearPackageOnCompletionWithFailure;
        set => OnPropertyChanged(ref _clearPackageOnCompletionWithFailure, value);
    }

    /// <summary>
    /// Gets or sets the minimum size of chunking and multiple part downloading.
    /// </summary>
    public long MinimumSizeOfChunking
    {
        get => _minimumSizeOfChunking;
        set => OnPropertyChanged(ref _minimumSizeOfChunking, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to reserve the storage space of the file as file size before starting the download.
    /// Default value is false.
    /// </summary>
    public bool ReserveStorageSpaceBeforeStartingDownload
    {
        get => _reserveStorageSpaceBeforeStartingDownload;
        set => OnPropertyChanged(ref _reserveStorageSpaceBeforeStartingDownload, value);
    }

    /// <summary>
    /// <para>
    /// Gets or sets the maximum amount of memory, in bytes, that the Downloader library is allowed
    /// to allocate for buffering downloaded content. Once this limit is reached, the library will
    /// stop downloading and start writing the buffered data to a file stream before continuing.
    /// The default value is 0, which indicates unlimited buffering.
    /// </para>
    ///
    /// <para>
    /// This setting is particularly useful when:
    /// 1. Downloading large files on systems with limited memory
    /// 2. Preventing out-of-memory exceptions during parallel downloads
    /// 3. Optimizing memory usage for long-running download operations
    /// </para>
    ///
    /// <para>
    /// Recommended values:
    /// - For systems with 4GB RAM: 256MB (268435456 bytes)
    /// - For systems with 8GB RAM: 512MB (536870912 bytes)
    /// - For systems with 16GB RAM: 1GB (1073741824 bytes)
    /// </para>
    ///
    /// Note: Setting this value too low may impact download performance due to frequent disk I/O operations.
    /// </summary>
    /// <example>
    /// The following example sets the maximum memory buffer to 50 MB:
    /// <code>
    /// MaximumMemoryBufferBytes = 1024 * 1024 * 50
    /// </code>
    /// </example>
    public long MaximumMemoryBufferBytes
    {
        get => _maximumMemoryBufferBytes;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(MaximumMemoryBufferBytes), "Maximum memory buffer bytes cannot be negative");

            OnPropertyChanged(ref _maximumMemoryBufferBytes, value);
        }
    }

    /// <summary>
    /// Gets the maximum memory buffer bytes per chunk through download chunk.
    /// This property indicates how many bytes can be saved in memory before write on disk operation.
    /// This property is read-only.
    /// </summary>
    public long MaximumMemoryBufferBytesPerChunk => GetMaximumMemoryBufferBytesPerChunk();

    /// <summary>
    /// <para>
    /// Gets or sets a value indicating whether live-streaming is enabled or not. If it's enabled, get the on-demand downloaded data
    /// with ReceivedBytes on the downloadProgressChanged event.
    /// </para>
    ///
    /// <para>
    /// Important considerations:
    /// 1. Enabling this option will increase memory usage as each downloaded block is copied to the ReceivedBytes buffer
    /// 2. The memory impact is proportional to the BufferBlockSize and download speed
    /// 3. For large files, consider setting MaximumMemoryBufferBytes to limit memory usage
    /// 4. This feature is best used when you need to process downloaded data in real-time
    /// </para>
    ///
    /// Default value is false.
    /// </summary>
    public bool EnableLiveStreaming
    {
        get => _enableLiveStreaming;
        set => OnPropertyChanged(ref _enableLiveStreaming, value);
    }

    /// <summary>
    /// Gets or sets the directory to store downloaded data for each chunk for merging at the end of the download.
    /// </summary>
    public string ChunkFilesOutputDirectory
    {
        get => _chunkFilesOutputDirectory;
        set => OnPropertyChanged(ref _chunkFilesOutputDirectory, value);
    }

    /// <summary>
    /// Gets or sets the maximum number of times to restart the download without clearing the temporary files.
    /// </summary>
    public int MaxRestartWithoutClearTempFile
    {
        get => _maximumRestartWithoutClearTempFile;
        set => OnPropertyChanged(ref _maximumRestartWithoutClearTempFile, value);
    }

    /// <summary>
    /// Gets or sets the maximum bytes per second speed for merging the final file.
    /// </summary>
    public long MaximumBytesPerSecondForMerge
    {
        get => _maximumBytesPerSecondForMerge;
        set => OnPropertyChanged(ref _maximumBytesPerSecondForMerge, value);
    }

    /// <summary>
    /// Creates a shallow copy of the current object.
    /// </summary>
    /// <returns>A shallow copy of the current object.</returns>
    public object Clone()
    {
        return MemberwiseClone();
    }

    #region Helpers

    /// <summary>
    /// Gets the maximum memory buffer bytes per chunk.
    /// </summary>
    /// <returns>The maximum memory buffer bytes per chunk. </returns>
    private long GetMaximumMemoryBufferBytesPerChunk()
    {
        if (MaximumMemoryBufferBytes <= 0)
            return 0;

        return ParallelDownload
            ? MaximumMemoryBufferBytes / Math.Max(Math.Min(Math.Min(ChunkCount, ParallelCount), ActiveChunks), 1)
            : MaximumMemoryBufferBytes;
    }

    #endregion Helpers
}