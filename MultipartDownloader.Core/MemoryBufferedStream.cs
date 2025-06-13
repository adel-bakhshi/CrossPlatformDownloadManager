namespace MultipartDownloader.Core;

/// <summary>
/// This class is a wrapper around a <see cref="Stream"/> that provides a memory buffer for reading and writing data.
/// </summary>
public class MemoryBufferedStream
{
    #region Private fields

    /// <summary>
    /// The base stream that <see cref="MemoryBufferedStream"/> class wraps.
    /// </summary>
    private readonly Stream _baseStream;

    /// <summary>
    /// The write blocker to prevent multiple write operations at the same time.
    /// </summary>
    private readonly SemaphoreSlim _writeSemaphore;

    /// <summary>
    /// The queue containing the memory packets.
    /// </summary>
    private readonly Queue<Packet> _memoryPackets;

    /// <summary>
    /// The dispose flag to indicate whether the stream has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// The maximum amount of data that can be buffered in memory.
    /// </summary>
    private long _maxMemoryBuffer;

    #endregion Private fields

    #region Properties

    /// <summary>
    /// Gets a value that indicates whether the current stream supports reading.
    /// </summary>
    public bool CanRead => _baseStream.CanRead;

    /// <summary>
    /// Gets a value that indicates whether the current stream supports seeking.
    /// </summary>
    public bool CanSeek => _baseStream.CanSeek;

    /// <summary>
    /// Gets a value that indicates whether the current stream supports writing.
    /// </summary>
    public bool CanWrite => _baseStream.CanWrite;

    /// <summary>
    /// Gets a value that indicates the length of the stream in bytes.
    /// </summary>
    public long Length => _baseStream.Length;

    /// <summary>
    /// Gets a value that indicates the current position of the file that the data is being written to.
    /// </summary>
    public long Position { get; private set; }

    /// <summary>
    /// Gets a value that indicates the length of the data that stored in memory in bytes.
    /// </summary>
    public long MemoryLength => _memoryPackets.Sum(p => p.Length);

    /// <summary>
    /// Gets or sets a value that indicates the maximum amount of data that can be buffered in memory.
    /// <para>
    /// The value must be in bytes. The default value is 0, which means that the stream will not buffer data in memory.
    /// </para>
    /// </summary>
    public long MaxMemoryBuffer
    {
        get => _maxMemoryBuffer;
        set => _maxMemoryBuffer = value < 0 ? 0 : value;
    }

    #endregion Properties

    /// <summary>
    /// Creates a new instance of the <see cref="MemoryBufferedStream"/> class.
    /// </summary>
    /// <param name="filePath">The path to the file that the data is being written to.</param>
    /// <param name="maxMemoryBuffer">The maximum amount of data that can be buffered in memory.</param>
    /// <param name="initialPosition">The initial position of the stream.</param>
    public MemoryBufferedStream(string filePath, long maxMemoryBuffer, long initialPosition = 0)
    {
        _writeSemaphore = new SemaphoreSlim(1);
        _memoryPackets = [];
        _baseStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite) { Position = 0 };

        MaxMemoryBuffer = maxMemoryBuffer;
        Position = initialPosition;
        // Check if the file position is greater than 0 and if so, seek to that position
        if (Position > 0)
            Seek(Position, SeekOrigin.Begin);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="MemoryBufferedStream"/> class.
    /// </summary>
    /// <param name="baseStream">The stream that the data is being written to.</param>
    /// <param name="maxMemoryBuffer">The maximum amount of data that can be buffered in memory.</param>
    /// <param name="initialPosition">The initial position of the stream.</param>
    public MemoryBufferedStream(Stream baseStream, long maxMemoryBuffer, long initialPosition = 0)
    {
        _writeSemaphore = new SemaphoreSlim(1);
        _memoryPackets = [];
        _baseStream = baseStream;
        _baseStream.Position = 0;

        MaxMemoryBuffer = maxMemoryBuffer;
        Position = initialPosition;
        // Check if the file position is greater than 0 and if so, seek to that position
        if (Position > 0)
            Seek(Position, SeekOrigin.Begin);
    }

    /// <summary>
    /// Flushes the stream and writes the data that still exists in the memory.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the asynchronous operation.</param>
    public async Task FlushAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Wait for write operation to complete
            await _writeSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            // Write all data stored in memory to the disk
            await WriteToDiskAsync(cancellationToken).ConfigureAwait(false);
            // Flush base stream
            await _baseStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // Release the semaphore
            _writeSemaphore.Release();
        }
    }

    /// <summary>
    /// Seeks to a specific position of the stream.
    /// </summary>
    /// <param name="offset">The offset to seek to.</param>
    /// <param name="origin">The origin to seek from.</param>
    public void Seek(long offset, SeekOrigin origin)
    {
        Position = offset;
        // Check file position
        if (Position > _baseStream.Length)
            Position = _baseStream.Length;

        _baseStream.Seek(Position, origin);
    }

    /// <summary>
    /// Sets the length of the stream.
    /// </summary>
    /// <param name="value">The length value to set.</param>
    public void SetLength(long value)
    {
        // Check file position
        if (Position > value)
            Position = value;

        _baseStream.SetLength(value);
    }

    /// <summary>
    /// Writes data to the stream.
    /// </summary>
    /// <param name="buffer">The buffered data to write.</param>
    /// <param name="offset">The offset to start writing from.</param>
    /// <param name="count">The number of bytes to write.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the asynchronous operation.</param>
    public async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        try
        {
            // Wait for write operation to finish
            await _writeSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            // Check if the cancellation token was requested or the stream was disposed
            if (cancellationToken.IsCancellationRequested || _disposed)
                return;

            // Create packet
            var packet = new Packet(buffer, offset, count);
            // If there is no memory limit, write the packet directly to disk.
            if (MaxMemoryBuffer == 0)
            {
                // Write packet
                await WritePacketAsync(packet, cancellationToken).ConfigureAwait(false);
            }
            // Otherwise, we wait until the memory limit is reached and then write the values to disk.
            else
            {
                // Write data to memory
                _memoryPackets.Enqueue(packet);
                // Check for memory limit size
                if (MemoryLength >= MaxMemoryBuffer)
                    await WriteToDiskAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            // Release the semaphore
            _writeSemaphore.Release();
        }
    }

    /// <summary>
    /// Disposes the <see cref="MemoryBufferedStream"/> object.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        // Make sure the stream is not disposed yet
        if (_disposed)
            return;

        try
        {
            // Block the write operation to ensure that all data is written to the memory buffer
            await _writeSemaphore.WaitAsync().ConfigureAwait(false);
            // Write remaining data to disk
            await WriteToDiskAsync(CancellationToken.None).ConfigureAwait(false);
            // Dispose the base stream
            await _baseStream.DisposeAsync().ConfigureAwait(false);
            // Dispose the memory buffer
            _memoryPackets.Clear();
            // Set the disposed flag to true
            _disposed = true;
        }
        finally
        {
            // Release and dispose the write blocker
            _writeSemaphore.Release();
            _writeSemaphore.Dispose();
        }
    }

    #region Helpers

    /// <summary>
    /// Writes the data from memory to disk.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the asynchronous operation.</param>
    private async Task WriteToDiskAsync(CancellationToken cancellationToken = default)
    {
        // Check memory size
        if (MemoryLength == 0)
            return;

        // Seek to the current position of the file
        Seek(Position, SeekOrigin.Begin);
        // Copy memory data to disk
        while (_memoryPackets.Count > 0)
        {
            // Get first item from queue
            var packet = _memoryPackets.Dequeue();
            // Write packet
            await WritePacketAsync(packet, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Writes a packet data to the storage.
    /// </summary>
    /// <param name="packet">The packet to write.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the asynchronous operation.</param>
    private async Task WritePacketAsync(Packet packet, CancellationToken cancellationToken = default)
    {
        // Write data to disk
        await _baseStream.WriteAsync(packet.Data, cancellationToken).ConfigureAwait(false);
        // Update file position
        Position += packet.Length;
        // Clear packet
        packet.Clear();
    }

    #endregion Helpers
}