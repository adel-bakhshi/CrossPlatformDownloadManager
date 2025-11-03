using System.Collections.Concurrent;

namespace MultipartDownloader.Core;

/// <summary>
/// Represents a buffer for managing chunks of data with file operations.
/// </summary>
internal class ChunkBuffer
{
    #region Properties

    /// <summary>
    /// Gets the unique identifier for this chunk.
    /// </summary>
    public string ChunkId { get; }

    /// <summary>
    /// Gets the thread-safe queue of packets for this chunk.
    /// </summary>
    public ConcurrentQueue<Packet> Packets { get; }

    /// <summary>
    /// Gets the file path of the temporary chunk file.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets or sets the file stream for this chunk.
    /// </summary>
    public FileStream? FileStream { get; private set; }

    /// <summary>
    /// Gets or sets the current position in the file.
    /// </summary>
    public long FilePosition { get; set; }

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ChunkBuffer"/> class.
    /// </summary>
    /// <param name="chunkId">The unique identifier for the chunk.</param>
    /// <param name="filePath">The path to the file associated with this chunk.</param>
    public ChunkBuffer(string chunkId, string filePath)
    {
        ChunkId = chunkId;
        Packets = new ConcurrentQueue<Packet>();
        FilePath = filePath;
        FilePosition = 0;

        CreateStreamIfNull();
    }

    /// <summary>
    /// Creates a file stream if one doesn't already exist.
    /// </summary>
    public void CreateStreamIfNull()
    {
        if (FileStream != null)
            return;

        FileStream = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
        FileStream.Seek(FilePosition > 0 ? FilePosition : 0, SeekOrigin.Begin);
    }

    /// <summary>
    /// Sets the position in the current stream.
    /// </summary>
    /// <param name="offset">A byte offset relative to the origin parameter.</param>
    /// <param name="origin">Specifies the beginning, the end, or the current position as the reference point for offset.</param>
    public void Seek(long offset, SeekOrigin origin)
    {
        CreateStreamIfNull();

        FilePosition = offset;
        if (FilePosition > FileStream!.Length)
            FilePosition = FileStream.Length;

        FileStream.Seek(FilePosition, origin);
    }

    /// <summary>
    /// Sets the length of the current stream.
    /// </summary>
    /// <param name="length">The desired length of the current stream in bytes.</param>
    public void SetLength(long length)
    {
        CreateStreamIfNull();
        if (FilePosition > length)
            FilePosition = length;

        FileStream!.SetLength(length);
    }

    /// <summary>
    /// Asynchronously clears the resources used by this chunk buffer.
    /// </summary>
    public async Task ClearAsync()
    {
        if (FileStream == null)
            return;

        await FileStream.DisposeAsync().ConfigureAwait(false);
        // Wait for the file stream to be disposed before setting it to null.
        await Task.Delay(100).ConfigureAwait(false);
        FileStream = null;
    }
}