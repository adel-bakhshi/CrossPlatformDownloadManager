using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Audio;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Audio.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.SettingsService;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.CustomEventArgs;
using CrossPlatformDownloadManager.Utils.Enums;
using CrossPlatformDownloadManager.Utils.PropertyChanged;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using RolandK.AvaloniaExtensions.DependencyInjection;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadQueueService;

/// <summary>
/// Service for managing download queues, including scheduling, file assignment, and lifecycle control.
/// </summary>
public class DownloadQueueService : PropertyChangedBase, IDownloadQueueService
{
    #region Private Fields

    /// <summary>
    /// The unit of work service for accessing the database.
    /// </summary>
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// The mapper service for converting and mapping different types.
    /// </summary>
    private readonly IMapper _mapper;

    /// <summary>
    /// The download file service for handling download file.
    /// </summary>
    private readonly IDownloadFileService _downloadFileService;

    /// <summary>
    /// The settings service for accessing application settings.
    /// </summary>
    private readonly ISettingsService _settingsService;

    /// <summary>
    /// The timer for managing schedules.
    /// </summary>
    private readonly DispatcherTimer _scheduleManagerTimer;

    // Backing field for properties
    private ObservableCollection<DownloadQueueViewModel> _downloadQueues;

    #endregion

    #region Properties

    public ObservableCollection<DownloadQueueViewModel> DownloadQueues
    {
        get => _downloadQueues;
        private set => SetField(ref _downloadQueues, value);
    }

    #endregion

    #region Events

    public event EventHandler? DataChanged;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadQueueService"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work for database operations.</param>
    /// <param name="mapper">The mapper for object mapping.</param>
    /// <param name="downloadFileService">The service for managing download files.</param>
    /// <param name="settingsService">The service for accessing application settings.</param>
    public DownloadQueueService(IUnitOfWork unitOfWork,
        IMapper mapper,
        IDownloadFileService downloadFileService,
        ISettingsService settingsService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _downloadFileService = downloadFileService;
        _downloadFileService.DataChanged += DownloadFileServiceOnDataChanged;
        _settingsService = settingsService;

        _downloadQueues = [];

        _scheduleManagerTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _scheduleManagerTimer.Tick += ScheduleManagerTimerOnTick;

        Log.Debug("DownloadQueueService initialized successfully.");
    }

    public async Task LoadDownloadQueuesAsync(bool addDefaultDownloadQueue = false)
    {
        Log.Information("Loading download queues...");

        // Add default download queue
        if (addDefaultDownloadQueue)
            await AddDefaultDownloadQueueAsync();

        // Get all download queues from database
        var downloadQueues = await _unitOfWork
            .DownloadQueueRepository
            .GetAllAsync();

        Log.Debug("Retrieved {QueueCount} download queues from database", downloadQueues.Count);

        // Find deleted download queues
        var deletedDownloadQueues = DownloadQueues
            .Where(vm => !downloadQueues.Exists(dq => dq.Id == vm.Id))
            .ToList();

        Log.Debug("Found {DeletedCount} download queues to remove from service", deletedDownloadQueues.Count);

        // Remove download queues from list
        foreach (var downloadQueue in deletedDownloadQueues)
        {
            DownloadQueues.Remove(downloadQueue);
            Log.Debug("Removed download queue with ID {QueueId} from service", downloadQueue.Id);
        }

        // Find added download queues
        var addedDownloadQueues = downloadQueues
            .Where(dq => DownloadQueues.All(vm => vm.Id != dq.Id))
            .Select(dq => _mapper.Map<DownloadQueueViewModel>(dq))
            .ToList();

        Log.Debug("Found {AddedCount} new download queues to add to service", addedDownloadQueues.Count);

        // Add new download queues to list
        foreach (var downloadQueue in addedDownloadQueues)
        {
            DownloadQueues.Add(downloadQueue);
            Log.Debug("Added new download queue with ID {QueueId} to service", downloadQueue.Id);
        }

        // Notify download queues changed
        OnPropertyChanged(nameof(DownloadQueues));
        // Raise changed event
        DataChanged?.Invoke(this, EventArgs.Empty);
        // Log information
        Log.Information("Download queues loaded successfully. Total queues: {QueueCount}", DownloadQueues.Count);
    }

    public async Task<int> AddNewDownloadQueueAsync(DownloadQueue? downloadQueue, bool reloadData = true)
    {
        if (downloadQueue is not { Id: 0 })
        {
            Log.Warning("Attempted to add invalid download queue. Model is null or has existing ID");
            return 0;
        }

        Log.Information("Adding new download queue: {QueueTitle}", downloadQueue.Title);

        await _unitOfWork.DownloadQueueRepository.AddAsync(downloadQueue);
        await _unitOfWork.SaveAsync();

        Log.Debug("New download queue added to database with ID: {QueueId}", downloadQueue.Id);

        if (reloadData)
        {
            Log.Debug("Reloading download queues after adding new queue");
            await LoadDownloadQueuesAsync();
        }

        Log.Information("Download queue added successfully with ID: {QueueId}", downloadQueue.Id);
        return downloadQueue.Id;
    }

    public async Task DeleteDownloadQueueAsync(DownloadQueueViewModel? viewModel, bool reloadData = true)
    {
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == viewModel?.Id);
        if (downloadQueue == null)
        {
            Log.Warning("Attempted to delete download queue with null or invalid ID");
            return;
        }

        Log.Information("Deleting download queue with ID: {QueueId}, Title: {QueueTitle}", downloadQueue.Id, downloadQueue.Title);

        var downloadQueueInDb = await _unitOfWork
            .DownloadQueueRepository
            .GetAsync(dq => dq.Id == downloadQueue.Id);

        if (downloadQueueInDb == null)
        {
            Log.Warning("Download queue with ID {QueueId} not found in database", downloadQueue.Id);
            return;
        }

        var downloadFiles = _downloadFileService
            .DownloadFiles
            .Where(df => df.DownloadQueueId == downloadQueueInDb.Id)
            .ToList();

        Log.Debug("Found {FileCount} download files in queue ID {QueueId} to remove", downloadFiles.Count, downloadQueue.Id);
        await RemoveDownloadFilesFromDownloadQueueAsync(downloadQueue, downloadFiles);

        await _unitOfWork.DownloadQueueRepository.DeleteAsync(downloadQueueInDb);
        await _unitOfWork.SaveAsync();

        Log.Debug("Download queue with ID {QueueId} deleted from database", downloadQueue.Id);

        if (reloadData)
        {
            Log.Debug("Reloading download queues after deletion");
            await LoadDownloadQueuesAsync();
        }

        Log.Information("Download queue with ID {QueueId} deleted successfully", downloadQueue.Id);
    }

    public async Task UpdateDownloadQueueAsync(DownloadQueue? downloadQueue, bool reloadData = true)
    {
        if (downloadQueue == null)
        {
            Log.Warning("Attempted to update null download queue model");
            return;
        }

        Log.Information("Updating download queue with ID: {QueueId}", downloadQueue.Id);

        await _unitOfWork.DownloadQueueRepository.UpdateAsync(downloadQueue);
        await _unitOfWork.SaveAsync();

        Log.Debug("Download queue with ID {QueueId} updated in database", downloadQueue.Id);

        if (reloadData)
        {
            Log.Debug("Reloading download queues after update");
            await LoadDownloadQueuesAsync();
        }

        Log.Information("Download queue with ID {QueueId} updated successfully", downloadQueue.Id);
    }

    public async Task UpdateDownloadQueueAsync(DownloadQueueViewModel? viewModel, bool reloadData = true)
    {
        if (viewModel == null)
        {
            Log.Warning("Attempted to update null download queue view model");
            return;
        }

        var downloadQueue = _mapper.Map<DownloadQueue>(viewModel);
        await UpdateDownloadQueueAsync(downloadQueue, reloadData);
    }

    public async Task UpdateDownloadQueuesAsync(List<DownloadQueue>? downloadQueues, bool reloadData = true)
    {
        if (downloadQueues == null || downloadQueues.Count == 0)
        {
            Log.Warning("Attempted to update empty or null download queues list");
            return;
        }

        Log.Information("Updating {QueueCount} download queues in batch", downloadQueues.Count);

        await _unitOfWork.DownloadQueueRepository.UpdateAllAsync(downloadQueues);
        await _unitOfWork.SaveAsync();

        Log.Debug("Batch update completed for {QueueCount} download queues", downloadQueues.Count);

        if (reloadData)
        {
            Log.Debug("Reloading download queues after batch update");
            await LoadDownloadQueuesAsync();
        }

        Log.Information("Successfully updated {QueueCount} download queues", downloadQueues.Count);
    }

    public async Task UpdateDownloadQueuesAsync(List<DownloadQueueViewModel>? viewModels, bool reloadData = true)
    {
        if (viewModels == null || viewModels.Count == 0)
        {
            Log.Warning("Attempted to update empty or null download queue view models list");
            return;
        }

        var downloadQueues = _mapper.Map<List<DownloadQueue>>(viewModels);
        await UpdateDownloadQueuesAsync(downloadQueues, reloadData);
    }

    public async Task StartDownloadQueueAsync(DownloadQueueViewModel? viewModel)
    {
        // Try to find the download queue
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == viewModel?.Id);
        if (downloadQueue == null || downloadQueue.IsRunning)
        {
            Log.Warning("Attempted to start invalid or already running download queue ID: {QueueId}", viewModel?.Id);
            return;
        }

        Log.Information("Starting download queue ID: {QueueId}, Title: {QueueTitle}", downloadQueue.Id, downloadQueue.Title);

        // Start the download queue
        await ContinueDownloadQueueAsync(downloadQueue);

        Log.Information("Download queue ID: {QueueId} started successfully", downloadQueue.Id);
    }

    public async Task StopDownloadQueueAsync(DownloadQueueViewModel? viewModel, bool playSound = true)
    {
        // Try to find the download queue
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == viewModel?.Id);
        if (downloadQueue == null)
        {
            Log.Warning("Attempted to stop download queue with null or invalid ID: {QueueId}", viewModel?.Id);
            return;
        }

        Log.Information("Stopping download queue ID: {QueueId}, Title: {QueueTitle}", downloadQueue.Id, downloadQueue.Title);

        // Set the IsRunning flag to false
        downloadQueue.IsRunning = false;
        // Set the IsStartSoundPlayed flag to false
        downloadQueue.IsStartSoundPlayed = false;

        // Get all download file belongs to the current download queue and currently downloading or paused
        var downloadFiles = _downloadFileService
            .DownloadFiles
            .Where(df => df.DownloadQueueId == downloadQueue.Id && (df.IsDownloading || df.IsPaused) && !df.IsStopping)
            .ToList();

        Log.Debug("Stopping {FileCount} active download files in queue ID {QueueId}", downloadFiles.Count, downloadQueue.Id);

        // Reset the count of error for each download file when the download is completed.
        foreach (var downloadFile in downloadFiles)
            _downloadFileService.AddCompletedTask(downloadFile, ResetCountOfError);

        var finishedTasks = downloadFiles.ConvertAll(df => _downloadFileService.StopDownloadFileAsync(df, ensureStopped: true, playSound: false));
        await Task.WhenAll(finishedTasks);

        // Turn off computer
        if (downloadQueue.TurnOffComputerWhenDone)
        {
            Log.Information("Queue ID {QueueId} configured to turn off computer after completion", downloadQueue.Id);
            TurnOffComputer(downloadQueue);
            return;
        }

        // Exit program
        if (downloadQueue.ExitProgramWhenDone)
        {
            Log.Information("Queue ID {QueueId} configured to exit program after completion", downloadQueue.Id);
            ExitProgram();
            return;
        }

        // Play the queue stopped sound if possible
        if (playSound && _settingsService.Settings.UseQueueStoppedSound)
        {
            Log.Debug("Playing queue stopped sound for queue ID {QueueId}", downloadQueue.Id);
            _ = AudioManager.PlayAsync(AppNotificationType.QueueStopped);
        }

        Log.Information("Download queue ID: {QueueId} stopped successfully", downloadQueue.Id);
    }

    public async Task AddDownloadFileToDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel,
        DownloadFileViewModel? downloadFileViewModel)
    {
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == downloadQueueViewModel?.Id);
        var downloadFile = _downloadFileService.DownloadFiles.FirstOrDefault(df => df.Id == downloadFileViewModel?.Id);
        if (downloadQueue == null || downloadFile == null || downloadFile.IsCompleted)
        {
            Log.Warning("Attempted to add invalid or completed download file to queue");
            return;
        }

        // The download file is already in the queue and there is no need to add it again.
        if (downloadFile.DownloadQueueId == downloadQueue.Id)
        {
            Log.Debug("Download file ID {FileId} already in queue ID {QueueId}", downloadFile.Id, downloadQueue.Id);
            return;
        }

        Log.Information("Adding download file ID {FileId} to queue ID {QueueId}", downloadFile.Id, downloadQueue.Id);

        downloadFile.DownloadQueueId = downloadQueue.Id;
        downloadFile.DownloadQueuePriority = (_downloadFileService
            .DownloadFiles
            .Where(df => df.DownloadQueueId == downloadQueue.Id)
            .Max(df => df.DownloadQueuePriority) ?? 0) + 1;

        await _downloadFileService.UpdateDownloadFileAsync(downloadFile);
        await LoadDownloadQueuesAsync(addDefaultDownloadQueue: false);

        Log.Information("Download file ID {FileId} added to queue ID {QueueId}", downloadFile.Id, downloadQueue.Id);
    }

    public async Task AddDownloadFilesToDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel,
        List<DownloadFileViewModel>? downloadFilesViewModels)
    {
        if (downloadFilesViewModels == null || downloadFilesViewModels.Count == 0)
        {
            Log.Warning("Attempted to add null or empty download files list to queue");
            return;
        }

        var primaryKeys = downloadFilesViewModels
            .Select(df => df.Id)
            .Distinct()
            .ToList();

        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == downloadQueueViewModel?.Id);
        if (downloadQueue == null)
        {
            Log.Warning("Target download queue not found for adding multiple files");
            return;
        }

        Log.Information("Adding {FileCount} download files to queue ID {QueueId}", primaryKeys.Count, downloadQueue.Id);

        var downloadFiles = primaryKeys
            .Select(pk => _downloadFileService.DownloadFiles.FirstOrDefault(df => df.Id == pk && !df.IsCompleted && df.DownloadQueueId != downloadQueue.Id))
            .OfType<DownloadFileViewModel>()
            .ToList();

        if (downloadFiles.Count == 0)
        {
            Log.Debug("No valid download files to add after filtering");
            return;
        }

        var maxDownloadQueuePriority = (_downloadFileService
            .DownloadFiles
            .Where(df => df.DownloadQueueId == downloadQueue.Id)
            .Max(df => df.DownloadQueuePriority) ?? 0) + 1;

        foreach (var downloadFile in downloadFiles)
        {
            downloadFile.DownloadQueueId = downloadQueue.Id;
            downloadFile.DownloadQueuePriority = maxDownloadQueuePriority;
            maxDownloadQueuePriority++;
        }

        await _downloadFileService.UpdateDownloadFilesAsync(downloadFiles);
        await LoadDownloadQueuesAsync(addDefaultDownloadQueue: false);

        Log.Information("Successfully added {FileCount} download files to queue ID {QueueId}", downloadFiles.Count, downloadQueue.Id);
    }

    public async Task RemoveDownloadFileFromDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel,
        DownloadFileViewModel? downloadFileViewModel)
    {
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == downloadQueueViewModel?.Id);
        var downloadFile = _downloadFileService.DownloadFiles.FirstOrDefault(df => df.Id == downloadFileViewModel?.Id);
        if (downloadQueue == null || downloadFile == null || downloadFile.IsDownloading || downloadFile.IsPaused)
        {
            Log.Warning("Attempted to remove invalid or active download file from queue");
            return;
        }

        // Make sure the file is in the queue.
        if (downloadFile.DownloadQueueId != downloadQueue.Id)
        {
            Log.Debug("Download file ID {FileId} not in queue ID {QueueId}", downloadFile.Id, downloadQueue.Id);
            return;
        }

        Log.Information("Removing download file ID {FileId} from queue ID {QueueId}", downloadFile.Id, downloadQueue.Id);

        downloadFile.DownloadQueueId = null;
        downloadFile.DownloadQueueName = null;
        downloadFile.DownloadQueuePriority = null;

        await _downloadFileService.UpdateDownloadFileAsync(downloadFile);
        await LoadDownloadQueuesAsync(addDefaultDownloadQueue: false);

        Log.Information("Download file ID {FileId} removed from queue ID {QueueId}", downloadFile.Id, downloadQueue.Id);
    }

    public async Task RemoveDownloadFilesFromDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel,
        List<DownloadFileViewModel> downloadFileViewModels)
    {
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == downloadQueueViewModel?.Id);
        if (downloadQueue == null || downloadFileViewModels.Count == 0)
        {
            Log.Warning("Attempted to remove files from null queue or empty list");
            return;
        }

        Log.Information("Removing {FileCount} download files from queue ID {QueueId}", downloadFileViewModels.Count, downloadQueue.Id);

        var downloadFiles = _downloadFileService
            .DownloadFiles
            .Where(df => downloadFileViewModels.Exists(vm => vm.Id == df.Id)
                         && df is { IsDownloading: false, IsPaused: false }
                         && df.DownloadQueueId == downloadQueue.Id)
            .ToList();

        if (downloadFiles.Count == 0)
        {
            Log.Debug("No valid files to remove after filtering");
            return;
        }

        foreach (var downloadFile in downloadFiles)
        {
            downloadFile.DownloadQueueId = null;
            downloadFile.DownloadQueueName = null;
            downloadFile.DownloadQueuePriority = null;
        }

        await _downloadFileService.UpdateDownloadFilesAsync(downloadFiles);
        await LoadDownloadQueuesAsync(addDefaultDownloadQueue: false);

        Log.Information("Successfully removed {FileCount} download files from queue ID {QueueId}", downloadFiles.Count, downloadQueue.Id);
    }

    public async Task ChangeDefaultDownloadQueueAsync(DownloadQueueViewModel? viewModel, bool reloadData = true)
    {
        // Make sure given download queue is not null
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == viewModel?.Id);
        if (downloadQueue == null)
        {
            Log.Warning("Attempted to set default queue with null or invalid ID");
            return;
        }

        Log.Information("Setting download queue ID {QueueId} as default", downloadQueue.Id);

        // Get all download queues that are set as default
        var viewModels = DownloadQueues
            .Where(dq => dq.IsDefault)
            .ToList();

        // Download queue already set as default and no need to set it again
        if (viewModels.Count == 1 && viewModels[0].Id == downloadQueue.Id)
        {
            Log.Debug("Queue ID {QueueId} already set as default", downloadQueue.Id);
            return;
        }

        // Unset all download queues that are set as default
        viewModels = viewModels
            .Select(dq =>
            {
                dq.IsDefault = false;
                return dq;
            })
            .ToList();

        // Update all download queues
        await UpdateDownloadQueuesAsync(viewModels, reloadData);

        // Set given download queue as default and update it
        downloadQueue.IsDefault = true;
        await UpdateDownloadQueueAsync(downloadQueue, reloadData);

        Log.Information("Download queue ID {QueueId} set as default successfully", downloadQueue.Id);
    }

    public async Task ChangeLastSelectedDownloadQueueAsync(DownloadQueueViewModel? viewModel, bool reloadData = true)
    {
        // Make sure given download queue is not null
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == viewModel?.Id);
        if (downloadQueue == null)
        {
            Log.Warning("Attempted to set last selected queue with null or invalid ID");
            return;
        }

        Log.Information("Setting download queue ID {QueueId} as last selected", downloadQueue.Id);

        // Get all download queues that are set as last choice
        var viewModels = DownloadQueues
            .Where(dq => dq.IsLastChoice)
            .ToList();

        // Download queue already set as last choice and no need to set it again
        if (viewModels.Count == 1 && viewModels[0].Id == downloadQueue.Id)
        {
            Log.Debug("Queue ID {QueueId} already set as last selected", downloadQueue.Id);
            return;
        }

        // Unset all download queues that are set as last choice
        viewModels = viewModels
            .Select(dq =>
            {
                dq.IsLastChoice = false;
                return dq;
            })
            .ToList();

        // Update all download queues
        await UpdateDownloadQueuesAsync(viewModels, reloadData);

        // Set given download queue as last choice and update it
        downloadQueue.IsLastChoice = true;
        await UpdateDownloadQueueAsync(downloadQueue, reloadData);

        Log.Information("Download queue ID {QueueId} set as last selected successfully", downloadQueue.Id);
    }

    public void StartScheduleManagerTimer()
    {
        Log.Debug("Starting schedule manager timer");
        _scheduleManagerTimer.Start();
    }

    #region Event handlers

    /// <summary>
    /// Handles the <see cref="IDownloadFileService.DataChanged"/> event of the download service.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    private void DownloadFileServiceOnDataChanged(object? sender, EventArgs e)
    {
        Log.Debug("Download file service data changed. Synchronizing download queues.");

        // Get all primary keys of download files
        var primaryKeys = _downloadFileService
            .DownloadFiles
            .Select(df => df.Id)
            .ToList();

        // Find download queues that has a download file that is not in the given list of primary keys
        var downloadQueues = DownloadQueues
            .Where(dq => dq.DownloadingFiles.Exists(df => !primaryKeys.Contains(df.Id)))
            .ToList();

        Log.Debug("Found {QueueCount} download queues with orphaned download files", downloadQueues.Count);

        // Remove download files from download queues that are not in the given list of primary keys
        foreach (var downloadQueue in downloadQueues)
        {
            var downloadFiles = downloadQueue
                .DownloadingFiles
                .Where(df => !primaryKeys.Contains(df.Id))
                .ToList();

            Log.Debug("Removing {FileCount} orphaned files from queue ID {QueueId}", downloadFiles.Count, downloadQueue.Id);

            foreach (var downloadFile in downloadFiles)
                downloadQueue.DownloadingFiles.Remove(downloadFile);

            if (downloadQueue.IsRunning)
                _ = ContinueDownloadQueueAsync(downloadQueue);
        }
    }

    /// <summary>
    /// Handles the <see cref="DownloadFileViewModel.DownloadPaused"/> event of the download file.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="DownloadFileEventArgs"/> instance containing the event data.</param>
    private void DownloadFileOnDownloadPaused(object? sender, DownloadFileEventArgs e)
    {
        Log.Debug("Download file ID {FileId} paused. Checking queue behavior.", e.Id);

        // Try to find download file by id
        var downloadFile = _downloadFileService.DownloadFiles.FirstOrDefault(df => df.Id == e.Id);
        // Try to find download queue by download queue id
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == downloadFile?.DownloadQueueId);
        // Check if the download queue supports paused files and currently is running
        if (downloadQueue == null || downloadQueue.IncludePausedFiles || !downloadQueue.IsRunning)
            return;

        Log.Debug("Queue ID {QueueId} does not include paused files. Continuing queue.", downloadQueue.Id);

        // If the download file is paused and the download queue is not supporting paused files,
        // Ignore paused download files and continue the download queue with the other download files
        _ = ContinueDownloadQueueAsync(downloadQueue);
    }

    /// <summary>
    /// Handle the <see cref="DispatcherTimer.Tick"/> event and manage scheduled download queues.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    private void ScheduleManagerTimerOnTick(object? sender, EventArgs e)
    {
        try
        {
            Log.Debug("Schedule manager timer tick. Processing scheduled queues.");

            // Stop timer until operation is finished
            _scheduleManagerTimer.Stop();

            // Get all download queues that are scheduled to start
            var startScheduledQueues = DownloadQueues
                .Where(dq => dq is { StartDownloadSchedule: not null, IsScheduleEnabled: false })
                .ToList();

            Log.Debug("Found {StartCount} queues scheduled to start", startScheduledQueues.Count);

            // Start download queue based on it's a daily schedule or just for date schedule
            foreach (var downloadQueue in startScheduledQueues)
                _ = downloadQueue.IsDaily ? StartDailyScheduleAsync(downloadQueue) : StartJustForDateScheduleAsync(downloadQueue);

            // Get all download queues that are scheduled to stop
            var stopScheduledQueues = DownloadQueues
                .Where(dq => dq.StopDownloadSchedule != null && (dq.IsScheduleEnabled || dq.IsRunning))
                .ToList();

            Log.Debug("Found {StopCount} queues scheduled to stop", stopScheduledQueues.Count);

            // Stop download queue based on it's a daily schedule or just for date schedule
            foreach (var downloadQueue in stopScheduledQueues)
                _ = downloadQueue.IsDaily ? StopDailyScheduleAsync(downloadQueue) : StopJustForDateScheduleAsync(downloadQueue);

            // Restart the timer to check for scheduled download queues again
            _scheduleManagerTimer.Start();

            Log.Debug("Schedule manager tick completed successfully.");
        }
        catch (Exception ex)
        {
            // Log the error
            Log.Error(ex, "An error occurred while scheduling manager. Error message: {ErrorMessage}", ex.Message);
            // Start the timer again if it's not enabled
            if (!_scheduleManagerTimer.IsEnabled)
                _scheduleManagerTimer.Start();
        }
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Adds the default download queue (Main Queue) to the database.
    /// </summary>
    private async Task AddDefaultDownloadQueueAsync()
    {
        Log.Debug("Checking for default download queue existence...");

        // Get the default download queue from the database
        var downloadQueueInDb = await _unitOfWork
            .DownloadQueueRepository
            .GetAsync(where: dq => dq.Title.ToLower() == Constants.DefaultDownloadQueueTitle.ToLower());

        // Check if the default download queue exists
        if (downloadQueueInDb != null)
        {
            Log.Debug("Default download queue already exists in database.");
            return;
        }

        Log.Information("Creating default download queue: {DefaultTitle}", Constants.DefaultDownloadQueueTitle);

        // Create default download queue
        var downloadQueue = new DownloadQueue
        {
            Title = Constants.DefaultDownloadQueueTitle,
            RetryOnDownloadingFailed = true,
            RetryCount = 3,
            ShowAlarmWhenDone = true,
            DownloadCountAtSameTime = 2,
            IncludePausedFiles = false,
        };

        // Add default download queue to the database
        await AddNewDownloadQueueAsync(downloadQueue, reloadData: false);

        Log.Information("Default download queue created successfully.");
    }

    /// <summary>
    /// Starts or continues the process of a download queue.
    /// </summary>
    /// <param name="viewModel">The download queue that should be started or continued.</param>
    private async Task ContinueDownloadQueueAsync(DownloadQueueViewModel viewModel)
    {
        Log.Debug("Continuing download queue ID {QueueId}", viewModel.Id);

        // Get download files that currently are not downloading or paused
        // And satisfy the following conditions
        var downloadFiles = _downloadFileService
            .DownloadFiles
            .Where(df => df.DownloadQueueId == viewModel.Id
                         && df is { IsDownloading: false, IsPaused: false, IsCompleted: false, IsStopping: false }
                         && (!viewModel.RetryOnDownloadingFailed || df.CountOfError < viewModel.RetryCount))
            .ToList();

        // Check if the download queue supports paused files
        if (viewModel.IncludePausedFiles)
        {
            // Get download files that are paused
            var pausedDownloadFiles = _downloadFileService
                .DownloadFiles
                .Where(df => df.DownloadQueueId == viewModel.Id
                             && df is { IsPaused: true, IsStopping: false }
                             && (!viewModel.RetryOnDownloadingFailed || df.CountOfError < viewModel.RetryCount))
                .ToList();

            // Add them to the download files list
            downloadFiles = downloadFiles
                .Union(pausedDownloadFiles)
                .Distinct()
                .ToList();

            Log.Debug("Included {PausedCount} paused files in queue ID {QueueId}", pausedDownloadFiles.Count, viewModel.Id);
        }

        // Sort download files by download queue priority
        downloadFiles = downloadFiles.OrderBy(df => df.DownloadQueuePriority).ToList();

        // All download files are finished
        if (downloadFiles.Count == 0 && viewModel.DownloadingFiles.Count == 0)
        {
            Log.Information("All files in queue ID {QueueId} are completed", viewModel.Id);

            // Check for play finished sound
            var playFinishedSound = viewModel.IsStartSoundPlayed && _settingsService.Settings.UseQueueFinishedSound;
            // Play sound when possible
            if (playFinishedSound || viewModel.ShowAlarmWhenDone)
            {
                Log.Debug("Playing queue finished sound for queue ID {QueueId}", viewModel.Id);
                _ = AudioManager.PlayAsync(AppNotificationType.QueueFinished);
            }

            // Stop download queue
            await StopDownloadQueueAsync(viewModel, playSound: !playFinishedSound);
            return;
        }

        // Set the IsRunning flag to true
        if (!viewModel.IsRunning)
        {
            viewModel.IsRunning = true;
            Log.Debug("Queue ID {QueueId} marked as running", viewModel.Id);
        }

        // Define the index variable
        var index = 0;
        // Start downloading files when the number of downloading files is less than the download count at the same time,
        // And there is a download file in the download files list
        while (viewModel.DownloadingFiles.Count < viewModel.DownloadCountAtSameTime && index < downloadFiles.Count)
        {
            // Get download file by index
            var downloadFile = downloadFiles[index];
            // Set is running in queue flag to true
            downloadFile.IsRunningInQueue = true;
            // Add completed tasks for the current download file
            _downloadFileService.AddCompletedTask(downloadFile, DownloadFileFinishedTaskAsync);
            // Subscribe to DownloadPaused event for managing paused files
            downloadFile.DownloadPaused += DownloadFileOnDownloadPaused;
            // Start download file
            _ = _downloadFileService.StartDownloadFileAsync(downloadFile, showWindow: false);
            // Add download file to the downloading files list of the download queue
            viewModel.DownloadingFiles.Add(downloadFile);

            Log.Debug("Started download file ID {FileId} in queue ID {QueueId}", downloadFile.Id, viewModel.Id);

            index++;
        }

        // Stop download queue when there is no download file in the downloading files list
        if (viewModel.DownloadingFiles.Count == 0)
        {
            Log.Debug("No active downloads in queue ID {QueueId}. Stopping queue.", viewModel.Id);
            await StopDownloadQueueAsync(viewModel);
            return;
        }

        // Play download queue started sound if possible
        if (!viewModel.IsStartSoundPlayed && _settingsService.Settings.UseQueueStartedSound)
        {
            Log.Debug("Playing queue started sound for queue ID {QueueId}", viewModel.Id);
            _ = AudioManager.PlayAsync(AppNotificationType.QueueStarted);
            viewModel.IsStartSoundPlayed = true;
        }
    }

    /// <summary>
    /// A task that runs when the downloading of a file is completed successfully or with an error.
    /// </summary>
    /// <param name="viewModel">The download file that the task is running for.</param>
    private async Task DownloadFileFinishedTaskAsync(DownloadFileViewModel? viewModel)
    {
        Log.Debug("Download file ID {FileId} finished. Processing completion.", viewModel?.Id);

        // Try to find the download queue by TempDownloadQueueId
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == viewModel?.TempDownloadQueueId);
        // Try to find the download file in the download queue
        var downloadFile = downloadQueue?.DownloadingFiles.Find(df => df.Id == viewModel?.Id);
        if (downloadFile == null)
        {
            Log.Warning("Download file ID {FileId} not found in queue during completion", viewModel?.Id);
            return;
        }

        // Unsubscribe from DownloadPaused event
        downloadFile.DownloadPaused -= DownloadFileOnDownloadPaused;

        // Check if an error occurred during the download or the download stopped by the user when the download queue is still running.
        // In this situation we have to change the priority to the lowest (move to end of queue).
        if ((downloadFile.IsError || downloadFile.IsStopped) && downloadQueue!.IsRunning)
        {
            Log.Debug("File ID {FileId} failed or stopped. Moving to end of queue.", downloadFile.Id);

            // Get the maximum priority for the current queue.
            var maxPriority = _downloadFileService
                .DownloadFiles
                .Where(df => df.DownloadQueueId == downloadQueue.Id)
                .Max(df => df.DownloadQueuePriority);

            // Change the priority of the download file
            downloadFile.DownloadQueuePriority = maxPriority + 1;

            // Check if an error occurred during the download
            if (downloadFile.IsError)
                downloadFile.CountOfError++;
        }

        // Remove the download file from the downloading files collection
        if (downloadFile.IsCompleted || downloadFile.IsError || downloadFile.IsStopped)
        {
            downloadQueue!.DownloadingFiles.Remove(downloadFile);
            Log.Debug("Removed file ID {FileId} from active list in queue ID {QueueId}", downloadFile.Id, downloadQueue.Id);
        }

        // Update the download file
        await _downloadFileService.UpdateDownloadFileAsync(downloadFile);

        // Check if the download queue still running
        if (downloadQueue!.IsRunning)
        {
            Log.Debug("Queue ID {QueueId} still running. Continuing with next files.", downloadQueue.Id);
            _ = ContinueDownloadQueueAsync(downloadQueue);
        }
    }

    /// <summary>
    /// Starts a download queue that scheduled to run daily.
    /// </summary>
    /// <param name="downloadQueue">The download queue that scheduled to run daily.</param>
    private async Task StartDailyScheduleAsync(DownloadQueueViewModel downloadQueue)
    {
        Log.Debug("Processing daily start schedule for queue ID {QueueId}", downloadQueue.Id);

        // Check if the current day of week is acceptable for the download queue
        if (!CheckTheDayIsAcceptable(downloadQueue))
        {
            Log.Debug("Today is not an acceptable day for daily schedule of queue ID {QueueId}", downloadQueue.Id);
            return;
        }

        // Start the schedule
        await StartScheduleAsync(downloadQueue);
    }

    /// <summary>
    /// Starts a download queue that scheduled to run in a specific date.
    /// </summary>
    /// <param name="downloadQueue">The download queue that scheduled to run in a specific date.</param>
    private async Task StartJustForDateScheduleAsync(DownloadQueueViewModel downloadQueue)
    {
        Log.Debug("Processing one-time start schedule for queue ID {QueueId}", downloadQueue.Id);

        // Compare current date with just for date
        if (downloadQueue.JustForDate?.Date.Equals(DateTime.Now.Date) != true)
        {
            Log.Debug("Today is not the scheduled date for queue ID {QueueId}", downloadQueue.Id);
            return;
        }

        // Start the schedule
        await StartScheduleAsync(downloadQueue);
    }

    /// <summary>
    /// Starts the schedule.
    /// </summary>
    /// <param name="downloadQueue">The download queue that scheduled.</param>
    private async Task StartScheduleAsync(DownloadQueueViewModel downloadQueue)
    {
        Log.Debug("Attempting to start scheduled queue ID {QueueId} at {Time}", downloadQueue.Id, DateTime.Now.ToString("HH:mm:ss"));

        // Make sure start time is equal to current time
        var startDownloadHour = downloadQueue.StartDownloadSchedule!.Value.Hours;
        var startDownloadMinute = downloadQueue.StartDownloadSchedule!.Value.Minutes;

        // Compare current time with start time
        if (startDownloadHour != DateTime.Now.Hour || startDownloadMinute != DateTime.Now.Minute)
        {
            Log.Debug("Current time does not match start time for queue ID {QueueId}", downloadQueue.Id);
            return;
        }

        // Get all download files that belong to the download queue
        var downloadFiles = _downloadFileService
            .DownloadFiles
            .Where(df => df.DownloadQueueId == downloadQueue.Id)
            .ToList();

        // Check if download files collection is empty
        if (downloadFiles.Count == 0)
        {
            Log.Debug("No files in queue ID {QueueId} to start schedule", downloadQueue.Id);
            return;
        }

        // Set the IsScheduleEnabled flag to true
        downloadQueue.IsScheduleEnabled = true;
        Log.Debug("Schedule enabled for queue ID {QueueId}", downloadQueue.Id);

        // Start download queue
        await StartDownloadQueueAsync(downloadQueue);

        Log.Information("Scheduled start executed for queue ID {QueueId}", downloadQueue.Id);
    }

    /// <summary>
    /// Stops a download queue that scheduled to run daily.
    /// </summary>
    /// <param name="downloadQueue">The download queue that scheduled to run daily.</param>
    private async Task StopDailyScheduleAsync(DownloadQueueViewModel downloadQueue)
    {
        Log.Debug("Processing daily stop schedule for queue ID {QueueId}", downloadQueue.Id);

        // Check if the current day of week is acceptable for the download queue
        if (!CheckTheDayIsAcceptable(downloadQueue))
        {
            Log.Debug("Today is not an acceptable day for daily stop of queue ID {QueueId}", downloadQueue.Id);
            return;
        }

        // Stop the schedule
        await StopScheduleAsync(downloadQueue);
    }

    /// <summary>
    /// Stops a download queue that scheduled to run in a specific date.
    /// </summary>
    /// <param name="downloadQueue">The download queue that scheduled to run in a specific date.</param>
    private async Task StopJustForDateScheduleAsync(DownloadQueueViewModel downloadQueue)
    {
        Log.Debug("Processing one-time stop schedule for queue ID {QueueId}", downloadQueue.Id);

        // Compare current date with just for date
        if (downloadQueue.JustForDate?.Date.Equals(DateTime.Now.Date) != true)
        {
            Log.Debug("Today is not the scheduled stop date for queue ID {QueueId}", downloadQueue.Id);
            return;
        }

        // Stop the schedule
        await StopScheduleAsync(downloadQueue);
    }

    /// <summary>
    /// Stops the schedule.
    /// </summary>
    /// <param name="downloadQueue">The download queue that scheduled.</param>
    private async Task StopScheduleAsync(DownloadQueueViewModel downloadQueue)
    {
        Log.Debug("Attempting to stop scheduled queue ID {QueueId} at {Time}", downloadQueue.Id, DateTime.Now.ToString("HH:mm:ss"));

        // Make sure stop time is equal to current time
        var stopDownloadHour = downloadQueue.StopDownloadSchedule!.Value.Hours;
        var stopDownloadMinute = downloadQueue.StopDownloadSchedule!.Value.Minutes;

        // Compare current time with stop time
        if (DateTime.Now.Hour != stopDownloadHour || DateTime.Now.Minute != stopDownloadMinute)
        {
            Log.Debug("Current time does not match stop time for queue ID {QueueId}", downloadQueue.Id);
            return;
        }

        // Set the IsScheduleEnabled flag to false
        downloadQueue.IsScheduleEnabled = false;
        Log.Debug("Schedule disabled for queue ID {QueueId}", downloadQueue.Id);

        // Stop download queue
        await StopDownloadQueueAsync(downloadQueue);

        Log.Information("Scheduled stop executed for queue ID {QueueId}", downloadQueue.Id);
    }

    /// <summary>
    /// Checks that the current day is an acceptable day for the schedule.
    /// </summary>
    /// <param name="downloadQueue">The download queue that scheduled to run daily.</param>
    /// <returns>Returns true if the current day is an acceptable day for the schedule, otherwise returns false.</returns>
    private static bool CheckTheDayIsAcceptable(DownloadQueueViewModel downloadQueue)
    {
        Log.Debug("Validating day of week for daily schedule in queue ID {QueueId}", downloadQueue.Id);

        // Get days of week that sets for the download queue
        var daysOfWeek = downloadQueue.DaysOfWeek;
        // Make sure the days of week is not empty
        if (daysOfWeek.IsStringNullOrEmpty())
        {
            Log.Debug("No days of week configured for queue ID {QueueId}", downloadQueue.Id);
            return false;
        }

        // Convert days of week from json to DaysOfWeekViewModel
        var daysOfWeekViewModel = daysOfWeek.ConvertFromJson<DaysOfWeekViewModel?>();
        // Check if view model is not null
        if (daysOfWeekViewModel == null)
        {
            Log.Debug("Failed to deserialize days of week for queue ID {QueueId}", downloadQueue.Id);
            return false;
        }

        // Get current day of week
        var currentDayOfWeek = DateTime.Now.DayOfWeek;
        // Define a variable that indicates whether the current day of week is acceptable for the download queue or not
        bool dayOfWeekAcceptable;
        switch (currentDayOfWeek)
        {
            case DayOfWeek.Saturday when daysOfWeekViewModel.Saturday:
            case DayOfWeek.Sunday when daysOfWeekViewModel.Sunday:
            case DayOfWeek.Monday when daysOfWeekViewModel.Monday:
            case DayOfWeek.Tuesday when daysOfWeekViewModel.Tuesday:
            case DayOfWeek.Wednesday when daysOfWeekViewModel.Wednesday:
            case DayOfWeek.Thursday when daysOfWeekViewModel.Thursday:
            case DayOfWeek.Friday when daysOfWeekViewModel.Friday:
            {
                dayOfWeekAcceptable = true;
                break;
            }

            default:
            {
                dayOfWeekAcceptable = false;
                break;
            }
        }

        Log.Debug("Day {Day} is acceptable: {IsAcceptable} for queue ID {QueueId}", currentDayOfWeek, dayOfWeekAcceptable, downloadQueue.Id);
        return dayOfWeekAcceptable;
    }

    /// <summary>
    /// Resets the count of error of the download file.
    /// This method should run when the download file is completed.
    /// </summary>
    /// <param name="downloadFile">The download file that is completed.</param>
    private static void ResetCountOfError(DownloadFileViewModel? downloadFile)
    {
        // Check if the download file is not null
        ArgumentNullException.ThrowIfNull(downloadFile);
        // Reset the count of error
        downloadFile.CountOfError = 0;

        Log.Debug("Reset error count for download file ID {FileId}", downloadFile.Id);
    }

    /// <summary>
    /// Shows the power off window and turn off the computer.
    /// </summary>
    /// <param name="downloadQueue">The download queue that stopped and should turn off the computer.</param>
    /// <exception cref="InvalidOperationException">if the mode of the turn-off computer is not valid.</exception>
    private static void TurnOffComputer(DownloadQueueViewModel downloadQueue)
    {
        Log.Information("Initiating computer shutdown for queue ID {QueueId}", downloadQueue.Id);

        // Check if the mode of the turn-off computer is valid 
        if (downloadQueue.TurnOffComputerMode == null)
        {
            Log.Warning("Turn off computer mode is null for queue ID {QueueId}", downloadQueue.Id);
            return;
        }

        // Convert the turn-off computer mode to string
        var turnOffComputerMode = downloadQueue.TurnOffComputerMode! switch
        {
            TurnOffComputerMode.Shutdown => "shut down",
            TurnOffComputerMode.Sleep => "sleep",
            TurnOffComputerMode.Hibernate => "hibernate",
            _ => throw new InvalidOperationException("TurnOffComputerMode is not valid.")
        };

        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                // Try to find app service
                var appService = Application.Current?.GetServiceProvider().GetService<IAppService>();
                if (appService == null)
                    throw new InvalidOperationException("AppService not found.");

                Log.Debug("Showing power off window with {Mode} mode", turnOffComputerMode);

                // Create view model and pass required data to it
                var viewModel = new PowerOffWindowViewModel(appService, turnOffComputerMode, TimeSpan.FromSeconds(30));
                // Create window
                var window = new PowerOffWindow { DataContext = viewModel };
                window.Show();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while trying to power off the computer. Error message: {ErrorMessage}", ex.Message);
                await DialogBoxManager.ShowErrorDialogAsync(ex);
            }
        });
    }

    /// <summary>
    /// Exits the program when the download queue is stopped.
    /// </summary>
    private static void ExitProgram()
    {
        Log.Information("Exiting application due to queue completion configuration");

        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                // Close the program
                App.Desktop?.Shutdown();
                Log.Information("Application shutdown initiated successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while trying to exit the app. Error message: {ErrorMessage}", ex.Message);
                await DialogBoxManager.ShowErrorDialogAsync(ex);
            }
        });
    }

    #endregion
}