using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MultipartDownloader.Core;

/// <summary>
/// Represents a buffered stream that uses shared memory for efficient data handling.
/// Implements IAsyncDisposable for proper asynchronous resource cleanup.
/// </summary>
public class SharedMemoryBufferedStream : IAsyncDisposable
{
    #region Constants

    private const long DirectFlushLimit = 50 * 1024 * 1024;

    #endregion Constants

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
    /// The concurrent dictionary for storing the received sizes of chunks.
    /// </summary>
    private readonly ConcurrentDictionary<string, long> _receivedSizes;

    /// <summary>
    /// The logger for logging data.
    /// </summary>
    private readonly ILogger? _logger;

    /// <summary>
    /// Indicates whether memory buffering is enabled.
    /// </summary>
    private readonly bool _isMemoryBufferingEnabled;

    /// <summary>
    /// The semaphore for releasing memory.
    /// </summary>
    private readonly SemaphoreSlim _releaseMemoryLock;

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
        _receivedSizes = new ConcurrentDictionary<string, long>();
        _logger = logger;
        _isMemoryBufferingEnabled = config.MaximumMemoryBufferBytes > 0;
        _releaseMemoryLock = new SemaphoreSlim(1, 1);
        _currentMemoryUsage = 0;
    }

    /// <summary>
    /// Creates a buffer for a specific chunk asynchronously.
    /// </summary>
    /// <param name="chunkId">The ID of the chunk to create a buffer for.</param>
    /// <param name="filePath">The file path associated with the chunk.</param>
    /// <exception cref="ObjectDisposedException">If the current <see cref="SharedMemoryBufferedStream"/> instance is disposed.</exception>
    public void CreateBuffer(string chunkId, string filePath, long offset, SeekOrigin origin)
    {
        // Check if the current instance is disposed
        ObjectDisposedException.ThrowIf(_disposed, this);

        _logger?.LogDebug("Creating a memory buffer for chunk {ChunkId} with file path '{FilePath}'", chunkId, filePath);

        // Get or add the chunk buffer
        var chunkBuffer = _chunkData.GetOrAdd(chunkId, id => new ChunkBuffer(id, filePath));
        // Seek to the specified offset
        chunkBuffer.Seek(offset, origin);

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

        if (!_chunkData.TryGetValue(chunkId, out var chunkData))
            throw new InvalidOperationException("Chunk memory buffer not found");

        var chunkLock = GetChunkLock(chunkId);

        try
        {
            await chunkLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            if (_isMemoryBufferingEnabled)
            {
                _logger?.LogDebug("Write {BytesLength} bytes to the chunk {ChunkId} memory buffer", count, chunkId);

                if (cancellationToken.IsCancellationRequested || _disposed)
                {
                    _logger?.LogDebug("Write bytes to the chunk {ChunkId} memory buffer was canceled or the stream is disposed", chunkId);
                    return;
                }

                // Copy buffer
                var copiedBuffer = new byte[count];
                Buffer.BlockCopy(buffer, offset, copiedBuffer, 0, count);

                // Add data to chunk
                var packet = new Packet(copiedBuffer, offset, count);
                chunkData.Packets.Enqueue(packet);

                // Update memory usage
                Interlocked.Add(ref _currentMemoryUsage, count);
            }
            else
            {
                // Write data to file stream
                await chunkData.FileStream!.WriteAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
                // Update file position
                chunkData.FilePosition = chunkData.FileStream.Position;

                // Update received size using AddOrUpdate
                _receivedSizes.AddOrUpdate(chunkId, count, (_, existingValue) => existingValue + count);

                if (_receivedSizes[chunkId] > DirectFlushLimit)
                {
                    await chunkData.FileStream!.FlushAsync(cancellationToken).ConfigureAwait(false);
                    _receivedSizes[chunkId] = 0;
                }
            }

            _logger?.LogDebug("{BytesLength} bytes added to the chunk {ChunkId} memory buffer", count, chunkId);
        }
        finally
        {
            chunkLock.Release();
        }

        // Release memory
        if (_isMemoryBufferingEnabled)
            await ReleaseMemoryAsync(cancellationToken).ConfigureAwait(false);
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

            if (_isMemoryBufferingEnabled)
            {
                _logger?.LogDebug("Flushing chunk {ChunkId} to disk", chunkId);

                if (cancellationToken.IsCancellationRequested || _disposed)
                {
                    _logger?.LogDebug("Flush chunk {ChunkId} to disk was canceled or the stream is disposed", chunkId);
                    return;
                }

                await WriteChunkToDiskAsync(chunkData, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                if (!_chunkData.TryGetValue(chunkId, out var chunk))
                {
                    _logger?.LogDebug("Chunk {ChunkId} not found", chunkId);
                    return;
                }

                // Flush data to file stream
                await chunk.FileStream!.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            chunkLock.Release();
        }
    }

    /// <summary>
    /// Gets the current file position for a chunk.
    /// </summary>
    /// <param name="chunkId">The ID of the chunk to get the file position for.</param>
    /// <returns>The current file position for the chunk.</returns>
    public async Task<long> GetChunkFilePositionAsync(string chunkId)
    {
        var chunkLock = GetChunkLock(chunkId);

        try
        {
            await chunkLock.WaitAsync().ConfigureAwait(false);
            return _chunkData.TryGetValue(chunkId, out var chunkData) ? chunkData.FilePosition : 0;
        }
        finally
        {
            chunkLock.Release();
        }
    }

    /// <summary>
    /// Sets the file position for a chunk.
    /// </summary>
    /// <param name="chunkId">The ID of the chunk to set the file position for.</param>
    /// <param name="offset">The new file position for the chunk.</param>
    /// <param name="origin">The origin of the file position.</param>
    public async Task SetChunkFilePositionAsync(string chunkId, long offset, SeekOrigin origin)
    {
        var chunkLock = GetChunkLock(chunkId);

        try
        {
            if (!_chunkData.TryGetValue(chunkId, out var chunkData))
                return;

            await chunkLock.WaitAsync().ConfigureAwait(false);

            chunkData.Seek(offset, origin);
            _logger?.LogInformation("Set chunk {ChunkId} file position to {Offset}", chunkId, offset);
        }
        finally
        {
            chunkLock.Release();
        }
    }

    /// <summary>
    /// Gets the length of the file for a chunk.
    /// </summary>
    /// <param name="chunkId">The ID of the chunk to get the file length for.</param>
    /// <returns>The length of the file for the chunk.</returns>
    public async Task<long> GetChunkFileLengthAsync(string chunkId)
    {
        var chunkLock = GetChunkLock(chunkId);

        try
        {
            if (!_chunkData.TryGetValue(chunkId, out var chunkData))
                return 0;

            await chunkLock.WaitAsync().ConfigureAwait(false);

            chunkData.CreateStreamIfNull();
            return chunkData.FileStream!.Length;
        }
        finally
        {
            chunkLock.Release();
        }
    }

    /// <summary>
    /// Sets the length of the file for a chunk.
    /// </summary>
    /// <param name="chunkId">The ID of the chunk to set the file length for.</param>
    /// <param name="length">The new length of the file for the chunk.</param>
    public async Task SetChunkFileLengthAsync(string chunkId, long length)
    {
        var chunkLock = GetChunkLock(chunkId);

        try
        {
            if (!_chunkData.TryGetValue(chunkId, out var chunkData))
                return;

            await chunkLock.WaitAsync().ConfigureAwait(false);

            chunkData.SetLength(length);
            _logger?.LogDebug("Set chunk {ChunkId} file length to {Length}", chunkId, length);
        }
        finally
        {
            chunkLock.Release();
        }
    }

    /// <summary>
    /// Gets the length of the memory buffer for a chunk.
    /// </summary>
    /// <param name="chunkId">The ID of the chunk to get the memory buffer length for.</param>
    /// <returns>The length of the memory buffer for the chunk.</returns>
    public async Task<long> GetChunkMemoryLengthAsync(string chunkId)
    {
        var chunkLock = GetChunkLock(chunkId);

        try
        {
            await chunkLock.WaitAsync().ConfigureAwait(false);
            return _chunkData.TryGetValue(chunkId, out var chunkData) ? chunkData.Packets.Sum(p => p.Length) : 0;
        }
        finally
        {
            chunkLock.Release();
        }
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
            if (_isMemoryBufferingEnabled)
            {
                _logger?.LogDebug("Flushing memory buffers to disk");
                await FlushToDiskAsync(CancellationToken.None).ConfigureAwait(false);
            }

            // Dispose all file streams
            _logger?.LogDebug("Disposing file streams");
            var disposalTasks = _chunkData.Values.Select(chunk => chunk.ClearAsync()).ToArray();
            await Task.WhenAll(disposalTasks).ConfigureAwait(false);

            _chunkData.Clear();

            // Dispose release memory lock
            _releaseMemoryLock.Dispose();

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
    /// Releases the memory used by the buffer.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ReleaseMemoryAsync(CancellationToken cancellationToken)
    {
        // Check if memory limit has been reached
        if (!IsMemoryLimitReached)
            return;

        // Try to acquire flush lock without waiting
        // If another thread is already flushing, we skip and return
        var canLock = await _releaseMemoryLock.WaitAsync(0, cancellationToken).ConfigureAwait(false);
        if (!canLock)
        {
            _logger?.LogDebug("Another thread is already flushing, skipping this flush request");
            return;
        }

        try
        {
            // Double-check after acquiring lock
            if (cancellationToken.IsCancellationRequested || _disposed || !IsMemoryLimitReached)
                return;

            _logger?.LogInformation("Memory limit reached ({CurrentUsage}/{MaxBuffer} bytes), flushing to disk...", _currentMemoryUsage, MaxMemoryBuffer);

            // Lock the chunks to prevent modifications while releasing memory
            await LockChunksAsync().ConfigureAwait(false);

            try
            {
                // Triple-check inside chunk locks
                if (IsMemoryLimitReached)
                    await FlushToDiskAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                ReleaseLocks();
            }

            _logger?.LogInformation("Memory flushed successfully, current usage: {CurrentUsage} bytes", _currentMemoryUsage);
        }
        finally
        {
            _releaseMemoryLock.Release();
        }
    }

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

        _logger?.LogInformation("Writing chunk {ChunkId} to disk", chunk.ChunkId);

        if (chunk.FileStream!.Position != chunk.FilePosition)
        {
            _logger?.LogDebug("Adjusting stream position for chunk {ChunkId}. Stream: {StreamPos}, Expected: {ExpectedPos}",
                chunk.ChunkId, chunk.FileStream.Position, chunk.FilePosition);

            chunk.Seek(chunk.FilePosition, SeekOrigin.Begin);
        }

        // Write all packets to disk
        while (chunk.Packets.TryDequeue(out var packet))
        {
            await chunk.FileStream!.WriteAsync(packet.Data, cancellationToken).ConfigureAwait(false);
            chunk.FilePosition = chunk.FileStream.Position;

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
        return _chunkLocks.GetOrAdd(chunkId, _ => new SemaphoreSlim(1));
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