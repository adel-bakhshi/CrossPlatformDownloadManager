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

    private readonly Chunk _chunk;
    private readonly DownloadConfiguration _configuration;
    private readonly SocketClient _client;
    private readonly ILogger? _logger;
    private readonly int _timeoutIncrement = 10;
    private ThrottledStream? _sourceStream;
    private MemoryBufferedStream? _storage;

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

    public ChunkDownloader(Chunk chunk, DownloadConfiguration config, SocketClient client, ILogger? logger = null)
    {
        _chunk = chunk;
        _configuration = config;
        _client = client;
        _logger = logger;
        _configuration.PropertyChanged += ConfigurationPropertyChanged;
    }

    private void ConfigurationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _logger?.LogDebug("Changed configuration {PropertyName} property", e.PropertyName);
        // Change maximum speed per second value
        if (e.PropertyName is nameof(_configuration.MaximumBytesPerSecond) or nameof(_configuration.ActiveChunks) && _sourceStream?.CanRead == true)
            _sourceStream.BandwidthLimit = _configuration.MaximumSpeedPerChunk;

        // Change maximum memory buffer bytes value
        if (e.PropertyName is nameof(_configuration.MaximumMemoryBufferBytes) or nameof(_configuration.ActiveChunks) && _storage != null)
            _storage.MaxMemoryBuffer = _configuration.MaximumMemoryBufferBytesPerChunk;
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
        finally
        {
            try
            {
                // Dispose storage when chunk download is completed
                if (_storage != null)
                    await _storage.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception error)
            {
                _logger?.LogError(error, "Error on dispose storage for chunk {ChunkId}.", _chunk.Id);
            }
        }
    }

    private async ValueTask<Chunk> ContinueWithDelayAsync(Request request, PauseToken pause, CancellationToken cancelToken)
    {
        if (cancelToken.IsCancellationRequested)
            return _chunk;

        _logger?.LogDebug("ContinueWithDelay of the chunk {ChunkId}", _chunk.Id);
        await _client.ThrowIfIsNotSupportDownloadInRange(request).ConfigureAwait(false);
        await Task.Delay(_chunk.Timeout, cancelToken).ConfigureAwait(false);
        // Increasing reading timeout to reduce stress and conflicts
        _chunk.Timeout += _timeoutIncrement;
        // re-request and continue downloading
        return await DownloadAsync(request, pause, cancelToken).ConfigureAwait(false);
    }

    private async ValueTask DownloadChunkAsync(Request request, PauseToken pauseToken, CancellationToken cancelToken)
    {
        if (cancelToken.IsCancellationRequested || _chunk.IsDownloadCompleted())
            return;

        _logger?.LogDebug("Memory buffered stream created for chunk {ChunkId}.", _chunk.Id);
        // Open or create temp stream for saving chunk data
        _storage = new MemoryBufferedStream(_chunk.TempFilePath, _configuration.MaximumMemoryBufferBytesPerChunk, _chunk.FilePosition);
        // Sync storage with chunk position
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
            await using CancellationTokenRegistration _ = cancelToken.Register(stream.Close);

            while (readSize > 0 && _chunk.CanWrite)
            {
                cancelToken.ThrowIfCancellationRequested();
                await pauseToken.WaitWhilePausedAsync().ConfigureAwait(false);
                var buffer = new byte[_configuration.BufferBlockSize];
                using var innerCts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
                innerToken = innerCts.Token;
                innerCts.CancelAfter(_chunk.Timeout);
                await using (innerToken.Value.Register(stream.Close))
                {
                    // if innerToken timeout occurs, close the stream just during the reading stream
                    readSize = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), innerToken.Value).ConfigureAwait(false);
                    _logger?.LogDebug("Read {ReadSize} bytes of the chunk {ChunkId} stream", readSize, _chunk.Id);
                }

                readSize = (int)Math.Min(_chunk.EmptyLength, readSize);
                if (readSize > 0)
                {
                    // ReSharper disable once PossiblyMistakenUseOfCancellationToken
                    await _storage!.WriteAsync(buffer, 0, readSize, cancelToken).ConfigureAwait(false);

                    _logger?.LogDebug("Write {ReadSize} bytes in the chunk {ChunkId}", readSize, _chunk.Id);
                    _chunk.Position += readSize;
                    _chunk.FilePosition = _storage.Position;
                    _logger?.LogDebug("The chunk {ChunkId} current position is: {ChunkPosition} of {ChunkLength}", _chunk.Id, _chunk.Position, _chunk.Length);

                    OnDownloadProgressChanged(new DownloadProgressChangedEventArgs(_chunk.Id)
                    {
                        TotalBytesToReceive = _chunk.Length,
                        ReceivedBytesSize = _chunk.Position,
                        ProgressedByteSize = readSize,
                        ReceivedBytes = _configuration.EnableLiveStreaming ? buffer.Take(readSize).ToArray() : []
                    });
                }
            }

            // Flush storage and update file position for last time
            if (_storage!.MemoryLength > 0)
                _chunk.FilePosition = await FlushStorageAsync(dispose: false).ConfigureAwait(false);

            // Compare file size with chunk length and throw exception if not match
            ThrowIfFileSizeNotMatchWithChunkLength();
        }
        catch (ObjectDisposedException exp) // When closing stream manually, ObjectDisposedException will be thrown
        {
            _logger?.LogError(exp, "ReadAsync of the chunk {ChunkId} stream was canceled or closed forcibly from server", _chunk.Id);
            cancelToken.ThrowIfCancellationRequested();
            if (innerToken?.IsCancellationRequested == true)
            {
                _logger?.LogError(exp, "ReadAsync of the chunk {ChunkId} stream has been timed out", _chunk.Id);
                throw new TaskCanceledException($"ReadAsync of the chunk {_chunk.Id} stream has been timed out", exp);
            }

            throw; // throw origin stack trace of exception
        }
        finally
        {
            // Flush storage and dispose it
            // Update file position
            _chunk.FilePosition = await FlushStorageAsync(dispose: true).ConfigureAwait(false);
        }

        _logger?.LogDebug("ReadStream of the chunk {ChunkId} completed successfully", _chunk.Id);
    }

    #region Helpers

    /// <summary>
    /// Syncs the position of the chunk with the position of the file.
    /// </summary>
    private void SyncPositionWithStorage()
    {
        if (_storage == null)
            return;

        // When the length of storage is greater than the chunk position and less than or equal to the chunk length
        // or when the length of storage is less than the chunk position
        // Change chunk position and file position based on the length of storage
        if ((_storage.Length > _chunk.Position && _storage.Length <= _chunk.Length) || (_storage.Length < _chunk.Position))
        {
            var length = _storage.Length >= _configuration.BufferBlockSize ? _storage.Length - _configuration.BufferBlockSize : 0;
            _storage.Seek(length, SeekOrigin.Begin);
            _chunk.Position = _chunk.FilePosition = length;
        }
        // When the length of storage is greater than the chunk position and greater than the chunk length
        // maybe file is corrupted and download must start from the beginning
        else if (_storage.Length > _chunk.Position && _storage.Length > _chunk.Length)
        {
            // Remove all data stored in file
            _storage.SetLength(0);
            _storage.Seek(0, SeekOrigin.Begin);
            // If Position is greater than 0 (i.e. the download has not yet started), restart the Chunk download.
            if (_chunk.Position > 0)
                OnDownloadRestarted(RestartReason.TempFileCorruption);

            // Clear chunk data for re-downloading
            _chunk.Clear();
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
    /// Flushes the temp storage and writes all data that stored in the memory to the storage.
    /// </summary>
    /// <param name="dispose">Whether the storage should be disposed or not.</param>
    /// <returns>Returns the position of the temp file after release memory and flush storage.</returns>
    private async Task<long> FlushStorageAsync(bool dispose)
    {
        if (_storage == null)
            return 0;

        long position;
        // Dispose storage
        if (dispose)
        {
            // Disposing storage, like flushing it, writes the remaining data in RAM to the hard drive.
            // Refer to MemoryBufferedStream class.
            await _storage.DisposeAsync().ConfigureAwait(false);
            // Wait for some times to ensure the data is written to the disk
            await Task.Delay(100).ConfigureAwait(false);
            // Get file position from storage
            position = _storage.Position;
            _storage = null;
        }
        // Flush storage
        else
        {
            // Write last bytes received from server to the disk.
            // Refer to MemoryBufferedStream class.
            await _storage.FlushAsync(CancellationToken.None).ConfigureAwait(false);
            // Wait for some times to ensure the data is written to the disk
            await Task.Delay(100).ConfigureAwait(false);
            // Get file position from storage
            position = _storage.Position;
        }

        // Log information
        _logger?.LogDebug("Chunk {ChunkId} flushed to disk", _chunk.Id);
        if (dispose)
            _logger?.LogDebug("Chunk {ChunkId} successfully disposed", _chunk.Id);

        return position;
    }

    /// <summary>
    /// Compare file size with chunk length and make sure file size is equal to chunk length
    /// </summary>
    /// <exception cref="InvalidOperationException">If file size not match with chunk length</exception>
    private void ThrowIfFileSizeNotMatchWithChunkLength()
    {
        // If the chunk has a length, the file size should be compared to the chunk length
        // Otherwise, the file size should be compared to the chunk position
        var chunkLength = _chunk.Length > 0 ? _chunk.Length : _chunk.Position;
        if (_storage?.Length != chunkLength)
            throw DownloadException.CreateDownloadException(DownloadException.FileSizeNotMatchWithChunkLength);
    }

    #endregion Helpers
}