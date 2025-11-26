using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MultipartDownloader.Core;

/// <summary>
/// Represents a buffered stream that uses shared memory for efficient data handling.
/// Implements IAsyncDisposable for proper asynchronous resource cleanup.
/// </summary>
public class SharedMemoryBufferedStream : IAsyncDisposable
{
    #region Private fields

    /// <summary>
    /// The download configuration.
    /// </summary>
    private readonly DownloadConfiguration _configuration;

    /// <summary>
    /// The concurrent dictionary for storing <see cref="ChunkBuffer"/> data.
    /// </summary>
    private readonly ConcurrentDictionary<string, ChunkBuffer> _chunkData;

    /// <summary>
    /// The semaphore slims for handling multiple operations.
    /// </summary>
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _chunkLocks;

    /// <summary>
    /// The logger for logging data.
    /// </summary>
    private readonly ILogger? _logger;

    /// <summary>
    /// Indicates whether the current <see cref="SharedMemoryBufferedStream"/> instance is disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// The total bytes that saved in the memory.
    /// </summary>
    private long _currentMemoryUsage;

    #endregion Private fields

    #region Properties

    /// <summary>
    /// Gets the maximum memory buffer size in bytes.
    /// </summary>
    public long MaxMemoryBuffer => _configuration.MaximumMemoryBufferBytes;

    /// <summary>
    /// Gets the current memory usage in bytes.
    /// </summary>
    public long CurrentMemoryUsage => _currentMemoryUsage;

    /// <summary>
    /// Gets a value indicating whether the memory limit has been reached.
    /// </summary>
    public bool IsMemoryLimitReached => _currentMemoryUsage >= MaxMemoryBuffer;

    #endregion Properties

    /// <summary>
    /// Initializes a new instance of the <see cref="SharedMemoryBufferedStream"/> class.
    /// </summary>
    /// <param name="config">The download configuration to use.</param>
    /// <param name="logger">The logger to use for logging.</param>
    public SharedMemoryBufferedStream(DownloadConfiguration config, ILogger? logger)
    {
        ArgumentNullException.ThrowIfNull(config);

        // Initialize fields
        _configuration = config;
        _chunkData = new ConcurrentDictionary<string, ChunkBuffer>();
        _chunkLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
        _logger = logger;
        _currentMemoryUsage = 0;
    }

    /// <summary>
    /// Creates a buffer for a specific chunk asynchronously.
    /// </summary>
    /// <param name="chunkId">The ID of the chunk to create a buffer for.</param>
    /// <param name="filePath">The file path associated with the chunk.</param>
    /// <param name="offset">The offset to seek to in the buffer.</param>
    /// <param name="origin">The origin of the seek operation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="ObjectDisposedException">If the current <see cref="SharedMemoryBufferedStream"/> instance is disposed.</exception>
    public void CreateBuffer(string chunkId, string filePath, long offset, SeekOrigin origin, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _logger?.LogDebug("Creating a memory buffer for chunk {ChunkId} with file path '{FilePath}'", chunkId, filePath);

        if (cancellationToken.IsCancellationRequested || _disposed)
            return;

        var buffer = _chunkData.GetOrAdd(chunkId, id => new ChunkBuffer(id, filePath));
        buffer.Seek(offset, origin);

        _logger?.LogDebug("Memory buffer created for chunk {ChunkId} with file path '{FilePath}'", chunkId, filePath);
    }

    /// <summary>
    /// Writes data to buffer for a specific chunk.
    /// </summary>
    /// <param name="chunkId">The ID of the chunk to write data to.</param>
    /// <param name="buffer">The data to write to the buffer.</param>
    /// <param name="offset">The offset in the buffer to start writing data.</param>
    /// <param name="count">The number of bytes to write to the buffer.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="ObjectDisposedException">If the current <see cref="SharedMemoryBufferedStream"/> instance is disposed.</exception>
    /// <exception cref="InvalidOperationException">If the chunk is not found.</exception>
    public async Task WriteAsync(string chunkId, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var chunkLock = GetChunkLock(chunkId);

        try
        {
            await chunkLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            _logger?.LogDebug("Write {BytesLength} bytes to the chunk {ChunkId} memory buffer", count, chunkId);

            if (cancellationToken.IsCancellationRequested || _disposed)
            {
                _logger?.LogDebug("Write bytes to the chunk {ChunkId} memory buffer was canceled or the stream is disposed", chunkId);
                return;
            }

            if (!_chunkData.TryGetValue(chunkId, out var chunkData))
                throw new InvalidOperationException("Chunk memory buffer not found");

            // Add data to chunk
            var packet = new Packet(buffer, offset, count);
            chunkData.Packets.Enqueue(packet);

            // Update memory usage
            Interlocked.Add(ref _currentMemoryUsage, count);

            // Check if we need to flush to disk
            if (IsMemoryLimitReached)
                await FlushToDiskAsync(cancellationToken).ConfigureAwait(false);

            _logger?.LogDebug("{BytesLength} bytes added to the chunk {ChunkId} memory buffer", count, chunkId);
        }
        finally
        {
            chunkLock.Release();
        }
    }

    /// <summary>
    /// Flushes all data for a specific chunk to disk.
    /// </summary>
    /// <param name="chunkId">The ID of the chunk to flush data for.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task FlushChunkAsync(string chunkId, CancellationToken cancellationToken)
    {
        if (_disposed || !_chunkData.TryGetValue(chunkId, out var chunkData))
            return;

        var chunkLock = GetChunkLock(chunkId);

        try
        {
            await chunkLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            _logger?.LogDebug("Flushing chunk {ChunkId} to disk", chunkId);

            if (cancellationToken.IsCancellationRequested || _disposed)
            {
                _logger?.LogDebug("Flush chunk {ChunkId} to disk was canceled or the stream is disposed", chunkId);
                return;
            }

            await WriteChunkToDiskAsync(chunkData, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            chunkLock.Release();
        }
    }

    /// <summary>
    /// Flushes all chunks to disk.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task FlushAllAsync(CancellationToken cancellationToken)
    {
        if (_disposed)
            return;

        try
        {
            await LockChunksAsync().ConfigureAwait(false);

            _logger?.LogDebug("Flushing all chunks to disk");

            if (cancellationToken.IsCancellationRequested || _disposed)
            {
                _logger?.LogDebug("Flush all chunks to disk was canceled or the stream is disposed");
                return;
            }

            await FlushToDiskAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ReleaseLocks();
        }
    }

    /// <summary>
    /// Gets the current file position for a chunk.
    /// </summary>
    /// <param name="chunkId">The ID of the chunk to get the file position for.</param>
    /// <returns>The current file position for the chunk.</returns>
    public long GetChunkFilePosition(string chunkId)
    {
        return _chunkData.TryGetValue(chunkId, out var chunkData) ? chunkData.FilePosition : 0;
    }

    /// <summary>
    /// Sets the file position for a chunk.
    /// </summary>
    /// <param name="chunkId">The ID of the chunk to set the file position for.</param>
    /// <param name="offset">The new file position for the chunk.</param>
    /// <param name="origin">The origin of the file position.</param>
    public void SetChunkFilePosition(string chunkId, long offset, SeekOrigin origin)
    {
        if (!_chunkData.TryGetValue(chunkId, out var chunkData))
            return;

        chunkData.Seek(offset, SeekOrigin.Begin);
        _logger?.LogDebug("Set chunk {ChunkId} file position to {Offset}", chunkId, offset);
    }

    /// <summary>
    /// Gets the length of the file for a chunk.
    /// </summary>
    /// <param name="chunkId">The ID of the chunk to get the file length for.</param>
    /// <returns>The length of the file for the chunk.</returns>
    public long GetChunkFileLength(string chunkId)
    {
        if (!_chunkData.TryGetValue(chunkId, out var chunkData))
            return 0;

        chunkData.CreateStreamIfNull();
        return chunkData.FileStream!.Length;
    }

    /// <summary>
    /// Sets the length of the file for a chunk.
    /// </summary>
    /// <param name="chunkId">The ID of the chunk to set the file length for.</param>
    /// <param name="length">The new length of the file for the chunk.</param>
    public void SetChunkFileLength(string chunkId, long length)
    {
        if (!_chunkData.TryGetValue(chunkId, out var chunkData))
            return;

        chunkData.SetLength(length);
        _logger?.LogDebug("Set chunk {ChunkId} file length to {Length}", chunkId, length);
    }

    /// <summary>
    /// Gets the length of the memory buffer for a chunk.
    /// </summary>
    /// <param name="chunkId">The ID of the chunk to get the memory buffer length for.</param>
    /// <returns>The length of the memory buffer for the chunk.</returns>
    public long GetChunkMemoryLength(string chunkId)
    {
        return _chunkData.TryGetValue(chunkId, out var chunkData) ? chunkData.Packets.Sum(p => p.Length) : 0;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        try
        {
            _logger?.LogDebug("Disposing SharedMemoryBufferedStream");
            await LockChunksAsync().ConfigureAwait(false);

            // Flush all remaining data to disk
            _logger?.LogDebug("Flushing memory buffers to disk");
            await FlushToDiskAsync(CancellationToken.None).ConfigureAwait(false);

            // Dispose all file streams
            _logger?.LogDebug("Disposing file streams");
            var disposalTasks = _chunkData.Values.Select(chunk => chunk.ClearAsync()).ToArray();
            await Task.WhenAll(disposalTasks).ConfigureAwait(false);

            // Add a small delay to ensure file is fully written
            await Task.Delay(100).ConfigureAwait(false);

            _chunkData.Clear();

            GC.SuppressFinalize(this);

            // Set disposed flag to true
            _disposed = true;
            _logger?.LogDebug("SharedMemoryBufferedStream disposed successfully");
        }
        finally
        {
            ReleaseLocks();
            DisposeLocks();
        }
    }

    #region Helpers

    /// <summary>
    /// Flushes all chunks to disk.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    private async Task FlushToDiskAsync(CancellationToken cancellationToken)
    {
        _logger?.LogDebug("Flushing memory data to disk");

        // Get all chunks with data
        var chunksWithData = _chunkData.Values.Where(c => !c.Packets.IsEmpty).ToList();
        // Write data of each chunk to disk
        foreach (var chunkData in chunksWithData.TakeWhile(_ => !cancellationToken.IsCancellationRequested))
            await WriteChunkToDiskAsync(chunkData, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes the data of a chunk to disk.
    /// </summary>
    /// <param name="chunk">The chunk to write to disk.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    private async Task WriteChunkToDiskAsync(ChunkBuffer chunk, CancellationToken cancellationToken)
    {
        if (chunk.Packets.IsEmpty)
            return;

        _logger?.LogDebug("Writing chunk {ChunkId} to disk", chunk.ChunkId);

        // Ensure file stream is created
        chunk.CreateStreamIfNull();
        chunk.Seek(chunk.FilePosition, SeekOrigin.Begin);

        // Write all packets to disk
        while (chunk.Packets.TryDequeue(out var packet))
        {
            await chunk.FileStream!.WriteAsync(packet.Data, cancellationToken).ConfigureAwait(false);
            chunk.FilePosition += packet.Length;

            // Update memory usage
            Interlocked.Add(ref _currentMemoryUsage, -packet.Length);

            packet.Clear();
        }

        // Flush the file stream to ensure data is written to disk
        await chunk.FileStream!.FlushAsync(cancellationToken).ConfigureAwait(false);
        _logger?.LogDebug("Chunk {ChunkId} written to disk", chunk.ChunkId);
    }

    /// <summary>
    /// Gets the chunk lock for managing asynchronous operations.
    /// </summary>
    /// <param name="chunkId">The ID of the chunk to get lock for it.</param>
    /// <returns>Returns the chunk lock for managing asynchronous operations.</returns>
    private SemaphoreSlim GetChunkLock(string chunkId)
    {
        return _chunkLocks.GetOrAdd(chunkId, _ => new SemaphoreSlim(1, 1));
    }

    /// <summary>
    /// Locks all chunks.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task LockChunksAsync()
    {
        if (_disposed || _chunkLocks.IsEmpty)
            return;

        var tasks = _chunkLocks.Values.Select(l => l.WaitAsync()).ToList();
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>
    /// Release all chunk locks.
    /// </summary>
    private void ReleaseLocks()
    {
        if (_disposed || _chunkLocks.IsEmpty)
            return;

        foreach (var chunkLock in _chunkLocks.Values)
            chunkLock.Release();
    }

    /// <summary>
    /// Disposes all chunk locks.
    /// </summary>
    private void DisposeLocks()
    {
        if (_chunkLocks.IsEmpty)
            return;

        foreach (var chunkLock in _chunkLocks.Values)
            chunkLock.Dispose();
    }

    #endregion Helpers
}