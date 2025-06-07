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
    private readonly ILogger? _logger;
    private readonly DownloadConfiguration _configuration;
    private readonly int _timeoutIncrement = 10;
    private ThrottledStream? _sourceStream;
    private MemoryBufferedStream? _storage;
    private readonly SocketClient _client;

    internal Chunk Chunk { get; set; }

    public event EventHandler<CustomEventArgs.DownloadProgressChangedEventArgs>? DownloadProgressChanged;

    public event EventHandler<ChunkDownloadRestartedEventArgs>? DownloadRestarted;

    public ChunkDownloader(Chunk chunk, DownloadConfiguration config, SocketClient client, ILogger? logger = null)
    {
        Chunk = chunk;
        _configuration = config;
        _client = client;
        _logger = logger;
        _configuration.PropertyChanged += ConfigurationPropertyChanged;
    }

    private void ConfigurationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _logger?.LogDebug($"Changed configuration {e.PropertyName} property");
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
            _logger?.LogDebug($"Starting download the chunk {Chunk.Id}.");
            await DownloadChunkAsync(downloadRequest, pause, cancelToken).ConfigureAwait(false);
            return Chunk;
        }
        catch (TaskCanceledException error) when (!cancelToken.IsCancellationRequested)
        {
            // when stream reader timeout occurred
            _logger?.LogError(error, $"Task time-outed on download chunk {Chunk.Id}. Retry ...");
            return await ContinueWithDelayAsync(downloadRequest, pause, cancelToken).ConfigureAwait(false);
        }
        catch (ObjectDisposedException error) when (!cancelToken.IsCancellationRequested)
        {
            // when stream reader cancel/timeout occurred
            _logger?.LogError(error, $"Disposed object error on download chunk {Chunk.Id}. Retry ...");
            return await ContinueWithDelayAsync(downloadRequest, pause, cancelToken).ConfigureAwait(false);
        }
        catch (HttpRequestException error) when (!cancelToken.IsCancellationRequested && Chunk.CanTryAgainOnFailure())
        {
            _logger?.LogError(error, $"HTTP request error on download chunk {Chunk.Id}. Retry ...");
            return await ContinueWithDelayAsync(downloadRequest, pause, cancelToken).ConfigureAwait(false);
        }
        catch (DownloadException error) when (error.ErrorCode == DownloadException.FileSizeNotMatchWithChunkLength) // when file size in not match with chunk length
        {
            // Log error data
            _logger?.LogError(error, $"File size not match with chunk length for chunk {Chunk.Id} with retry");
            // Reset chunk positions
            Chunk.Position = Chunk.FilePosition = 0;
            // Check if chunk can restart without clear temp file
            if (!Chunk.CanRestartWithoutClearTempFile())
            {
                // Clear chunk temp file
                Chunk.ClearTempFile();
                // Raise DownloadRestarted event
                OnDownloadRestarted(RestartReason.FileSizeIsNotMatchWithChunkLength);
            }

            // Re-request and continue downloading
            return await ContinueWithDelayAsync(downloadRequest, pause, cancelToken).ConfigureAwait(false);
        }
        catch (Exception error) when (!cancelToken.IsCancellationRequested && error.IsMomentumError() && Chunk.CanTryAgainOnFailure())
        {
            _logger?.LogError(error, $"Error on download chunk {Chunk.Id}. Retry ...");
            return await ContinueWithDelayAsync(downloadRequest, pause, cancelToken).ConfigureAwait(false);
        }
        catch (Exception error)
        {
            cancelToken.ThrowIfCancellationRequested();
            // Can't handle this exception
            _logger?.LogCritical(error, $"Fatal error on download chunk {Chunk.Id}.");
            throw;
        }
    }

    private async ValueTask<Chunk> ContinueWithDelayAsync(Request request, PauseToken pause, CancellationToken cancelToken)
    {
        if (cancelToken.IsCancellationRequested)
            return Chunk;

        _logger?.LogDebug($"ContinueWithDelay of the chunk {Chunk.Id}");
        await _client.ThrowIfIsNotSupportDownloadInRange(request).ConfigureAwait(false);
        await Task.Delay(Chunk.Timeout, cancelToken).ConfigureAwait(false);
        // Increasing reading timeout to reduce stress and conflicts
        Chunk.Timeout += _timeoutIncrement;
        // re-request and continue downloading
        return await DownloadAsync(request, pause, cancelToken).ConfigureAwait(false);
    }

    private async ValueTask DownloadChunkAsync(Request request, PauseToken pauseToken, CancellationToken cancelToken)
    {
        if (cancelToken.IsCancellationRequested || Chunk.IsDownloadCompleted())
            return;

        _logger?.LogDebug($"Memory buffered stream created for chunk {Chunk.Id}.");
        // Dispose the storage if it is not null
        if (_storage != null)
        {
            await _storage.DisposeAsync();
            _storage = null;
        }

        // Open or create temp stream for saving chunk data
        _storage = new MemoryBufferedStream(Chunk.TempFilePath, _configuration.MaximumMemoryBufferBytesPerChunk, Chunk.FilePosition);
        // Sync storage with chunk position
        SyncPositionWithStorage();

        _logger?.LogDebug($"DownloadChunk of the chunk {Chunk.Id}.");
        var requestMsg = request.GetRequest();
        SetRequestRange(requestMsg);
        using var responseMsg = await _client.SendRequestAsync(requestMsg, cancelToken).ConfigureAwait(false);

        _logger?.LogDebug($"Downloading the chunk {Chunk.Id} " + $"with response status code: {responseMsg.StatusCode}");
        await using var responseStream = await responseMsg.Content.ReadAsStreamAsync(cancelToken).ConfigureAwait(false);

        _sourceStream = new ThrottledStream(responseStream, _configuration.MaximumSpeedPerChunk);
        await ReadStreamAsync(_sourceStream, pauseToken, cancelToken).ConfigureAwait(false);
        await _sourceStream.DisposeAsync();
    }

    private void SetRequestRange(HttpRequestMessage request)
    {
        long startOffset = Chunk.Start + Chunk.Position;

        // Set the range of the content
        if (Chunk.End > 0 && startOffset < Chunk.End && (_configuration.ChunkCount > 1 || Chunk.Position > 0))
            request.Headers.Range = new RangeHeaderValue(startOffset, Chunk.End);
    }

    internal async Task ReadStreamAsync(Stream stream, PauseToken pauseToken, CancellationToken cancelToken)
    {
        int readSize = 1;
        CancellationToken? innerToken = null;

        try
        {
            // close stream on cancellation because, it doesn't work on .Net Framework
            await using CancellationTokenRegistration _ = cancelToken.Register(stream.Close);

            while (readSize > 0 && Chunk.CanWrite)
            {
                cancelToken.ThrowIfCancellationRequested();
                await pauseToken.WaitWhilePausedAsync().ConfigureAwait(false);
                byte[] buffer = new byte[_configuration.BufferBlockSize];
                using var innerCts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
                innerToken = innerCts.Token;
                innerCts.CancelAfter(Chunk.Timeout);
                await using (innerToken.Value.Register(stream.Close))
                {
                    // if innerToken timeout occurs, close the stream just during the reading stream
                    readSize = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), innerToken.Value).ConfigureAwait(false);
                    _logger?.LogDebug($"Read {readSize}bytes of the chunk {Chunk.Id} stream");
                }

                readSize = (int)Math.Min(Chunk.EmptyLength, readSize);
                if (readSize > 0)
                {
                    await _storage!.WriteAsync(buffer, 0, readSize, cancelToken).ConfigureAwait(false);

                    _logger?.LogDebug($"Write {readSize}bytes in the chunk {Chunk.Id}");
                    Chunk.Position += readSize;
                    Chunk.FilePosition = _storage.Position;
                    _logger?.LogDebug($"The chunk {Chunk.Id} current position is: {Chunk.Position} of {Chunk.Length}");

                    OnDownloadProgressChanged(new CustomEventArgs.DownloadProgressChangedEventArgs(Chunk.Id)
                    {
                        TotalBytesToReceive = Chunk.Length,
                        ReceivedBytesSize = Chunk.Position,
                        ProgressedByteSize = readSize,
                        ReceivedBytes = _configuration.EnableLiveStreaming ? buffer.Take(readSize).ToArray() : []
                    });
                }
            }

            // Flush storage and update file position for last time
            if (_storage!.MemoryLength > 0)
                Chunk.FilePosition = await FlushStorageAsync(dispose: false).ConfigureAwait(false);

            // Compare file size with chunk length and throw exception if not match
            ThrowIfFileSizeNotMatchWithChunkLength();
        }
        catch (ObjectDisposedException exp) // When closing stream manually, ObjectDisposedException will be thrown
        {
            _logger?.LogError(exp, $"ReadAsync of the chunk {Chunk.Id} stream was canceled or closed forcibly from server");
            cancelToken.ThrowIfCancellationRequested();
            if (innerToken?.IsCancellationRequested == true)
            {
                _logger?.LogError(exp, $"ReadAsync of the chunk {Chunk.Id} stream has been timed out");
                throw new TaskCanceledException($"ReadAsync of the chunk {Chunk.Id} stream has been timed out", exp);
            }

            throw; // throw origin stack trace of exception
        }
        finally
        {
            // Flush storage and dispose it
            // Update file position
            Chunk.FilePosition = await FlushStorageAsync(dispose: true).ConfigureAwait(false);
        }

        _logger?.LogDebug($"ReadStream of the chunk {Chunk.Id} completed successfully");
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
        if ((_storage.Length > Chunk.Position && _storage.Length <= Chunk.Length) || (_storage.Length < Chunk.Position))
        {
            var length = _storage.Length >= _configuration.BufferBlockSize ? _storage.Length - _configuration.BufferBlockSize : 0;
            _storage.Seek(length, SeekOrigin.Begin);
            Chunk.Position = Chunk.FilePosition = length;
        }
        // When the length of storage is greater than the chunk position and greater than the chunk length
        // maybe file is corrupted and download must start from the beginning
        else if (_storage.Length > Chunk.Position && _storage.Length > Chunk.Length)
        {
            // Remove all data stored in file
            _storage.SetLength(0);
            _storage.Seek(0, SeekOrigin.Begin);
            // If Position is greater than 0 (i.e. the download has not yet started), restart the Chunk download.
            if (Chunk.Position > 0)
                OnDownloadRestarted(RestartReason.TempFileCorruption);

            // Clear chunk data for re-downloading
            Chunk.Clear();
        }
    }

    /// <summary>
    /// Raises the <see cref="DownloadProgressChanged"/> event.
    /// </summary>
    /// <param name="e">The arguments of the event.</param>
    private void OnDownloadProgressChanged(CustomEventArgs.DownloadProgressChangedEventArgs e)
    {
        DownloadProgressChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the <see cref="DownloadRestarted"/> event.
    /// </summary>
    /// <param name="reason">The reason that cause restart the download of the chunk.</param>
    private void OnDownloadRestarted(RestartReason reason)
    {
        DownloadRestarted?.Invoke(this, new ChunkDownloadRestartedEventArgs(Chunk.Id, reason));
    }

    /// <summary>
    /// Flushes the temp storage and writes all data that stored in the memory to the storage.
    /// </summary>
    /// <param name="dispose">Whether the storage should be dispose or not.</param>
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
            // Get file position from storage
            position = _storage.Position;
        }

        // Log information
        _logger?.LogDebug($"Chunk {Chunk.Id} flushed to disk");
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
        var chunkLength = Chunk.Length > 0 ? Chunk.Length : Chunk.Position;
        if (_storage?.Length != chunkLength)
            throw DownloadException.CreateDownloadException(DownloadException.FileSizeNotMatchWithChunkLength);
    }

    #endregion Helpers
}