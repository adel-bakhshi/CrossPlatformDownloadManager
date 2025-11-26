using Microsoft.Extensions.Logging;
using MultipartDownloader.Core.CustomEventArgs;
using MultipartDownloader.Core.CustomExceptions;
using MultipartDownloader.Core.Enums;
using MultipartDownloader.Core.Extensions.Helpers;
using System.ComponentModel;
using System.Net.Http.Headers;

namespace MultipartDownloader.Core;

internal class ChunkDownloader
{
    #region Private fields

    private const int TimeoutIncrement = 10;

    private readonly Chunk _chunk;
    private readonly DownloadConfiguration _configuration;
    private readonly SocketClient _client;
    private readonly SharedMemoryBufferedStream _storage;
    private readonly ILogger? _logger;
    private ThrottledStream? _sourceStream;

    #endregion Private fields

    #region Events

    /// <summary>
    /// The event that is raised when the download progress of the chunk has changed.
    /// </summary>
    public event EventHandler<DownloadProgressChangedEventArgs>? DownloadProgressChanged;

    /// <summary>
    /// The event that is raised when the download of the chunk has been restarted.
    /// </summary>
    public event EventHandler<ChunkDownloadRestartedEventArgs>? DownloadRestarted;

    #endregion Events

    public ChunkDownloader(Chunk chunk, DownloadConfiguration config, SocketClient client, SharedMemoryBufferedStream storage, ILogger? logger = null)
    {
        _chunk = chunk;
        _configuration = config;
        _client = client;
        _storage = storage;
        _logger = logger;
        _configuration.PropertyChanged += ConfigurationPropertyChanged;
    }

    private void ConfigurationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _logger?.LogDebug("Changed configuration {PropertyName} property", e.PropertyName);
        // Change maximum speed per second value
        if (e.PropertyName is nameof(_configuration.MaximumBytesPerSecond) or nameof(_configuration.ActiveChunks) && _sourceStream?.CanRead == true)
            _sourceStream.BandwidthLimit = _configuration.MaximumSpeedPerChunk;
    }

    public async ValueTask<Chunk> DownloadAsync(Request downloadRequest, PauseToken pause, CancellationToken cancelToken)
    {
        try
        {
            _logger?.LogDebug("Starting download the chunk {ChunkId}.", _chunk.Id);
            await DownloadChunkAsync(downloadRequest, pause, cancelToken).ConfigureAwait(false);
            return _chunk;
        }
        catch (TaskCanceledException error) when (!cancelToken.IsCancellationRequested)
        {
            // when stream reader timeout occurred
            _logger?.LogError(error, "Task time-outed on download chunk {ChunkId}. Retry ...", _chunk.Id);
            return await ContinueWithDelayAsync(downloadRequest, pause, cancelToken).ConfigureAwait(false);
        }
        catch (ObjectDisposedException error) when (!cancelToken.IsCancellationRequested)
        {
            // when stream reader cancel/timeout occurred
            _logger?.LogError(error, "Disposed object error on download chunk {ChunkId}. Retry ...", _chunk.Id);
            return await ContinueWithDelayAsync(downloadRequest, pause, cancelToken).ConfigureAwait(false);
        }
        catch (HttpRequestException error) when (!cancelToken.IsCancellationRequested && _chunk.CanTryAgainOnFailure())
        {
            _logger?.LogError(error, "HTTP request error on download chunk {ChunkId}. Retry ...", _chunk.Id);
            return await ContinueWithDelayAsync(downloadRequest, pause, cancelToken).ConfigureAwait(false);
        }
        catch (DownloadException error) when (error.ErrorCode == DownloadException.FileSizeNotMatchWithChunkLength) // when file size in not match with chunk length
        {
            // Log error data
            _logger?.LogError(error, "File size not match with chunk length for chunk {ChunkId} with retry", _chunk.Id);
            // Reset chunk positions
            _chunk.Position = _chunk.FilePosition = 0;
            // Check if chunk can restart without clear temp file
            if (!_chunk.CanRestartWithoutClearTempFile())
            {
                // Clear chunk temp file
                _chunk.ClearTempFile();
                // Raise DownloadRestarted event
                OnDownloadRestarted(RestartReason.FileSizeIsNotMatchWithChunkLength);
            }

            // Re-request and continue downloading
            return await ContinueWithDelayAsync(downloadRequest, pause, cancelToken).ConfigureAwait(false);
        }
        catch (Exception error) when (!cancelToken.IsCancellationRequested && error.IsMomentumError() && _chunk.CanTryAgainOnFailure())
        {
            _logger?.LogError(error, "Error on download chunk {ChunkId}. Retry ...", _chunk.Id);
            return await ContinueWithDelayAsync(downloadRequest, pause, cancelToken).ConfigureAwait(false);
        }
        catch (Exception error)
        {
            cancelToken.ThrowIfCancellationRequested();
            // Can't handle this exception
            _logger?.LogCritical(error, "Fatal error on download chunk {ChunkId}.", _chunk.Id);
            throw;
        }
    }

    private async ValueTask<Chunk> ContinueWithDelayAsync(Request request, PauseToken pause, CancellationToken cancelToken)
    {
        if (cancelToken.IsCancellationRequested)
            return _chunk;

        _logger?.LogDebug("ContinueWithDelay of the chunk {ChunkId}", _chunk.Id);
        await _client.ThrowIfIsNotSupportDownloadInRange(request).ConfigureAwait(false);
        await Task.Delay(_chunk.RetryDelay, cancelToken).ConfigureAwait(false);
        // Increasing reading timeout to reduce stress and conflicts
        _chunk.ReadTimeout += 5000;
        // Increasing delay between each retry
        _chunk.RetryDelay = Math.Min(_chunk.RetryDelay + TimeoutIncrement, 2000);
        // re-request and continue downloading
        return await DownloadAsync(request, pause, cancelToken).ConfigureAwait(false);
    }

    private async ValueTask DownloadChunkAsync(Request request, PauseToken pauseToken, CancellationToken cancelToken)
    {
        if (cancelToken.IsCancellationRequested || _chunk.IsDownloadCompleted())
            return;

        // Create buffer for chunk
        _storage.CreateBuffer(_chunk.Id, _chunk.TempFilePath, _chunk.FilePosition, SeekOrigin.Begin, cancelToken);
        _logger?.LogDebug("Memory buffered stream created for chunk {ChunkId}.", _chunk.Id);

        // Sync storage with chunk position
        _logger?.LogDebug("Syncing chunk {ChunkId} position with storage...", _chunk.Id);
        SyncPositionWithStorage();

        _logger?.LogDebug("DownloadChunk of the chunk {ChunkId}.", _chunk.Id);
        var requestMsg = request.GetRequest();
        SetRequestRange(requestMsg);
        using var responseMsg = await _client.SendRequestAsync(requestMsg, cancelToken).ConfigureAwait(false);

        _logger?.LogDebug("Downloading the chunk {ChunkId} with response status code: {StatusCode}", _chunk.Id, responseMsg.StatusCode);
        await using var responseStream = await responseMsg.Content.ReadAsStreamAsync(cancelToken).ConfigureAwait(false);

        _sourceStream = new ThrottledStream(responseStream, _configuration.MaximumSpeedPerChunk);
        await ReadStreamAsync(_sourceStream, pauseToken, cancelToken).ConfigureAwait(false);
        await _sourceStream.DisposeAsync();
    }

    private void SetRequestRange(HttpRequestMessage request)
    {
        var startOffset = _chunk.Start + _chunk.Position;

        // Set the range of the content
        if (_chunk.End > 0 && startOffset < _chunk.End && (_configuration.ChunkCount > 1 || _chunk.Position > 0))
            request.Headers.Range = new RangeHeaderValue(startOffset, _chunk.End);
    }

    private async Task ReadStreamAsync(Stream stream, PauseToken pauseToken, CancellationToken cancelToken)
    {
        var readSize = 1;
        CancellationToken? innerToken = null;

        try
        {
            // close stream on cancellation because, it doesn't work on .Net Framework
            await using var _ = cancelToken.Register(stream.Close);

            while (readSize > 0 && _chunk.CanWrite)
            {
                cancelToken.ThrowIfCancellationRequested();
                await pauseToken.WaitWhilePausedAsync().ConfigureAwait(false);
                var buffer = new byte[_configuration.BufferBlockSize];
                using var innerCts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
                innerToken = innerCts.Token;

                // Calculate dynamic timeout based on current conditions
                var dynamicTimeout = CalculateDynamicTimeout();
                innerCts.CancelAfter(dynamicTimeout);

                _logger?.LogDebug("Chunk {ChunkId} read timeout set to {Timeout}ms", _chunk.Id, dynamicTimeout);

                await using (innerToken.Value.Register(stream.Close))
                {
                    // if innerToken timeout occurs, close the stream just during the reading stream
                    readSize = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), innerToken.Value).ConfigureAwait(false);
                    _logger?.LogDebug("Read {ReadSize} bytes of the chunk {ChunkId} stream", readSize, _chunk.Id);
                }

                readSize = (int)Math.Min(_chunk.EmptyLength, readSize);
                if (readSize <= 0)
                    continue;

                // ReSharper disable once PossiblyMistakenUseOfCancellationToken
                await _storage.WriteAsync(_chunk.Id, buffer, 0, readSize, cancelToken).ConfigureAwait(false);

                _logger?.LogDebug("Write {ReadSize} bytes in the chunk {ChunkId}", readSize, _chunk.Id);
                _chunk.Position += readSize;
                _chunk.FilePosition = _storage.GetChunkFilePosition(_chunk.Id);
                _logger?.LogDebug("The chunk {ChunkId} current position is: {ChunkPosition} of {ChunkLength}", _chunk.Id, _chunk.Position, _chunk.Length);

                OnDownloadProgressChanged(new DownloadProgressChangedEventArgs(_chunk.Id)
                {
                    TotalBytesToReceive = _chunk.Length,
                    ReceivedBytesSize = _chunk.Position,
                    ProgressedByteSize = readSize,
                    ReceivedBytes = _configuration.EnableLiveStreaming ? buffer.Take(readSize).ToArray() : []
                });
            }

            // Flush chunk to storage
            await FlushChunkAsync(cancelToken).ConfigureAwait(false);

            // Compare file size with chunk length and throw exception if not match
            ThrowIfFileSizeNotMatchWithChunkLength();
        }
        catch (ObjectDisposedException exp) // When closing stream manually, ObjectDisposedException will be thrown
        {
            _logger?.LogError(exp, "ReadAsync of the chunk {ChunkId} stream was canceled or closed forcibly from server", _chunk.Id);
            cancelToken.ThrowIfCancellationRequested();
            // throw origin stack trace of exception
            if (innerToken?.IsCancellationRequested != true)
                throw;

            _logger?.LogError(exp, "ReadAsync of the chunk {ChunkId} stream has been timed out", _chunk.Id);
            throw new TaskCanceledException($"ReadAsync of the chunk {_chunk.Id} stream has been timed out", exp);
        }
        finally
        {
            // Flush chunk to storage if there is any data in the memory
            if (_storage.GetChunkMemoryLength(_chunk.Id) > 0)
                await FlushChunkAsync(CancellationToken.None).ConfigureAwait(false);
        }

        _logger?.LogDebug("ReadStream of the chunk {ChunkId} completed successfully", _chunk.Id);
    }

    /// <summary>
    /// Syncs the position of the chunk with the position of the file.
    /// </summary>
    private void SyncPositionWithStorage()
    {
        _logger?.LogDebug("Sync position of the chunk {ChunkId} with the position of the file", _chunk.Id);

        // Get chunk file length
        var storageLength = _storage.GetChunkFileLength(_chunk.Id);

        // When the length of storage is greater than the chunk position and less than or equal to the chunk length
        // or when the length of storage is less than the chunk position
        // Change chunk position and file position based on the length of storage
        if ((storageLength > _chunk.Position && storageLength <= _chunk.Length) || storageLength < _chunk.Position)
        {
            var length = storageLength >= _configuration.BufferBlockSize ? storageLength - _configuration.BufferBlockSize : 0;
            _logger?.LogDebug("The length of the chunk file {ChunkId} is {StorageLength} and the chunk position is {ChunkPosition}", _chunk.Id, storageLength, _chunk.Position);

            _storage.SetChunkFilePosition(_chunk.Id, length, SeekOrigin.Begin);
            _chunk.Position = _chunk.FilePosition = length;

            _logger?.LogDebug("The chunk {ChunkId} current position is: {ChunkPosition} of {ChunkLength}", _chunk.Id, _chunk.Position, _chunk.Length);
        }
        // When the length of storage is greater than the chunk position and greater than the chunk length
        // maybe file is corrupted and download must start from the beginning
        else if (storageLength > _chunk.Position && storageLength > _chunk.Length)
        {
            _logger?.LogWarning("The length of the chunk file {ChunkId} is {StorageLength} and the chunk position is {ChunkPosition}", _chunk.Id, storageLength, _chunk.Position);
            _logger?.LogWarning("The chunk {ChunkId} file is corrupted, download must start from the beginning", _chunk.Id);

            // Remove all data stored in file
            _storage.SetChunkFileLength(_chunk.Id, 0);
            _storage.SetChunkFilePosition(_chunk.Id, 0, SeekOrigin.Begin);
            // If Position is greater than 0 (i.e. the download has not yet started), restart the Chunk download.
            if (_chunk.Position > 0)
                OnDownloadRestarted(RestartReason.TempFileCorruption);

            // Clear chunk data for re-downloading
            _chunk.Clear();

            _logger?.LogDebug("The chunk {ChunkId} cleared and restarted.", _chunk.Id);
        }
    }

    /// <summary>
    /// Raises the <see cref="DownloadProgressChanged"/> event.
    /// </summary>
    /// <param name="e">The arguments of the event.</param>
    private void OnDownloadProgressChanged(DownloadProgressChangedEventArgs e)
    {
        DownloadProgressChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the <see cref="DownloadRestarted"/> event.
    /// </summary>
    /// <param name="reason">The reason that cause restart the download of the chunk.</param>
    private void OnDownloadRestarted(RestartReason reason)
    {
        DownloadRestarted?.Invoke(this, new ChunkDownloadRestartedEventArgs(_chunk.Id, reason));
    }

    /// <summary>
    /// Flushes the storage and updates the file position.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the asynchronous operation.</param>
    private async Task FlushChunkAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        _logger?.LogDebug("Flush chunk {ChunkId} storage", _chunk.Id);

        await _storage.FlushChunkAsync(_chunk.Id, cancellationToken).ConfigureAwait(false);
        // Add a small delay to ensure file is fully written
        await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        _chunk.FilePosition = _storage.GetChunkFilePosition(_chunk.Id);

        _logger?.LogDebug("Chunk {ChunkId} storage flushed and file position updated to {FilePosition}", _chunk.Id, _chunk.FilePosition);
    }

    /// <summary>
    /// Compare file size with chunk length and make sure file size is equal to chunk length
    /// </summary>
    /// <exception cref="InvalidOperationException">If file size not match with chunk length</exception>
    private void ThrowIfFileSizeNotMatchWithChunkLength()
    {
        _logger?.LogDebug("Compare file size with chunk length for chunk {ChunkId}", _chunk.Id);

        // If the chunk has a length, the file size should be compared to the chunk length
        // Otherwise, the file size should be compared to the chunk position
        var chunkLength = _chunk.Length > 0 ? _chunk.Length : _chunk.Position;
        if (_storage.GetChunkFileLength(_chunk.Id) != chunkLength)
            throw DownloadException.CreateDownloadException(DownloadException.FileSizeNotMatchWithChunkLength);
    }

    /// <summary>
    /// Calculates a dynamic timeout value based on current download speed and buffer size.
    /// This helps prevent unnecessary timeouts on slow connections while quickly detecting frozen servers.
    /// </summary>
    /// <returns>The calculated timeout in milliseconds, bounded between min and max values.</returns>
    private int CalculateDynamicTimeout()
    {
        // Minimum timeout: 5 seconds
        // This ensures we don't timeout too quickly even on fast connections
        const int minTimeout = 5000;

        // Maximum timeout: 30 seconds
        // This prevents waiting too long for frozen/unresponsive servers
        const int maxTimeout = 30000;

        // Default timeout: 10 seconds
        // Used when we can't calculate based on speed (e.g., unlimited bandwidth)
        const int defaultTimeout = 10000;

        // Check if we have bandwidth limiting enabled and can calculate based on speed
        if (_sourceStream?.BandwidthLimit > 0 && _sourceStream.BandwidthLimit != long.MaxValue)
        {
            // Calculate expected time to read one buffer block at current speed
            // Formula: (BufferSize / BytesPerSecond) * 1000 to convert to milliseconds
            var expectedTimeMs = (_configuration.BufferBlockSize * 1000.0) / _sourceStream.BandwidthLimit;

            // Add 100% safety margin to account for:
            // - Network latency fluctuations
            // - TCP overhead
            // - Temporary slowdowns
            // - Operating system scheduling delays
            var timeoutWithMargin = expectedTimeMs * 2.0;

            // Round up to nearest integer
            var calculatedTimeout = (int)Math.Ceiling(timeoutWithMargin);

            // Ensure the timeout is within acceptable bounds
            return Math.Max(minTimeout, Math.Min(calculatedTimeout, maxTimeout));
        }

        // If bandwidth limiting is disabled or set to infinite,
        // check if we have a configured ReadTimeout
        if (_chunk.ReadTimeout > 0)
        {
            // Use the configured timeout, but ensure it's within bounds
            return Math.Max(minTimeout, Math.Min(_chunk.ReadTimeout, maxTimeout));
        }

        // Fallback to default timeout if no other value is available
        return defaultTimeout;
    }
}