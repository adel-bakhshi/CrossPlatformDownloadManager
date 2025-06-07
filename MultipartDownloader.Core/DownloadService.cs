using Microsoft.Extensions.Logging;
using MultipartDownloader.Core.CustomEventArgs;
using MultipartDownloader.Core.CustomExceptions;
using MultipartDownloader.Core.Extensions.Helpers;
using System.ComponentModel;

namespace MultipartDownloader.Core;

/// <summary>
/// Concrete implementation of the <see cref="AbstractDownloadService"/> class.
/// </summary>
public class DownloadService : AbstractDownloadService
{
    #region Private fields

    /// <summary>
    /// Shows the current position of the merge operation.
    /// </summary>
    private long _mergePosition;

    #endregion Private fields

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadService"/> class with the specified options.
    /// </summary>
    /// <param name="options">The configuration options for the download service.</param>
    /// <param name="loggerFactory">Pass standard logger factory</param>
    public DownloadService(DownloadConfiguration? options, ILoggerFactory? loggerFactory = null) : base(options)
    {
        Logger = loggerFactory?.CreateLogger<DownloadService>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadService"/> class with default options.
    /// </summary>
    public DownloadService(ILoggerFactory? loggerFactory = null) : this(null, loggerFactory)
    {
    }

    /// <summary>
    /// Starts the download operation.
    /// </summary>
    /// <returns>A task that represents the asynchronous download operation. The task result contains the downloaded stream.</returns>
    protected override async Task<Stream?> StartDownloadAsync(bool forceBuildStorage = true)
    {
        try
        {
            Logger?.LogInformation("Starting download process with forceBuildStorage={ForceBuildStorage}", forceBuildStorage);
            await SingleInstanceSemaphore.WaitAsync().ConfigureAwait(false);

            var firstRequest = RequestInstances[0];
            Logger?.LogDebug("Getting file size from first request");
            Package.TotalFileSize = await Client.GetFileSizeAsync(firstRequest).ConfigureAwait(false);
            Package.IsSupportDownloadInRange = await Client.IsSupportDownloadInRange(firstRequest).ConfigureAwait(false);
            Logger?.LogInformation("File size: {TotalFileSize}, Supports range download: {IsSupportDownloadInRange}", Package.TotalFileSize, Package.IsSupportDownloadInRange);

            // Check if we need to rebuild storage
            bool needToBuildStorage = forceBuildStorage || !Package.IsStorageExists() || Package.Chunks.Any(c => c.Position > 0);
            if (needToBuildStorage)
            {
                Logger?.LogDebug("Building storage with ReserveStorageSpace={ReserveStorage}, MaxMemoryBuffer={MaxMemoryBuffer}",
                    Options.ReserveStorageSpaceBeforeStartingDownload, Options.MaximumMemoryBufferBytes);

                Package.BuildStorage(Options.ReserveStorageSpaceBeforeStartingDownload);
            }

            ValidateBeforeChunking();
            ChunkHub.SetFileChunks(Package);

            Logger?.LogInformation("Starting download of {FileName} with size {TotalFileSize}", Package.FileName, Package.TotalFileSize);
            OnDownloadStarted(new DownloadStartedEventArgs(Package.FileName, Package.TotalFileSize));

            if (Options.ParallelDownload)
            {
                Logger?.LogDebug("Starting parallel download with {ChunkCount} chunks", Package.Chunks?.Length);
                await ParallelDownloadAsync(PauseTokenSource.Token).ConfigureAwait(false);
            }
            else
            {
                Logger?.LogDebug("Starting serial download with {ChunkCount} chunks", Package.Chunks?.Length);
                await SerialDownloadAsync(PauseTokenSource.Token).ConfigureAwait(false);
            }

            // Check if download is completed
            if (Status == DownloadStatus.Running)
            {
                Logger?.LogInformation("Download completed successfully");
                // Merge chunks
                await MergeChunksAsync().ConfigureAwait(false);
                // Send the completion signal
                SendDownloadCompletionSignal(DownloadStatus.Completed);
            }
        }
        catch (OperationCanceledException exp)
        {
            Logger?.LogWarning(exp, "Download was cancelled");
            SendDownloadCompletionSignal(DownloadStatus.Stopped, exp);
        }
        catch (Exception exp)
        {
            Logger?.LogError(exp, "Download failed with error: {ErrorMessage}", exp.Message);
            SendDownloadCompletionSignal(DownloadStatus.Failed, exp);
        }
        finally
        {
            SingleInstanceSemaphore.Release();
            Logger?.LogDebug("Download process completed, semaphore released");
        }

        return Package.GetStorageStream();
    }

    /// <summary>
    /// Merges chunks into a single file.
    /// </summary>
    /// <returns>A task that represents the asynchronous download operation.</returns>
    /// <exception cref="InvalidOperationException">Throw an exception if the final file size does not match the expected size.</exception>
    private async Task MergeChunksAsync()
    {
        if (Package.Chunks.Length == 0)
            return;

        // Reset merge position
        _mergePosition = 0;
        // Raise merge started event
        OnMergeStarted();

        // Open or create final file
        await using var finalStream = new FileStream(Package.FileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
        // Clear final stream
        finalStream.SetLength(0);
        // Seek to the beginning
        finalStream.Seek(0, SeekOrigin.Begin);
        // Order parts by Start value
        var sortedChunks = Package.Chunks.OrderBy(chunk => chunk.Start).ToList();
        // Merge parts
        foreach (var chunk in sortedChunks)
            await MergeFileWithProgressAsync(finalStream, chunk).ConfigureAwait(false);

        // Check if operation is canceled
        if (GlobalCancellationTokenSource.IsCancellationRequested)
            return;

        // Check final size
        if (finalStream.Length != Package.TotalFileSize)
            throw new InvalidOperationException($"Final file size mismatch! Expected: {Package.TotalFileSize} bytes, Actual: {finalStream.Length} bytes");

        // Make sure merge progress changed raised
        SendMergeProgressChangedSignal(finalStream.Length, Package.TotalFileSize);

        // Remove temporary files
        for (var i = 0; i < sortedChunks.Count; i++)
        {
            var tempFilePath = sortedChunks[i].TempFilePath;
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);

            // Remove temp directory after delete last download part
            if (i == sortedChunks.Count - 1)
            {
                var directory = Path.GetDirectoryName(tempFilePath);
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }
    }

    /// <summary>
    /// Merges the temp file that belongs to a chunk with the final file and change the progress of merge operation.
    /// </summary>
    /// <param name="finalStrem">The final file that all chunks merge to it.</param>
    /// <param name="chunk">The chunk that should be merge with the final file.</param>
    /// <returns>A task that represents the asynchronous download operation.</returns>
    private async Task MergeFileWithProgressAsync(Stream finalStrem, Chunk chunk)
    {
        // Open temp stream
        await using var tempStream = new FileStream(chunk.TempFilePath, FileMode.Open, FileAccess.Read);
        await using var throttledStream = new ThrottledStream(tempStream, Options.MaximumBytesPerSecondForMerge);
        var buffer = new byte[8192]; // 8 kilobyte buffer
        var bytesRead = 0;

        // Read bytes from temp stream
        while ((bytesRead = await throttledStream.ReadAsync(buffer).ConfigureAwait(false)) > 0)
        {
            // Check if operation is canceled
            if (GlobalCancellationTokenSource.IsCancellationRequested)
                break;

            // Write bytes to final stream
            await finalStrem.WriteAsync(buffer.AsMemory(0, bytesRead)).ConfigureAwait(false);
            // Update position
            _mergePosition += bytesRead;

            // Raise MergeProgressChanged event
            SendMergeProgressChangedSignal(_mergePosition, Package.TotalFileSize);
        }
    }

    /// <summary>
    /// Sends the merge progress changed signal with the given parameters.
    /// </summary>
    /// <param name="copiedBytesSize">The amount of file size that merged.</param>
    /// <param name="totalBytesSize">The total file size that should be merge.</param>
    private void SendMergeProgressChangedSignal(double copiedBytesSize, double totalBytesSize)
    {
        // Calculate progress percentage
        var progress = copiedBytesSize / totalBytesSize * 100;
        var eventArgs = new MergeProgressChangedEventArgs(progress);
        // Raise event
        OnMergeProgressChanged(eventArgs);
    }

    /// <summary>
    /// Sends the download completion signal with the specified <paramref name="state"/> and optional <paramref name="error"/>.
    /// </summary>
    /// <param name="state">The state of the download operation.</param>
    /// <param name="error">The exception that caused the download to fail, if any.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private void SendDownloadCompletionSignal(DownloadStatus state, Exception? error = null)
    {
        // Another event throws before this
        if (Status == DownloadStatus.Failed)
            return;

        Status = state;
        var isCancelled = Status == DownloadStatus.Stopped;
        Package.IsSaveComplete = Status == DownloadStatus.Completed && error == null;
        Package.IsSaving = false; // Reset IsSaving flag regardless of status
        OnDownloadFileCompleted(new AsyncCompletedEventArgs(error, isCancelled, Package));
    }

    /// <summary>
    /// Validates the download configuration before chunking the file.
    /// </summary>
    private void ValidateBeforeChunking()
    {
        CheckSingleChunkDownload();
        CheckSupportDownloadInRange();
        CheckSizes();
        CheckOutputDirectory();
    }

    /// <summary>
    /// Checks if the output directory exists and creates it if it does not.
    /// </summary>
    private void CheckOutputDirectory()
    {
        Options.ChunkFilesOutputDirectory = Options.ChunkFilesOutputDirectory?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(Options.ChunkFilesOutputDirectory?.Trim()) && !string.IsNullOrEmpty(Package.FileName?.Trim()))
        {
            var directory = Path.GetDirectoryName(Package.FileName);
            if (!string.IsNullOrEmpty(directory))
                Options.ChunkFilesOutputDirectory = directory;
        }

        if (string.IsNullOrEmpty(Options.ChunkFilesOutputDirectory))
            throw DownloadException.CreateDownloadException(DownloadException.ChunkFilesDirectoryIsNotValid);

        var fileName = Path.GetFileNameWithoutExtension(Package.FileName) ?? Guid.NewGuid().ToString();
        var temporaryPath = Path.Combine(Options.ChunkFilesOutputDirectory, fileName);
        if (Directory.Exists(Package.TemporarySavePath) && Directory.GetFiles(Package.TemporarySavePath, $"{fileName}.*.tmp").Length > 0)
            return;

        Package.TemporarySavePath = temporaryPath;
        Package.CreateTemporarySavePath();
    }

    /// <summary>
    /// Checks if there is enough disk space before starting the download.
    /// </summary>
    private void CheckSizes()
    {
        if (Options.CheckDiskSizeBeforeDownload)
            FileHelper.ThrowIfNotEnoughSpace(Package.TotalFileSize, Package.FileName);
    }

    /// <summary>
    /// Checks if the download should be handled as a single chunk.
    /// </summary>
    private void CheckSingleChunkDownload()
    {
        if (Package.TotalFileSize <= 1)
            Package.TotalFileSize = 0;

        if (Package.TotalFileSize <= Options.MinimumSizeOfChunking)
            SetSingleChunkDownload();
    }

    /// <summary>
    /// Checks if the server supports download in a specific range.
    /// </summary>
    private void CheckSupportDownloadInRange()
    {
        if (!Package.IsSupportDownloadInRange)
            SetSingleChunkDownload();
    }

    /// <summary>
    /// Sets the download configuration to handle the file as a single chunk.
    /// </summary>
    private void SetSingleChunkDownload()
    {
        Options.ChunkCount = 1;
        Options.ParallelCount = 1;
        ParallelSemaphore = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// Downloads the file in parallel chunks.
    /// </summary>
    /// <param name="pauseToken">The pause token for pausing the download.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task ParallelDownloadAsync(PauseToken pauseToken)
    {
        try
        {
            var chunkTasks = GetChunksTasks(pauseToken).ToList();
            int maxConcurrentTasks = Math.Min(Options.ParallelCount, chunkTasks.Count);

            Logger?.LogDebug("Starting parallel download with {MaxConcurrentTasks} concurrent tasks", maxConcurrentTasks);

            // Process tasks in batches to prevent overwhelming the system
            for (int i = 0; i < chunkTasks.Count; i += maxConcurrentTasks)
            {
                var batch = chunkTasks.Skip(i).Take(maxConcurrentTasks).ToList();
                await Task.WhenAll(batch).ConfigureAwait(false);

                // Check for cancellation after each batch
                if (GlobalCancellationTokenSource.Token.IsCancellationRequested)
                {
                    Logger?.LogInformation("Download cancelled during batch processing");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error during parallel download: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Downloads the file in serial chunks.
    /// </summary>
    /// <param name="pauseToken">The pause token for pausing the download.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task SerialDownloadAsync(PauseToken pauseToken)
    {
        try
        {
            var chunkTasks = GetChunksTasks(pauseToken).ToList();
            Logger?.LogDebug("Starting serial download with {ChunkCount} chunks", chunkTasks.Count);

            foreach (Task task in chunkTasks)
            {
                await task.ConfigureAwait(false);

                // Check for cancellation after each chunk
                if (GlobalCancellationTokenSource.Token.IsCancellationRequested)
                {
                    Logger?.LogInformation("Download cancelled during serial processing");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error during serial download: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Gets the tasks for downloading the chunks.
    /// </summary>
    /// <param name="pauseToken">The pause token for pausing the download.</param>
    /// <returns>An enumerable collection of tasks representing the chunk downloads.</returns>
    private IEnumerable<Task> GetChunksTasks(PauseToken pauseToken)
    {
        for (int i = 0; i < Package.Chunks.Length; i++)
        {
            var request = RequestInstances[i % RequestInstances.Count];
            yield return DownloadChunk(Package.Chunks[i], request, pauseToken, GlobalCancellationTokenSource);
        }
    }

    /// <summary>
    /// Downloads a specific chunk of the file.
    /// </summary>
    /// <param name="chunk">The chunk to download.</param>
    /// <param name="request">The request to use for the download.</param>
    /// <param name="pause">The pause token for pausing the download.</param>
    /// <param name="cancellationTokenSource">The cancellation token source for cancelling the download.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the downloaded chunk.</returns>
    private async Task<Chunk> DownloadChunk(Chunk chunk, Request request, PauseToken pause, CancellationTokenSource cancellationTokenSource)
    {
        ChunkDownloader chunkDownloader = GetChunkDownloader(chunk);
        chunkDownloader.DownloadProgressChanged += OnChunkDownloadProgressChanged;
        chunkDownloader.DownloadRestarted += OnChunkDownloadRestarted;
        await ParallelSemaphore.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
        try
        {
            cancellationTokenSource.Token.ThrowIfCancellationRequested();
            return await chunkDownloader.DownloadAsync(request, pause, cancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            SendDownloadCompletionSignal(DownloadStatus.Stopped);
            throw;
        }
        catch (Exception exp)
        {
            SendDownloadCompletionSignal(DownloadStatus.Failed, exp);
            cancellationTokenSource.Cancel(false);
            throw;
        }
        finally
        {
            ParallelSemaphore.Release();
        }
    }

    /// <summary>
    /// Gets the chunk downloader for the specified chunk.
    /// </summary>
    /// <param name="chunk">The chunk to get the downloader for.</param>
    /// <returns>The chunk downloader for the specified chunk.</returns>
    private ChunkDownloader GetChunkDownloader(Chunk chunk)
    {
        if (string.IsNullOrEmpty(chunk.TempFilePath))
        {
            var fileName = Path.GetFileNameWithoutExtension(Package.FileName) + $".part{int.Parse(chunk.Id) + 1}.tmp";
            chunk.TempFilePath = Path.Combine(Package.TemporarySavePath, fileName);
        }

        return new(chunk, Options, Client, Logger);
    }
}