using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.CustomEventArgs;
using CrossPlatformDownloadManager.Utils.Enums;
using MathNet.Numerics;
using MathNet.Numerics.Statistics;
using Microsoft.Extensions.DependencyInjection;
using MultipartDownloader.Core;
using MultipartDownloader.Core.CustomEventArgs;
using MultipartDownloader.Core.Enums;
using RolandK.AvaloniaExtensions.DependencyInjection;
using Serilog;
using Constants = CrossPlatformDownloadManager.Utils.Constants;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService.Models;

/// <summary>
/// Handle downloading file with specific configuration
/// </summary>
public class FileDownloader
{
    #region Constants

    /// <summary>
    /// The number of samples of the download speed to calculate average download speed and time left of the download process.
    /// </summary>
    private const int NumberOfSamples = 101;

    #endregion

    #region Private Fields

    /// <summary>
    /// Access to app services with AppService service.
    /// </summary>
    private readonly IAppService _appService;

    /// <summary>
    /// The http client handler.
    /// </summary>
    private readonly DownloadRequest _downloadRequest;

    /// <summary>
    /// Cancellation token source for checking resume capability. 
    /// </summary>
    private CancellationTokenSource? _checkResumeCancelToken;

    /// <summary>
    /// The timer that manage various operations during the download process.
    /// </summary>
    private DispatcherTimer? _downloaderTimer;

    /// <summary>
    /// Indicates the ticks count of the downloader timer to calculate the elapsed timer and time left of the download.
    /// </summary>
    private int _downloaderTimerTicksCount = 1;

    /// <summary>
    /// Indicate the elapsed time of the start download process.
    /// </summary>
    private TimeSpan? _elapsedTimeOfStartingDownload;

    /// <summary>
    /// The total bytes size that received from server.
    /// </summary>
    private long _receivedBytesSize;

    /// <summary>
    /// A list containing download speed samples for finding median download speeds.
    /// </summary>
    private readonly List<double> _downloadSpeeds = [];

    /// <summary>
    /// A list containing median download speed samples for calculating average download speed and time left of the download process.
    /// </summary>
    private readonly List<double> _medianDownloadSpeeds = [];

    /// <summary>
    /// A list containing the progress state of each chunk.
    /// </summary>
    private List<ChunkProgress>? _chunkProgresses;

    /// <summary>
    /// This variable is used to keep the tasks that should be run asynchronously after the download is completed.
    /// </summary>
    private readonly List<Func<DownloadFileViewModel?, Task>> _completedAsyncTasks = [];

    /// <summary>
    /// This variable is used to keep the tasks that should be run synchronously after the download is completed.
    /// </summary>
    private readonly List<Action<DownloadFileViewModel?>> _completedSyncTasks = [];

    #endregion

    #region Properties

    /// <summary>
    /// Gets the DownloadFile object that contains the information of the file to be downloaded.
    /// </summary>
    public DownloadFileViewModel DownloadFile { get; }

    /// <summary>
    /// Gets the DownloadConfiguration object that contains the information of the download configuration.
    /// </summary>
    public DownloadConfiguration DownloadConfiguration { get; }

    /// <summary>
    /// Gets the DownloadService object that contains the information of the download service.
    /// </summary>
    public DownloadService DownloadService { get; }

    /// <summary>
    /// Gets a value that indicates the download window of the current download file.
    /// </summary>
    public DownloadWindow? DownloadWindow { get; private set; }

    #endregion

    /// <summary>
    /// Initialize a new instance of the <see cref="FileDownloader"/> class.
    /// </summary>
    /// <param name="downloadFile">The file to be downloaded.</param>
    public FileDownloader(DownloadFileViewModel downloadFile)
    {
        _appService = GetAppService();
        _downloadRequest = new DownloadRequest(downloadFile.Url ?? string.Empty);

        DownloadFile = downloadFile;
        DownloadConfiguration = GetDownloadConfiguration();
        DownloadService = GetDownloadService();
    }

    /// <summary>
    /// Starts the download process.
    /// </summary>
    public async Task StartDownloadFileAsync()
    {
        // Subscribe to events
        DownloadService.DownloadStarted += DownloadServiceOnDownloadStarted;
        DownloadService.DownloadFileCompleted += DownloadServiceOnDownloadFileCompleted;
        DownloadService.DownloadProgressChanged += DownloadServiceOnDownloadProgressChanged;
        DownloadService.ChunkDownloadProgressChanged += DownloadServiceOnChunkDownloadProgressChanged;
        DownloadService.MergeStarted += DownloadServiceOnMergeStarted;
        DownloadService.MergeProgressChanged += DownloadServiceOnMergeProgressChanged;
        DownloadService.ChunkDownloadRestarted += DownloadServiceOnChunkDownloadRestarted;

        // Get save location of the file
        var downloadPath = DownloadFile.SaveLocation;
        // Check if save location is null or empty
        if (downloadPath.IsStringNullOrEmpty())
        {
            // Get general category
            var generalDirectory = _appService
                .CategoryService
                .Categories
                .FirstOrDefault(c => c.Title.Equals(Constants.GeneralCategoryTitle));

            // Set the save location of the general category to downloadPath
            downloadPath = generalDirectory?.CategorySaveDirectory?.SaveDirectory;
            // Check if downloadPath still is null or empty, return
            if (downloadPath.IsStringNullOrEmpty())
                return;
        }

        // Check if the file name and url are valid
        if (DownloadFile.FileName.IsStringNullOrEmpty()
            || DownloadFile.Url.IsStringNullOrEmpty()
            || !DownloadFile.Url.CheckUrlValidation())
        {
            return;
        }

        // Make sure that the download path is exists
        if (!Directory.Exists(downloadPath!))
            Directory.CreateDirectory(downloadPath!);

        // Update save location if not equal to download path
        if (DownloadFile.SaveLocation?.Equals(downloadPath) != true)
            DownloadFile.SaveLocation = downloadPath;

        // Create chunk data for showing in the UI
        CreateChunksData(DownloadConfiguration.ChunkCount);
        // Start the downloader timer for calculating some processes during download process
        StartDownloaderTimer();

        // Check resume capability
        DownloadFile.CanResumeDownload = null;
        await CheckResumeCapabilityAsync().ConfigureAwait(false);

        // Get file name
        var fileName = Path.Combine(DownloadFile.SaveLocation!, DownloadFile.FileName!);
        // Get download package
        var downloadPackage = DownloadFile.GetDownloadPackage();
        // If download package is null we have to download file from the beginning
        if (downloadPackage == null)
        {
            await DownloadService.DownloadFileTaskAsync(address: DownloadFile.Url!, fileName: fileName).ConfigureAwait(false);
        }
        // Otherwise, use the download package to continue the download process
        else
        {
            // Load previous chunks data
            LoadChunksData(downloadPackage.Chunks);

            // Update download url if user changed it
            var urls = downloadPackage.Urls.ToList();
            var currentUrl = urls.FirstOrDefault(u => u.Equals(DownloadFile.Url!));
            if (currentUrl.IsStringNullOrEmpty())
            {
                urls.Clear();
                urls.Add(DownloadFile.Url!);

                downloadPackage.Urls = urls.ToArray();
            }

            // Continue the download process
            await DownloadService.DownloadFileTaskAsync(downloadPackage).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Stops the download process.
    /// </summary>
    public async Task StopDownloadFileAsync()
    {
        // Reset download options
        ResetDownload();
        // Change download status to stopping
        DownloadFile.Status = DownloadFileStatus.Stopping;
        // Raise download stopped event
        DownloadFile.RaiseDownloadStoppedEvent(new DownloadFileEventArgs(DownloadFile.Id));
        // Cancel download
        await DownloadService.CancelTaskAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Resumes the download process.
    /// </summary>
    public void ResumeDownloadFile()
    {
        // Resume the download
        DownloadService.Resume();
        // Start the downloader timer to resume processes
        _downloaderTimer?.Start();
        // Change the download status to downloading
        DownloadFile.Status = DownloadFileStatus.Downloading;
        // Invoke DownloadResumed event
        DownloadFile.RaiseDownloadResumedEvent(new DownloadFileEventArgs(DownloadFile.Id));
    }

    /// <summary>
    /// Pauses the download process.
    /// </summary>
    public void PauseDownloadFile()
    {
        // Pause the download
        DownloadService.Pause();
        // Stop the downloader timer to stop processes
        _downloaderTimer?.Stop();
        // Change the download status to paused
        DownloadFile.Status = DownloadFileStatus.Paused;
        // Update chunks data to update the UI
        UpdateChunksData();
        // Save download package
        SaveDownloadPackage(DownloadService.Package);
        // Invoke DownloadPaused event
        DownloadFile.RaiseDownloadPausedEvent(new DownloadFileEventArgs(DownloadFile.Id));
    }

    /// <summary>
    /// Creates a window for showing the progress of the download.
    /// </summary>
    /// <param name="showWindow">This parameter indicates whether the created window should be opened and showed to the user or not.</param>
    public void CreateDownloadWindow(bool showWindow = true)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var viewModel = new DownloadWindowViewModel(_appService, DownloadFile);
            DownloadWindow = new DownloadWindow { DataContext = viewModel };
            DownloadWindow.Closing += DownloadWindowOnClosing;

            if (showWindow)
                DownloadWindow.Show();
        });
    }

    /// <summary>
    /// Shows or focuses the download window.
    /// </summary>
    public void ShowOrFocusWindow()
    {
        if (DownloadWindow == null)
            return;

        Dispatcher.UIThread.Post(() =>
        {
            if (!DownloadWindow.IsVisible)
                DownloadWindow.Show();
            else
                DownloadWindow.Focus();
        });
    }

    /// <summary>
    /// Adds an async task to the completed tasks list.
    /// This task will be executed when the download is finished.
    /// </summary>
    /// <param name="completedTask">The task that is to be executed when the download is finished.</param>
    public void AddAsyncCompletedTask(Func<DownloadFileViewModel?, Task> completedTask)
    {
        _completedAsyncTasks.Add(completedTask);
    }

    /// <summary>
    /// Adds a sync task to the completed tasks list.
    /// This task will be executed when the download is finished.
    /// </summary>
    /// <param name="completedTask">The task that is to be executed when the download is finished.</param>
    public void AddSyncCompletedTask(Action<DownloadFileViewModel?> completedTask)
    {
        _completedSyncTasks.Add(completedTask);
    }

    /// <summary>
    /// Gets the completed async tasks.
    /// </summary>
    /// <returns>Returns a list of completed async tasks.</returns>
    public List<Func<DownloadFileViewModel?, Task>> GetCompletedAsyncTasks()
    {
        return _completedAsyncTasks;
    }

    /// <summary>
    /// Gets the completed sync tasks.
    /// </summary>
    /// <returns>Returns a list of completed sync tasks.</returns>
    public List<Action<DownloadFileViewModel?>> GetCompletedSyncTasks()
    {
        return _completedSyncTasks;
    }

    /// <summary>
    /// Clears all tasks from the completed tasks list.
    /// </summary>
    public void ClearCompletedAsyncTasks()
    {
        _completedAsyncTasks.Clear();
    }

    /// <summary>
    /// Clears all tasks from the completed tasks list.
    /// </summary>
    public void ClearCompletedSyncTasks()
    {
        _completedSyncTasks.Clear();
    }

    #region Event handlers

    /// <summary>
    /// Handles the DownloadStarted event of the DownloadService.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The arguments of the event.</param>
    private void DownloadServiceOnDownloadStarted(object? sender, DownloadStartedEventArgs e)
    {
        // Change the status of the download to downloading
        DownloadFile.Status = DownloadFileStatus.Downloading;
        // Update the last try date of the download
        DownloadFile.LastTryDate = DateTime.Now;
    }

    /// <summary>
    /// Handles the DownloadFileCompleted event of the DownloadService.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The arguments of the event.</param> 
    private void DownloadServiceOnDownloadFileCompleted(object? sender, AsyncCompletedEventArgs e)
    {
        var isSuccess = true;
        Exception? error = null;

        // Save download package
        SaveDownloadPackage(DownloadService.Package);
        // Update chunk progresses for the last time
        UpdateChunksData();
        // Reset download options
        ResetDownload();

        // Change download status and if error occurred, store that
        if (e is { Error: not null, Cancelled: false })
        {
            DownloadFile.Status = DownloadFileStatus.Error;
            isSuccess = false;
            error = e.Error;
        }
        else
        {
            DownloadFile.Status = e.Cancelled ? DownloadFileStatus.Stopped : DownloadFileStatus.Completed;
        }

        // Raise download finished event
        DownloadFile.RaiseDownloadFinishedEvent(new DownloadFileEventArgs(DownloadFile.Id, isSuccess, error));
    }

    /// <summary>
    /// Handles the DownloadProgressChanged event of the DownloadService.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The arguments of the event.</param> 
    private void DownloadServiceOnDownloadProgressChanged(object? sender, DownloadProgressChangedEventArgs e)
    {
        // Update download progress
        DownloadFile.DownloadProgress = (float)e.ProgressPercentage;
        // Update transfer rate
        DownloadFile.TransferRate = (float)e.BytesPerSecondSpeed;
        // Update downloaded size
        DownloadFile.DownloadedSize = e.ReceivedBytesSize;
        // Update total file size if it's not set or the size is changed
        if (DownloadFile.Size == null || e.TotalBytesToReceive != (long)DownloadFile.Size.Value)
            DownloadFile.Size = e.TotalBytesToReceive;

        // Save required data to calculate time left
        _receivedBytesSize = e.ReceivedBytesSize;
        // Store average download speeds to find median
        _downloadSpeeds.Add(e.AverageBytesPerSecondSpeed);
        // Store specified number of samples
        // For calculating median it's better to have odd number of samples
        if (_downloadSpeeds.Count > NumberOfSamples)
            _downloadSpeeds.RemoveAt(0);
    }

    /// <summary>
    /// Handles the ChunkDownloadProgressChanged event of the DownloadService.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The arguments of the event.</param>
    private void DownloadServiceOnChunkDownloadProgressChanged(object? sender, DownloadProgressChangedEventArgs e)
    {
        // Check if chunk progresses are null or empty
        if (_chunkProgresses == null || _chunkProgresses.Count == 0)
            return;

        // Find chunk progress by progress id
        var chunkProgress = _chunkProgresses.FirstOrDefault(cp => cp.ProgressId.Equals(e.ProgressId));
        // Check if chunk progress is null
        if (chunkProgress == null)
            return;

        // Update chunk progress data based on new data
        chunkProgress.ReceivedBytesSize = e.ReceivedBytesSize;
        chunkProgress.TotalBytesToReceive = e.TotalBytesToReceive;
        chunkProgress.IsCompleted = e.ReceivedBytesSize >= e.TotalBytesToReceive;
    }

    /// <summary>
    /// Handles the MergeStarted event of the DownloadService.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The arguments of the event.</param>
    private void DownloadServiceOnMergeStarted(object? sender, MergeStartedEventArgs e)
    {
        // Change the status of the download to merging
        DownloadFile.Status = DownloadFileStatus.Merging;
        // Stop the download timer
        _downloaderTimer?.Stop();
    }

    /// <summary>
    /// Handles the MergeProgressChanged event of the DownloadService.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The arguments of the event.</param>
    private void DownloadServiceOnMergeProgressChanged(object? sender, MergeProgressChangedEventArgs e)
    {
        // Update the merge progress
        DownloadFile.MergeProgress = e.Progress;
    }

    /// <summary>
    /// Handles the ChunkDownloadRestarted event of the DownloadService.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The arguments of the event.</param>
    private static async void DownloadServiceOnChunkDownloadRestarted(object? sender, ChunkDownloadRestartedEventArgs e)
    {
        try
        {
            switch (e.Reason)
            {
                case RestartReason.FileSizeIsNotMatchWithChunkLength:
                {
                    await DialogBoxManager.ShowWarningDialogAsync("Part Size Mismatch Detected",
                        "A problem was detected while downloading a part of the file. the received size does not match the expected size. This part will be re-downloaded to maintain data integrity and ensure a complete file.",
                        DialogButtons.Ok);

                    break;
                }

                case RestartReason.TempFileCorruption:
                {
                    await DialogBoxManager.ShowWarningDialogAsync("Corrupted Temporary File Detected",
                        "The temporary file for a part of the download appears to be corrupted or unreadable. To ensure data integrity and a successful download, this part will be re-downloaded from the server.",
                        DialogButtons.Ok);

                    break;
                }

                default:
                    throw new InvalidOperationException("We detected that the chunk download was restarted, but we don't know why. Please report this issue to the developer.");
            }
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    /// <summary>
    /// Handles the Tick event of the <see cref="_downloaderTimer"/> timer.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The arguments of the event</param>
    private void DownloaderTimerOnTick(object? sender, EventArgs e)
    {
        // If timer ticks count is divisible by 4, it means 1 second passed, and we can calculate the elapsed time and time left
        if (_downloaderTimerTicksCount % 4 == 0)
        {
            // Calculate elapsed time from the start time of the download
            CalculateElapsedTime();
            // Calculate time left of the download process
            CalculateTimeLeft();
            // Reset tick count
            _downloaderTimerTicksCount = 1;
        }
        else
        {
            // Increase tick count
            _downloaderTimerTicksCount++;
        }

        // Update chunks data
        UpdateChunksData();
    }

    /// <summary>
    /// Handles the Closing event of the <see cref="DownloadWindow"/> class.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="WindowClosingEventArgs"/> event arguments.</param>
    private async void DownloadWindowOnClosing(object? sender, WindowClosingEventArgs e)
    {
        try
        {
            // Check if download window is not null and has data context of type DownloadWindowViewModel
            if (DownloadWindow is not { DataContext: DownloadWindowViewModel viewModel })
                return;

            // Check if download window can be closed
            if (!viewModel.CanCloseWindow)
            {
                // Cancel the closing of the window
                e.Cancel = true;
                Dispatcher.UIThread.Post(() => DownloadWindow!.Hide());
                return;
            }

            // Unsubscribe from Closing event
            DownloadWindow.Closing -= DownloadWindowOnClosing;
            // Stop update chunks data timer
            DownloadWindow.StopUpdateChunksDataTimer();
            // Remove event handlers
            viewModel.RemoveEventHandlers();
            // Stop download if it's not stopped yet
            if (viewModel.DownloadFile.IsDownloading || viewModel.DownloadFile.IsPaused)
                await viewModel.StopDownloadAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while closing download window. Error message: {ErrorMessage}", ex.Message);

            await DialogBoxManager.ShowDangerDialogAsync("Error closing download window",
                $"An error occurred while closing download window.\nError message: {ex.Message}",
                DialogButtons.Ok);
        }
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Try find AppService from services and return it.
    /// </summary>
    /// <returns>Returns an instance of the IAppService interface.</returns>
    /// <exception cref="InvalidOperationException">If AppService not found.</exception>
    private static IAppService GetAppService()
    {
        // Get the current application's service provider
        var serviceProvider = Application.Current?.TryGetServiceProvider();
        // Get the service of type IAppService from the service provider
        var appService = serviceProvider?.GetService<IAppService>() ?? throw new InvalidOperationException("App service not found.");
        // Return the app service
        return appService;
    }

    /// <summary>
    /// Create download configuration for download file.
    /// </summary>
    /// <returns>Returns download configuration for download service.</returns>
    private DownloadConfiguration GetDownloadConfiguration()
    {
        // Create a new DownloadConfiguration object
        var configuration = new DownloadConfiguration
        {
            ChunkCount = _appService.SettingsService.Settings.MaximumConnectionsCount,
            MaximumBytesPerSecond = GetMaximumBytesPerSecond(),
            ParallelDownload = true,
            MaximumMemoryBufferBytes = GetMaximumMemoryBufferBytes(),
            ReserveStorageSpaceBeforeStartingDownload = false,
            ChunkFilesOutputDirectory = _appService.SettingsService.GetTemporaryFileLocation(),
            MaxRestartWithoutClearTempFile = 5,
            MaximumBytesPerSecondForMerge = GetMaximumBytesPerSecondForMerge()
        };

        // If the proxy is not null, set the proxy in the request configuration
        if (_downloadRequest.Proxy != null)
            configuration.RequestConfiguration.Proxy = _downloadRequest.Proxy;

        // Return the configuration
        return configuration;
    }

    /// <summary>
    /// Calculate the maximum bytes per second for download operation.
    /// </summary>
    /// <returns>Maximum bytes per second for download operation.</returns>
    private long GetMaximumBytesPerSecond()
    {
        // Check if the speed limiter is enabled
        if (!_appService.SettingsService.Settings.IsSpeedLimiterEnabled)
            return 0;

        // Get the limit speed from the settings
        var limitSpeed = (long)(_appService.SettingsService.Settings.LimitSpeed ?? 0);
        // Get the limit unit from the settings
        var limitUnit = _appService.SettingsService.Settings.LimitUnit?.Equals("KB", StringComparison.OrdinalIgnoreCase) == true
            ? Constants.KiloByte
            : Constants.MegaByte;

        // Return the limit speed multiplied by the limit unit
        return limitSpeed * limitUnit;
    }

    /// <summary>
    /// Calculates the maximum bytes per second speed for merge operation.
    /// </summary>
    /// <returns>Maximum bytes per second speed for merge operation.</returns>
    private long GetMaximumBytesPerSecondForMerge()
    {
        // Check if the speed limiter is enabled
        if (!_appService.SettingsService.Settings.IsMergeSpeedLimitEnabled)
            return 0;

        // Get the limit speed from the settings
        var limitSpeed = (long)(_appService.SettingsService.Settings.MergeLimitSpeed ?? 0);
        // Get the limit unit from the settings
        var limitUnit = _appService.SettingsService.Settings.MergeLimitUnit?.Equals("KB", StringComparison.OrdinalIgnoreCase) == true
            ? Constants.KiloByte
            : Constants.MegaByte;

        // Return the limit speed multiplied by the limit unit
        return limitSpeed * limitUnit;
    }

    /// <summary>
    /// Calculates the maximum bytes that can be stored in memory before writing to the disk.
    /// </summary>
    /// <returns>Returns maximum bytes that can be stored in memory before writing to the disk.</returns>
    private long GetMaximumMemoryBufferBytes()
    {
        // Get the limit speed from the settings
        var maximumMemoryBufferBytes = _appService.SettingsService.Settings.MaximumMemoryBufferBytes;
        // Get the limit unit from the settings
        var unit = _appService.SettingsService.Settings.MaximumMemoryBufferBytesUnit.Equals("KB", StringComparison.OrdinalIgnoreCase)
            ? Constants.KiloByte
            : Constants.MegaByte;

        // Return the limit speed multiplied by the limit unit
        return maximumMemoryBufferBytes * unit;
    }

    /// <summary>
    /// Create download service for managing download file.
    /// </summary>
    /// <returns>Returns an instance of DownloadService.</returns>
    private DownloadService GetDownloadService()
    {
        return new DownloadService(DownloadConfiguration);
    }

    /// <summary>
    /// Resets the download options.
    /// </summary>
    private void ResetDownload()
    {
        // Clear elapsed time timer
        if (_downloaderTimer != null)
        {
            _downloaderTimer.Stop();
            _downloaderTimer.Tick -= DownloaderTimerOnTick;
            _downloaderTimer = null;
        }

        // If resume capability cancellation token source is not null
        if (_checkResumeCancelToken != null)
        {
            // Make sure resume capability cancelled
            _checkResumeCancelToken.Cancel();
            _checkResumeCancelToken = null;
        }

        // Reset elapsed time of starting download
        _elapsedTimeOfStartingDownload = null;
        // Reset chunk progresses
        _chunkProgresses = null;
        // Reset received bytes size
        _receivedBytesSize = 0;
        // Clear download speeds list
        _downloadSpeeds.Clear();
        // Clear median download speeds list
        _medianDownloadSpeeds.Clear();
        // Reset resume capability
        DownloadFile.CanResumeDownload = null;
    }

    /// <summary>
    /// Creates data for all chunks to manage the progress of chunks. 
    /// </summary>
    /// <param name="count">The count of chunks.</param>
    private void CreateChunksData(int count)
    {
        // Create a list of chunk data
        var chunks = new List<ChunkDataViewModel>();
        // Create a list of chunk progresses
        _chunkProgresses ??= [];

        // Iterate through the count of chunks
        for (var i = 0; i < count; i++)
        {
            // Add a new chunk data with chunk index to the list
            chunks.Add(new ChunkDataViewModel { ChunkIndex = i });
            // For each chunk data that we create, we have to add a chunk progress to the list also
            _chunkProgresses.Add(new ChunkProgress { ProgressId = i.ToString() });
        }

        // Set the chunks data of the download file to the list of chunk data
        DownloadFile.ChunksData = chunks.ToObservableCollection();
    }

    /// <summary>
    /// Starts the timer for calculating elapsed time and time left of the download process.
    /// </summary>
    private void StartDownloaderTimer()
    {
        _downloaderTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.25) };
        _downloaderTimer.Tick += DownloaderTimerOnTick;
        _downloaderTimer.Start();
    }

    /// <summary>
    /// Calculates the elapsed time from the beginning of the download process.
    /// </summary>
    private void CalculateElapsedTime()
    {
        _elapsedTimeOfStartingDownload ??= TimeSpan.Zero;
        _elapsedTimeOfStartingDownload = TimeSpan.FromSeconds(_elapsedTimeOfStartingDownload.Value.TotalSeconds + 1);
        DownloadFile.ElapsedTime = _elapsedTimeOfStartingDownload;
    }

    /// <summary>
    /// Calculates the remaining time to the end of download process.
    /// </summary>
    private void CalculateTimeLeft()
    {
        var timeLeft = TimeSpan.Zero;

        // Calculate median download speed
        var median = _downloadSpeeds.Median();
        // Store median download speed to calculate average better
        _medianDownloadSpeeds.Add(median);
        // Store specified number of samples
        if (_medianDownloadSpeeds.Count > NumberOfSamples)
            _medianDownloadSpeeds.RemoveAt(0);

        // Calculate average download speed
        var averageDownloadSpeed = _medianDownloadSpeeds.Mean();
        // Make sure download speeds count is greater than 1
        if (averageDownloadSpeed <= 0)
        {
            DownloadFile.TimeLeft = timeLeft;
            return;
        }

        // Calculate remain size
        var remainSizeToReceive = (DownloadFile.Size ?? 0) - _receivedBytesSize;
        // Calculate remain seconds by average download speed
        var remainSeconds = (remainSizeToReceive / averageDownloadSpeed).Round(0);
        if (!double.IsInfinity(remainSeconds) && !double.IsNaN(remainSeconds))
            timeLeft = TimeSpan.FromSeconds(remainSeconds);

        // Set time left of the download file
        DownloadFile.TimeLeft = timeLeft;
    }

    /// <summary>
    /// Updates the chunks data during download process.
    /// </summary>
    private void UpdateChunksData()
    {
        // Check if chunk progresses is null or empty
        if (_chunkProgresses == null || _chunkProgresses.Count == 0)
            return;

        foreach (var chunkProgress in _chunkProgresses)
        {
            // Try parse progress id
            if (!int.TryParse(chunkProgress.ProgressId, out var progressId))
                continue;

            // Find chunk data by progress id
            var chunkData = DownloadFile.ChunksData.FirstOrDefault(cd => cd.ChunkIndex == progressId);
            // Check if chunk data is null
            if (chunkData == null)
                continue;

            // If the chunk is completed change the chunk info to "Completed"
            if (chunkProgress.IsCompleted)
            {
                // If chunk info is not equal to "Completed", update it
                chunkData.Info = "Completed";
                // Update the chunk downloaded size and total size
                chunkData.DownloadedSize = chunkData.TotalSize = chunkProgress.TotalBytesToReceive;
            }
            // Otherwise, if the download is paused change the chunk info to "Paused"
            else if (DownloadFile.IsPaused)
            {
                // If chunk info is not equal to "Paused", update it
                chunkData.Info = "Paused";
            }
            // Otherwise, update the chunk progress
            else
            {
                // Check if the previous chunk downloaded size is equal to the current received bytes size
                // This means the download is not progressing, so change the chunk info to "Connecting..."
                // Otherwise, change the chunk info to "Receiving..."
                chunkData.Info = chunkData.DownloadedSize == chunkProgress.ReceivedBytesSize ? "Connecting..." : "Receiving...";

                // Update the chunk downloaded size and total size
                chunkData.DownloadedSize = chunkProgress.ReceivedBytesSize;
                chunkData.TotalSize = chunkProgress.TotalBytesToReceive;
            }
        }
    }

    /// <summary>
    /// Saves the download package to the download file.
    /// </summary>
    /// <param name="downloadPackage"></param>
    private void SaveDownloadPackage(DownloadPackage? downloadPackage)
    {
        DownloadFile.DownloadPackage = downloadPackage?.ConvertToJson();
    }

    /// <summary>
    /// Checks whether the download file URL is resume-capable.
    /// </summary>
    private async Task CheckResumeCapabilityAsync()
    {
        try
        {
            // Check url
            if (DownloadFile.Url.IsStringNullOrEmpty() || !DownloadFile.Url.CheckUrlValidation())
            {
                DownloadFile.CanResumeDownload = false;
                return;
            }

            // Create a cancellation token source for managing resume capability operation
            _checkResumeCancelToken = new CancellationTokenSource();
            // Check if URL supports download in range
            DownloadFile.CanResumeDownload = await _downloadRequest.CheckSupportsDownloadInRangeAsync(_checkResumeCancelToken.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Log error
            Log.Error(ex, "An error occurred while checking resume capability. Error message: {ErrorMessage}", ex.Message);
            // Set resume capability to false
            DownloadFile.CanResumeDownload = false;
        }
    }

    /// <summary>
    /// Loads the data of the chunks with the data stored in the database.
    /// Updates the chunk progresses for resuming download process after canceling it.
    /// </summary>
    /// <param name="chunks">The chunks that we want to load.</param>
    private void LoadChunksData(Chunk[] chunks)
    {
        if (chunks.Length == 0)
            return;

        foreach (var chunk in chunks)
        {
            // Find chunk progress
            var chunkProgress = _chunkProgresses?.Find(c => c.ProgressId.Equals(chunk.Id));
            // Check if chunk progress is null
            if (chunkProgress == null)
                continue;

            // Update the chunk progress data
            chunkProgress.ReceivedBytesSize = chunk.IsDownloadCompleted() ? chunk.Length : chunk.Position;
            chunkProgress.TotalBytesToReceive = chunk.Length;
            chunkProgress.IsCompleted = chunk.IsDownloadCompleted();
        }
    }

    #endregion
}