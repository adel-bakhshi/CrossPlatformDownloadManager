using Microsoft.Extensions.Logging;
using MultipartDownloader.Core.CustomEventArgs;
using System.ComponentModel;

namespace MultipartDownloader.Core;

/// <summary>
/// Abstract base class for download services implementing <see cref="IDownloadService"/> and <see cref="IDisposable"/>.
/// </summary>
public abstract class AbstractDownloadService : IDownloadService, IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Logger instance for logging messages.
    /// </summary>
    protected ILogger? Logger { get; set; }

    /// <summary>
    /// Semaphore to control parallel downloads.
    /// </summary>
    protected SemaphoreSlim? ParallelSemaphore { get; set; }

    /// <summary>
    /// Semaphore to ensure single instance operations.
    /// </summary>
    protected SemaphoreSlim SingleInstanceSemaphore { get; } = new(1, 1);

    /// <summary>
    /// Global cancellation token source for managing download cancellation.
    /// </summary>
    protected CancellationTokenSource? GlobalCancellationTokenSource { get; set; }

    /// <summary>
    /// Task completion source for managing asynchronous operations.
    /// </summary>
    protected TaskCompletionSource<AsyncCompletedEventArgs>? TaskCompletion { get; set; }

    /// <summary>
    /// Pause token source for managing download pausing.
    /// </summary>
    protected PauseTokenSource PauseTokenSource { get; }

    /// <summary>
    /// Chunk hub for managing download chunks.
    /// </summary>
    protected ChunkHub? ChunkHub { get; set; }

    /// <summary>
    /// List of request instances for download operations.
    /// </summary>
    protected List<Request> RequestInstances { get; set; } = [];

    /// <summary>
    /// Bandwidth tracker for download speed calculations.
    /// </summary>
    protected Bandwidth Bandwidth { get; }

    /// <summary>
    /// Configuration options for the download service.
    /// </summary>
    protected DownloadConfiguration Options { get; set; }

    /// <summary>
    /// Indicates whether the download service is currently busy.
    /// </summary>
    public bool IsBusy => Status == DownloadStatus.Running;

    /// <summary>
    /// Indicates whether the download operation has been cancelled.
    /// </summary>
    public bool IsCancelled => GlobalCancellationTokenSource?.IsCancellationRequested == true;

    /// <summary>
    /// Indicates whether the download operation is paused.
    /// </summary>
    public bool IsPaused => PauseTokenSource.IsPaused;

    /// <summary>
    /// The download package containing the necessary information for the download.
    /// </summary>
    public DownloadPackage Package { get; set; }

    /// <summary>
    /// The current status of the download operation.
    /// </summary>
    public DownloadStatus Status
    {
        get => Package?.Status ?? DownloadStatus.None;
        set => Package.Status = value;
    }

    /// <summary>
    /// The Socket client for the download service.
    /// </summary>
    protected SocketClient? Client { get; private set; }

    /// <summary>
    /// Event triggered when the download file operation is completed.
    /// </summary>
    public event EventHandler<AsyncCompletedEventArgs>? DownloadFileCompleted;

    /// <summary>
    /// Event triggered when the download progress changes.
    /// </summary>
    public event EventHandler<DownloadProgressChangedEventArgs>? DownloadProgressChanged;

    /// <summary>
    /// Event triggered when the progress of a chunk download changes.
    /// </summary>
    public event EventHandler<DownloadProgressChangedEventArgs>? ChunkDownloadProgressChanged;

    /// <summary>
    /// Event triggered when the download operation starts.
    /// </summary>
    public event EventHandler<DownloadStartedEventArgs>? DownloadStarted;

    /// <summary>
    /// Event triggered when the merge operation starts.
    /// </summary>
    public event EventHandler<MergeStartedEventArgs>? MergeStarted;

    /// <summary>
    /// Event triggered when the merge operation progress changed.
    /// </summary>
    public event EventHandler<MergeProgressChangedEventArgs>? MergeProgressChanged;

    /// <summary>
    /// Event triggered when a chunk starts downloading again from the beginning for some reason.
    /// </summary>
    public event EventHandler<ChunkDownloadRestartedEventArgs>? ChunkDownloadRestarted;

    /// <summary>
    /// Initializes a new instance of the <see cref="AbstractDownloadService"/> class with the specified options.
    /// </summary>
    /// <param name="options">The configuration options for the download service.</param>
    protected AbstractDownloadService(DownloadConfiguration? options)
    {
        PauseTokenSource = new PauseTokenSource();
        Bandwidth = new Bandwidth();
        Options = options ?? new DownloadConfiguration();
        Package = new DownloadPackage();
    }

    /// <summary>
    /// Downloads a file asynchronously using the specified <paramref name="package"/> and optional <paramref name="cancellationToken"/>.
    /// </summary>
    /// <param name="package">The download package containing the necessary information for the download.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the download.</param>
    /// <returns>A task that represents the asynchronous download operation. The task result contains the downloaded stream.</returns>
    public Task<Stream?> DownloadFileTaskAsync(DownloadPackage package, CancellationToken cancellationToken = default)
    {
        return DownloadFileTaskAsync(package, package.Urls, cancellationToken);
    }

    /// <summary>
    /// Downloads a file asynchronously using the specified <paramref name="package"/> and <paramref name="address"/>, with an optional <paramref name="cancellationToken"/>.
    /// </summary>
    /// <param name="package">The download package containing the necessary information for the download.</param>
    /// <param name="address">The URL address of the file to download.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the download.</param>
    /// <returns>A task that represents the asynchronous download operation. The task result contains the downloaded stream.</returns>
    public Task<Stream?> DownloadFileTaskAsync(DownloadPackage package, string address, CancellationToken cancellationToken = default)
    {
        return DownloadFileTaskAsync(package, [address], cancellationToken);
    }

    /// <summary>
    /// Downloads a file asynchronously using the specified <paramref name="package"/> and <paramref name="urls"/>, with an optional <paramref name="cancellationToken"/>.
    /// </summary>
    /// <param name="package">The download package containing the necessary information for the download.</param>
    /// <param name="urls">The array of URL addresses of the file to download.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the download.</param>
    /// <returns>A task that represents the asynchronous download operation. The task result contains the downloaded stream.</returns>
    public virtual async Task<Stream?> DownloadFileTaskAsync(DownloadPackage package, string[] urls, CancellationToken cancellationToken = default)
    {
        Package = package;
        await InitialDownloader(cancellationToken, urls).ConfigureAwait(false);
        return await StartDownloadAsync(false).ConfigureAwait(false);
    }

    /// <summary>
    /// Downloads a file asynchronously using the specified <paramref name="address"/> and optional <paramref name="cancellationToken"/>.
    /// </summary>
    /// <param name="address">The URL address of the file to download.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the download.</param>
    /// <returns>A task that represents the asynchronous download operation. The task result contains the downloaded stream.</returns>
    public Task<Stream?> DownloadFileTaskAsync(string address, CancellationToken cancellationToken = default)
    {
        return DownloadFileTaskAsync([address], cancellationToken);
    }

    /// <summary>
    /// Downloads a file asynchronously using the specified <paramref name="urls"/> and optional <paramref name="cancellationToken"/>.
    /// </summary>
    /// <param name="urls">The array of URL addresses of the file to download.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the download.</param>
    /// <returns>A task that represents the asynchronous download operation. The task result contains the downloaded stream.</returns>
    public virtual async Task<Stream?> DownloadFileTaskAsync(string[] urls, CancellationToken cancellationToken = default)
    {
        await InitialDownloader(cancellationToken, urls).ConfigureAwait(false);
        return await StartDownloadAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Downloads a file asynchronously using the specified <paramref name="address"/> and saves it to the specified <paramref name="fileName"/>, with an optional <paramref name="cancellationToken"/>.
    /// </summary>
    /// <param name="address">The URL address of the file to download.</param>
    /// <param name="fileName">The name of the file to save the download as.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the download.</param>
    /// <returns>A task that represents the asynchronous download operation.</returns>
    public Task DownloadFileTaskAsync(string address, string fileName, CancellationToken cancellationToken = default)
    {
        return DownloadFileTaskAsync([address], fileName, cancellationToken);
    }

    /// <summary>
    /// Downloads a file asynchronously using the specified <paramref name="urls"/> and saves it to the specified <paramref name="fileName"/>, with an optional <paramref name="cancellationToken"/>.
    /// </summary>
    /// <param name="urls">The array of URL addresses of the file to download.</param>
    /// <param name="fileName">The name of the file to save the download as.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the download.</param>
    /// <returns>A task that represents the asynchronous download operation.</returns>
    public virtual async Task DownloadFileTaskAsync(string[] urls, string fileName, CancellationToken cancellationToken = default)
    {
        await InitialDownloader(cancellationToken, urls).ConfigureAwait(false);
        await StartDownloadAsync(fileName).ConfigureAwait(false);
    }

    /// <summary>
    /// Downloads a file asynchronously using the specified <paramref name="address"/> and saves it to the specified <paramref name="folder"/>, with an optional <paramref name="cancellationToken"/>.
    /// </summary>
    /// <param name="address">The URL address of the file to download.</param>
    /// <param name="folder">The directory to save the downloaded file in.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the download.</param>
    /// <returns>A task that represents the asynchronous download operation.</returns>
    public Task DownloadFileTaskAsync(string address, DirectoryInfo folder, CancellationToken cancellationToken = default)
    {
        return DownloadFileTaskAsync([address], folder, cancellationToken);
    }

    /// <summary>
    /// Downloads a file asynchronously using the specified <paramref name="urls"/> and saves it to the specified <paramref name="folder"/>, with an optional <paramref name="cancellationToken"/>.
    /// </summary>
    /// <param name="urls">The array of URL addresses of the file to download.</param>
    /// <param name="folder">The directory to save the downloaded file in.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the download.</param>
    /// <returns>A task that represents the asynchronous download operation.</returns>
    public virtual async Task DownloadFileTaskAsync(string[] urls, DirectoryInfo folder, CancellationToken cancellationToken = default)
    {
        await InitialDownloader(cancellationToken, urls).ConfigureAwait(false);
        var name = await Client!.SetRequestFileNameAsync(RequestInstances[0]).ConfigureAwait(false);
        var filename = Path.Combine(folder.FullName, name);
        await StartDownloadAsync(filename).ConfigureAwait(false);
    }

    /// <summary>
    /// Cancels the current download operation.
    /// </summary>
    public virtual void Cancel()
    {
        GlobalCancellationTokenSource?.Cancel(true);
        Status = DownloadStatus.Stopped;
        Resume();
    }

    /// <summary>
    /// Cancels the current download operation asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous cancellation operation.</returns>
    public virtual async Task CancelTaskAsync()
    {
        Cancel();
        if (TaskCompletion != null)
            await TaskCompletion.Task.ConfigureAwait(false);
    }

    /// <summary>
    /// Resumes the paused download operation.
    /// </summary>
    public virtual void Resume()
    {
        Status = DownloadStatus.Running;
        PauseTokenSource.Resume();
    }

    /// <summary>
    /// Pauses the current download operation.
    /// </summary>
    public virtual void Pause()
    {
        PauseTokenSource.Pause();
        Status = DownloadStatus.Paused;
    }

    /// <summary>
    /// Clears the current download operation, including cancellation and disposal of resources.
    /// </summary>
    /// <returns>A task that represents the asynchronous clear operation.</returns>
    public virtual async Task ClearAsync()
    {
        try
        {
            if (IsBusy || IsPaused)
                await CancelTaskAsync().ConfigureAwait(false);

            await SingleInstanceSemaphore.WaitAsync().ConfigureAwait(false);

            ParallelSemaphore?.Dispose();
            GlobalCancellationTokenSource?.Dispose();
            Bandwidth.Reset();
            RequestInstances = [];

            if (TaskCompletion != null)
            {
                if (!TaskCompletion.Task.IsCompleted)
                    TaskCompletion.TrySetCanceled();

                TaskCompletion = null;
            }
            // Note: don't clear package from `DownloadService.Dispose()`.
            // Because maybe it will be used at another time.
        }
        finally
        {
            SingleInstanceSemaphore?.Release();
        }
    }

    /// <summary>
    /// Initializes the downloader with the specified <paramref name="cancellationToken"/> and <paramref name="addresses"/>.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the download.</param>
    /// <param name="addresses">The array of URL addresses of the file to download.</param>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    protected async Task InitialDownloader(CancellationToken cancellationToken, params string[] addresses)
    {
        await ClearAsync().ConfigureAwait(false);
        Status = DownloadStatus.Created;
        GlobalCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        TaskCompletion = new TaskCompletionSource<AsyncCompletedEventArgs>();
        Client = new SocketClient(Options.RequestConfiguration);
        RequestInstances = addresses.Select(url => new Request(url, Options.RequestConfiguration)).ToList();
        Package.Urls = RequestInstances.Select(req => req.Address.OriginalString).ToArray();
        ChunkHub = new ChunkHub(Options);
        ParallelSemaphore = new SemaphoreSlim(Options.ParallelCount, Options.ParallelCount);
    }

    /// <summary>
    /// Starts the download operation and saves it to the specified <paramref name="fileName"/>.
    /// </summary>
    /// <param name="fileName">The name of the file to save the download as.</param>
    /// <returns>A task that represents the asynchronous download operation.</returns>
    protected async Task StartDownloadAsync(string fileName)
    {
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            Package.FileName = fileName;
            var dirName = Path.GetDirectoryName(fileName);
            if (!string.IsNullOrWhiteSpace(dirName))
            {
                Directory.CreateDirectory(dirName); // ensure the folder is existing
                await Task.Delay(100).ConfigureAwait(false); // Add a small delay to ensure directory creation is complete
            }

            // Remove file from storage if exists
            if (File.Exists(fileName))
                File.Delete(fileName);
        }

        await StartDownloadAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Starts the download operation.
    /// </summary>
    /// <returns>A task that represents the asynchronous download operation. The task result contains the downloaded stream.</returns>
    protected abstract Task<Stream?> StartDownloadAsync(bool forceBuildStorage = true);

    /// <summary>
    /// Raises the <see cref="DownloadStarted"/> event.
    /// </summary>
    /// <param name="e">The event arguments for the download started event.</param>
    protected void OnDownloadStarted(DownloadStartedEventArgs e)
    {
        Status = DownloadStatus.Running;
        Package.IsSaving = true;
        DownloadStarted?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the <see cref="DownloadFileCompleted"/> event.
    /// </summary>
    /// <param name="e">The event arguments for the download file completed event.</param>
    protected void OnDownloadFileCompleted(AsyncCompletedEventArgs e)
    {
        Package.IsSaving = false;

        if (e.Cancelled)
        {
            Status = DownloadStatus.Stopped;
        }
        else if (e.Error != null)
        {
            if (Options.ClearPackageOnCompletionWithFailure)
            {
                Package.Clear();
                File.Delete(Package.FileName);
            }
        }
        else // completed
        {
            Package.Clear();
        }

        TaskCompletion?.TrySetResult(e);
        DownloadFileCompleted?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the <see cref="ChunkDownloadProgressChanged"/> and <see cref="DownloadProgressChanged"/> events in a unified way.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event arguments for the download progress changed event.</param>
    protected void OnChunkDownloadProgressChanged(object? sender, DownloadProgressChangedEventArgs e)
    {
        if (e.ReceivedBytesSize > Package.TotalFileSize)
            Package.TotalFileSize = e.ReceivedBytesSize;

        Bandwidth.CalculateSpeed(e.ProgressedByteSize);
        Options.ActiveChunks = Options.ParallelCount - ParallelSemaphore!.CurrentCount;
        var totalProgressArg = new DownloadProgressChangedEventArgs(nameof(DownloadService))
        {
            TotalBytesToReceive = Package.TotalFileSize,
            ReceivedBytesSize = Package.ReceivedBytesSize,
            BytesPerSecondSpeed = Bandwidth.Speed,
            AverageBytesPerSecondSpeed = Bandwidth.AverageSpeed,
            ProgressedByteSize = e.ProgressedByteSize,
            ReceivedBytes = e.ReceivedBytes,
            ActiveChunks = Options.ActiveChunks
        };

        Package.SaveProgress = totalProgressArg.ProgressPercentage;
        e.ActiveChunks = totalProgressArg.ActiveChunks;
        ChunkDownloadProgressChanged?.Invoke(this, e);
        DownloadProgressChanged?.Invoke(this, totalProgressArg);
    }

    /// <summary>
    /// Raises the <see cref="MergeStarted"/> event.
    /// </summary>
    protected void OnMergeStarted()
    {
        var totalFileSize = Package.TotalFileSize;
        var numberOfChunks = Package.Chunks.Length;
        var chunksDirectoryPath = Package.Chunks.Length > 1 ? (Path.GetDirectoryName(Package.Chunks[0].TempFilePath) ?? string.Empty) : string.Empty;
        var eventArgs = new MergeStartedEventArgs(totalFileSize, numberOfChunks, chunksDirectoryPath);
        MergeStarted?.Invoke(this, eventArgs);
    }

    /// <summary>
    /// Raises the <see cref="MergeProgressChanged"/> event.
    /// </summary>
    /// <param name="e">The event arguments for the merge progress changed event.</param>
    protected void OnMergeProgressChanged(MergeProgressChangedEventArgs e)
    {
        MergeProgressChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the <see cref="ChunkDownloadRestarted"/> event.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event arguments for the chunk download restarted event.</param>
    protected void OnChunkDownloadRestarted(object? sender, ChunkDownloadRestartedEventArgs e)
    {
        ChunkDownloadRestarted?.Invoke(this, e);
    }

    /// <summary>
    /// Adds a logger to the download service.
    /// </summary>
    /// <param name="logger">The logger instance to add.</param>
    public void AddLogger(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Disposes of the download service, including clearing the current download operation.
    /// </summary>
    public void Dispose()
    {
        ClearAsync().Wait();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes asynchronously of the download service, including clearing the current download operation.
    /// </summary>
    public virtual async ValueTask DisposeAsync()
    {
        await ClearAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}