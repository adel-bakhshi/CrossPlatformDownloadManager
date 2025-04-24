namespace MultipartDownloader.Core;

public class MemoryBufferedStream
{
    #region Private fields

    private readonly Stream _baseStream;
    private readonly SemaphoreSlim _writeSemaphore;
    private readonly Queue<Packet> _memoryPackets;
    private long _filePosition;
    private bool _disposed;

    private long _maxMemoryBuffer;

    #endregion Private fields

    #region Properties

    public bool CanRead => _baseStream.CanRead;

    public bool CanSeek => _baseStream.CanSeek;

    public bool CanWrite => _baseStream.CanWrite;

    public long Length => _baseStream.Length;

    public long Position => _filePosition;

    public long MemoryLength => _memoryPackets.Sum(p => p.Length);

    public long MaxMemoryBuffer
    {
        get => _maxMemoryBuffer;
        set => _maxMemoryBuffer = value < 0 ? 0 : value;
    }

    #endregion Properties

    public MemoryBufferedStream(string filePath, long maxMemoryBuffer, long initialPosition = 0)
    {
        _writeSemaphore = new SemaphoreSlim(1, 1);
        _memoryPackets = [];
        _filePosition = initialPosition;
        _baseStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite) { Position = 0 };

        MaxMemoryBuffer = maxMemoryBuffer;
    }

    public MemoryBufferedStream(Stream baseStream, long maxMemoryBuffer, long initialPosition = 0)
    {
        _writeSemaphore = new SemaphoreSlim(1, 1);
        _memoryPackets = [];
        _filePosition = initialPosition;
        _baseStream = baseStream;
        _baseStream.Position = 0;

        MaxMemoryBuffer = maxMemoryBuffer;
    }

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
            _writeSemaphore.Release();
        }
    }

    public long Seek(long offset, SeekOrigin origin)
    {
        // Check file position
        _filePosition = offset;
        return _baseStream.Seek(_filePosition, origin);
    }

    public void SetLength(long value)
    {
        // Check file position
        if (_filePosition > value)
            _filePosition = value;

        _baseStream.SetLength(value);
    }

    public async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        try
        {
            // Wait for write operation to finish
            await _writeSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            // Create packet
            var packet = new Packet(buffer, offset, count);
            // If there is no memory limit, write the packet directly to disk.
            if (MaxMemoryBuffer == 0)
            {
                // Write packet
                await WritePacketAsync(packet, cancellationToken).ConfigureAwait(false);
            }
            // Otherwise, we wait until the memory limit is reached and then write the values ​​to disk.
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
            _writeSemaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        try
        {
            await _writeSemaphore.WaitAsync().ConfigureAwait(false);

            await WriteToDiskAsync(CancellationToken.None).ConfigureAwait(false);
            await _baseStream.DisposeAsync().ConfigureAwait(false);
            _memoryPackets.Clear();
            _disposed = true;
        }
        finally
        {
            _writeSemaphore.Release();
            _writeSemaphore.Dispose();
        }
    }

    #region Helpers

    private async Task WriteToDiskAsync(CancellationToken cancellationToken = default)
    {
        // Check memory size
        if (MemoryLength == 0)
            return;

        // Seek to the current position of the file
        _baseStream.Seek(_filePosition, SeekOrigin.Begin);

        // Copy memory data to disk
        while (_memoryPackets.Count > 0)
        {
            // Get first item from queue
            var packet = _memoryPackets.Dequeue();
            // Write packet
            await WritePacketAsync(packet, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task WritePacketAsync(Packet packet, CancellationToken cancellationToken = default)
    {
        // Write data to disk
        await _baseStream.WriteAsync(packet.Data, cancellationToken).ConfigureAwait(false);
        // Update file position
        _filePosition += packet.Length;
        // Clear packet
        packet.Clear();
    }

    #endregion Helpers
}