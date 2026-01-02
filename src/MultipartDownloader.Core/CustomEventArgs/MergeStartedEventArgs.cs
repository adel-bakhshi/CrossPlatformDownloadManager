namespace MultipartDownloader.Core.CustomEventArgs;

public class MergeStartedEventArgs
{
    #region Properties

    /// <summary>
    /// Gets total size of the file.
    /// </summary>
    public long TotalFileSize { get; }

    /// <summary>
    /// Gets the number of downloaded chunks.
    /// </summary>
    public int NumberOfChunks { get; }

    /// <summary>
    /// Gets the directory path where the chunks are saved.
    /// </summary>
    public string ChunksDirectoryPath { get; } = string.Empty;

    #endregion Properties

    public MergeStartedEventArgs(long totalFileSize, int numberOfChunks, string? chunkDirectoryPath)
    {
        TotalFileSize = totalFileSize;
        NumberOfChunks = numberOfChunks;
        ChunksDirectoryPath = chunkDirectoryPath ?? string.Empty;
    }
}