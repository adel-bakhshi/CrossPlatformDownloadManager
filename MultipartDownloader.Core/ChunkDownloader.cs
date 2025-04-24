using Microsoft.Extensions.Logging;
using MultipartDownloader.Core.CustomEventArgs;
using MultipartDownloader.Core.CustomExceptions;
using MultipartDownloader.Core.Enums;
using MultipartDownloader.Core.Extensions.Helpers;
using System.ComponentModel;
using System.Net;

namespace MultipartDownloader.Core;

internal class ChunkDownloader
{
    private readonly ILogger? _logger;
    private readonly DownloadConfiguration _configuration;
    private readonly int _timeoutIncrement = 10;
    private ThrottledStream? _sourceStream;
    private MemoryBufferedStream? _storage;

    internal Chunk Chunk { get; set; }

    public event EventHandler<CustomEventArgs.DownloadProgressChangedEventArgs>? DownloadProgressChanged;

    public event EventHandler<ChunkDownloadRestartedEventArgs>? DownloadRestarted;

    public ChunkDownloader(Chunk chunk, DownloadConfiguration config, ILogger? logger = null)
    {
        Chunk = chunk;
        _configuration = config;
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

    public async Task<Chunk> Download(Request downloadRequest, PauseToken pause, CancellationToken cancelToken)
    {
        try
        {
            _logger?.LogDebug($"Starting download the chunk {Chunk.Id}");
            await DownloadChunk(downloadRequest, pause, cancelToken).ConfigureAwait(false);
            return Chunk;
        }
        catch (TaskCanceledException error) // when stream reader timeout occurred
        {
            _logger?.LogError(error, $"Task Canceled on download chunk {Chunk.Id} with retry");
            return await ContinueWithDelay(downloadRequest, pause, cancelToken).ConfigureAwait(false);
        }
        catch (ObjectDisposedException error) // when stream reader cancel/timeout occurred
        {
            _logger?.LogError(error, $"Disposed object error on download chunk {Chunk.Id} with retry");
            return await ContinueWithDelay(downloadRequest, pause, cancelToken).ConfigureAwait(false);
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
            return await ContinueWithDelay(downloadRequest, pause, cancelToken).ConfigureAwait(false);
        }
        catch (Exception error) when (Chunk.CanTryAgainOnFailover() && error.IsMomentumError())
        {
            _logger?.LogError(error, $"Error on download chunk {Chunk.Id} with retry");
            return await ContinueWithDelay(downloadRequest, pause, cancelToken).ConfigureAwait(false);
        }
        catch (Exception error)
        {
            // Can't handle this exception
            _logger?.LogCritical(error, $"Fatal error on download chunk {Chunk.Id}");
            throw;
        }
        finally
        {
            _logger?.LogDebug($"Exit from download method of the chunk {Chunk.Id}");
            await Task.Yield();
        }
    }

    private async Task<Chunk> ContinueWithDelay(Request request, PauseToken pause, CancellationToken cancelToken)
    {
        _logger?.LogDebug($"ContinueWithDelay of the chunk {Chunk.Id}");
        await request.ThrowIfIsNotSupportDownloadInRange().ConfigureAwait(false);
        await Task.Delay(Chunk.Timeout, cancelToken).ConfigureAwait(false);
        // Increasing reading timeout to reduce stress and conflicts
        Chunk.Timeout += _timeoutIncrement;
        // re-request and continue downloading
        return await Download(request, pause, cancelToken).ConfigureAwait(false);
    }

    private async Task DownloadChunk(Request downloadRequest, PauseToken pauseToken, CancellationToken cancelToken)
    {
        cancelToken.ThrowIfCancellationRequested();
        _logger?.LogDebug($"DownloadChunk of the chunk {Chunk.Id}");
        if (!Chunk.IsDownloadCompleted())
        {
            // Open or create temp stream for saving chunk data
            _storage = new MemoryBufferedStream(Chunk.TempFilePath, _configuration.MaximumMemoryBufferBytesPerChunk, Chunk.FilePosition);
            // Sync storage with chunk position
            SyncPositionWithStorage();

            HttpWebRequest request = downloadRequest.GetRequest();
            SetRequestRange(request);
            using var downloadResponse = await request.GetResponseAsync().ConfigureAwait(false) as HttpWebResponse;
            if (downloadResponse?.StatusCode == HttpStatusCode.OK ||
                downloadResponse?.StatusCode == HttpStatusCode.PartialContent ||
                downloadResponse?.StatusCode == HttpStatusCode.Created ||
                downloadResponse?.StatusCode == HttpStatusCode.Accepted ||
                downloadResponse?.StatusCode == HttpStatusCode.ResetContent)
            {
                _logger?.LogDebug($"DownloadChunk of the chunk {Chunk.Id} with response status: {downloadResponse.StatusCode}");
                _configuration.RequestConfiguration.CookieContainer = request.CookieContainer;
                await using Stream responseStream = downloadResponse.GetResponseStream();
                await using (_sourceStream = new ThrottledStream(responseStream, _configuration.MaximumSpeedPerChunk))
                {
                    await ReadStream(_sourceStream, pauseToken, cancelToken).ConfigureAwait(false);
                }
            }
            else
            {
                throw new WebException($"Download response status of the chunk {Chunk.Id} was " + $"{downloadResponse?.StatusCode}: " + downloadResponse?.StatusDescription);
            }
        }
    }

    private void SetRequestRange(HttpWebRequest request)
    {
        long startOffset = Chunk.Start + Chunk.Position;

        // has limited range
        if (Chunk.End > 0 && (_configuration.ChunkCount > 1 || Chunk.Position > 0 || _configuration.RangeDownload))
        {
            if (startOffset < Chunk.End)
                request.AddRange(startOffset, Chunk.End);
            else
                request.AddRange(startOffset);
        }
    }

    internal async Task ReadStream(Stream stream, PauseToken pauseToken, CancellationToken cancelToken)
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

    private void OnDownloadProgressChanged(CustomEventArgs.DownloadProgressChangedEventArgs e)
    {
        DownloadProgressChanged?.Invoke(this, e);
    }

    private void OnDownloadRestarted(RestartReason reason)
    {
        DownloadRestarted?.Invoke(this, new ChunkDownloadRestartedEventArgs(Chunk.Id, reason));
    }

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