using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
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
using Microsoft.Extensions.DependencyInjection;
using RolandK.AvaloniaExtensions.DependencyInjection;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadQueueService;

public class DownloadQueueService : PropertyChangedBase, IDownloadQueueService
{
    #region Private Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IDownloadFileService _downloadFileService;
    private readonly ISettingsService _settingsService;

    private ObservableCollection<DownloadQueueViewModel> _downloadQueues;
    private readonly DispatcherTimer _scheduleManagerTimer;

    #endregion

    #region Events

    public event EventHandler? DataChanged;

    #endregion

    #region Properties

    public ObservableCollection<DownloadQueueViewModel> DownloadQueues
    {
        get => _downloadQueues;
        private set => SetField(ref _downloadQueues, value);
    }

    #endregion

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
    }

    public async Task LoadDownloadQueuesAsync(bool addDefaultDownloadQueue = false)
    {
        // Add default download queue
        if (addDefaultDownloadQueue)
            await AddDefaultDownloadQueueAsync();

        // Get all download queues from database
        var downloadQueues = await _unitOfWork
            .DownloadQueueRepository
            .GetAllAsync();

        // Find deleted download queues
        var deletedDownloadQueues = DownloadQueues
            .Where(vm => !downloadQueues.Exists(dq => dq.Id == vm.Id))
            .ToList();

        // Remove download queues from list
        foreach (var downloadQueue in deletedDownloadQueues)
            DownloadQueues.Remove(downloadQueue);

        // Find added download queues
        var addedDownloadQueues = downloadQueues
            .Where(dq => DownloadQueues.All(vm => vm.Id != dq.Id))
            .Select(dq => _mapper.Map<DownloadQueueViewModel>(dq))
            .ToList();

        // Add new download queues to list
        foreach (var downloadQueue in addedDownloadQueues)
            DownloadQueues.Add(downloadQueue);

        // Notify download queues changed
        OnPropertyChanged(nameof(DownloadQueues));
        // Raise changed event
        DataChanged?.Invoke(this, EventArgs.Empty);
        // Log information
        Log.Information("Download queues loaded successfully.");
    }

    public async Task<int> AddNewDownloadQueueAsync(DownloadQueue? downloadQueue, bool reloadData = true)
    {
        if (downloadQueue is not { Id: 0 })
            return 0;

        await _unitOfWork.DownloadQueueRepository.AddAsync(downloadQueue);
        await _unitOfWork.SaveAsync();

        if (reloadData)
            await LoadDownloadQueuesAsync();

        return downloadQueue.Id;
    }

    public async Task DeleteDownloadQueueAsync(DownloadQueueViewModel? viewModel, bool reloadData = true)
    {
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == viewModel?.Id);
        if (downloadQueue == null)
            return;

        var downloadQueueInDb = await _unitOfWork
            .DownloadQueueRepository
            .GetAsync(dq => dq.Id == downloadQueue.Id);

        if (downloadQueueInDb == null)
            return;

        var downloadFiles = _downloadFileService
            .DownloadFiles
            .Where(df => df.DownloadQueueId == downloadQueueInDb.Id)
            .ToList();

        await RemoveDownloadFilesFromDownloadQueueAsync(downloadQueue, downloadFiles);

        await _unitOfWork.DownloadQueueRepository.DeleteAsync(downloadQueueInDb);
        await _unitOfWork.SaveAsync();

        if (reloadData)
            await LoadDownloadQueuesAsync();
    }

    public async Task UpdateDownloadQueueAsync(DownloadQueue? downloadQueue, bool reloadData = true)
    {
        if (downloadQueue == null)
            return;

        await _unitOfWork.DownloadQueueRepository.UpdateAsync(downloadQueue);
        await _unitOfWork.SaveAsync();

        if (reloadData)
            await LoadDownloadQueuesAsync();
    }

    public async Task UpdateDownloadQueueAsync(DownloadQueueViewModel? viewModel, bool reloadData = true)
    {
        if (viewModel == null)
            return;

        var downloadQueue = _mapper.Map<DownloadQueue>(viewModel);
        await UpdateDownloadQueueAsync(downloadQueue, reloadData);
    }

    public async Task UpdateDownloadQueuesAsync(List<DownloadQueue>? downloadQueues, bool reloadData = true)
    {
        if (downloadQueues == null || downloadQueues.Count == 0)
            return;

        await _unitOfWork.DownloadQueueRepository.UpdateAllAsync(downloadQueues);
        await _unitOfWork.SaveAsync();

        if (reloadData)
            await LoadDownloadQueuesAsync();
    }

    public async Task UpdateDownloadQueuesAsync(List<DownloadQueueViewModel>? viewModels, bool reloadData = true)
    {
        if (viewModels == null || viewModels.Count == 0)
            return;

        var downloadQueues = _mapper.Map<List<DownloadQueue>>(viewModels);
        await UpdateDownloadQueuesAsync(downloadQueues, reloadData);
    }

    public async Task StartDownloadQueueAsync(DownloadQueueViewModel? viewModel)
    {
        // Try to find the download queue
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == viewModel?.Id);
        if (downloadQueue == null || downloadQueue.IsRunning)
            return;

        // Start the download queue
        await ContinueDownloadQueueAsync(downloadQueue);
    }

    public async Task StopDownloadQueueAsync(DownloadQueueViewModel? viewModel, bool playSound = true)
    {
        // Try to find the download queue
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == viewModel?.Id);
        if (downloadQueue == null)
            return;

        // Set the IsRunning flag to false
        downloadQueue.IsRunning = false;
        // Set the IsStartSoundPlayed flag to false
        downloadQueue.IsStartSoundPlayed = false;
        // Get all download file belongs to the current download queue and currently downloading or paused
        var downloadFiles = _downloadFileService
            .DownloadFiles
            .Where(df => df.DownloadQueueId == downloadQueue.Id && (df.IsDownloading || df.IsPaused) && !df.IsStopping)
            .ToList();

        // Reset the count of error for each download file when the download is completed.
        foreach (var downloadFile in downloadFiles)
            _downloadFileService.AddCompletedTask(downloadFile, ResetCountOfError);

        var finishedTasks = downloadFiles.ConvertAll(df => _downloadFileService.StopDownloadFileAsync(df, ensureStopped: true, playSound: false));
        await Task.WhenAll(finishedTasks);

        // Turn off computer
        if (downloadQueue.TurnOffComputerWhenDone)
        {
            TurnOffComputer(downloadQueue);
            return;
        }

        // Exit program
        if (downloadQueue.ExitProgramWhenDone)
        {
            ExitProgram();
            return;
        }

        // Play the queue stopped sound if possible
        if (playSound && _settingsService.Settings.UseQueueStoppedSound)
            _ = AudioManager.PlayAsync(AppNotificationType.QueueStopped);
    }

    public async Task AddDownloadFileToDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel,
        DownloadFileViewModel? downloadFileViewModel)
    {
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == downloadQueueViewModel?.Id);
        var downloadFile = _downloadFileService.DownloadFiles.FirstOrDefault(df => df.Id == downloadFileViewModel?.Id);
        if (downloadQueue == null || downloadFile == null || downloadFile.IsCompleted)
            return;

        // The download file is already in the queue and there is no need to add it again.
        if (downloadFile.DownloadQueueId == downloadQueue.Id)
            return;

        downloadFile.DownloadQueueId = downloadQueue.Id;
        downloadFile.DownloadQueuePriority = (_downloadFileService
            .DownloadFiles
            .Where(df => df.DownloadQueueId == downloadQueue.Id)
            .Max(df => df.DownloadQueuePriority) ?? 0) + 1;

        await _downloadFileService.UpdateDownloadFileAsync(downloadFile);
        await LoadDownloadQueuesAsync(addDefaultDownloadQueue: false);
    }

    public async Task AddDownloadFilesToDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel,
        List<DownloadFileViewModel>? downloadFilesViewModels)
    {
        if (downloadFilesViewModels == null || downloadFilesViewModels.Count == 0)
            return;

        var primaryKeys = downloadFilesViewModels
            .Select(df => df.Id)
            .Distinct()
            .ToList();

        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == downloadQueueViewModel?.Id);
        if (downloadQueue == null)
            return;

        var downloadFiles = primaryKeys
            .Select(pk => _downloadFileService.DownloadFiles.FirstOrDefault(df => df.Id == pk && !df.IsCompleted && df.DownloadQueueId != downloadQueue.Id))
            .OfType<DownloadFileViewModel>()
            .ToList();

        if (downloadFiles.Count == 0)
            return;

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
    }

    public async Task RemoveDownloadFileFromDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel,
        DownloadFileViewModel? downloadFileViewModel)
    {
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == downloadQueueViewModel?.Id);
        var downloadFile = _downloadFileService.DownloadFiles.FirstOrDefault(df => df.Id == downloadFileViewModel?.Id);
        if (downloadQueue == null || downloadFile == null || downloadFile.IsDownloading || downloadFile.IsPaused)
            return;

        // Make sure the file is in the queue.
        if (downloadFile.DownloadQueueId != downloadQueue.Id)
            return;

        downloadFile.DownloadQueueId = null;
        downloadFile.DownloadQueueName = null;
        downloadFile.DownloadQueuePriority = null;

        await _downloadFileService.UpdateDownloadFileAsync(downloadFile);
        await LoadDownloadQueuesAsync(addDefaultDownloadQueue: false);
    }

    public async Task RemoveDownloadFilesFromDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel,
        List<DownloadFileViewModel> downloadFileViewModels)
    {
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == downloadQueueViewModel?.Id);
        if (downloadQueue == null || downloadFileViewModels.Count == 0)
            return;

        var downloadFiles = _downloadFileService
            .DownloadFiles
            .Where(df => downloadFileViewModels.Exists(vm => vm.Id == df.Id)
                         && df is { IsDownloading: false, IsPaused: false }
                         && df.DownloadQueueId == downloadQueue.Id)
            .ToList();

        if (downloadFiles.Count == 0)
            return;

        foreach (var downloadFile in downloadFiles)
        {
            downloadFile.DownloadQueueId = null;
            downloadFile.DownloadQueueName = null;
            downloadFile.DownloadQueuePriority = null;
        }

        await _downloadFileService.UpdateDownloadFilesAsync(downloadFiles);
        await LoadDownloadQueuesAsync(addDefaultDownloadQueue: false);
    }

    public async Task ChangeDefaultDownloadQueueAsync(DownloadQueueViewModel? viewModel, bool reloadData = true)
    {
        // Make sure given download queue is not null
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == viewModel?.Id);
        if (downloadQueue == null)
            return;

        // Get all download queues that are set as default
        var viewModels = DownloadQueues
            .Where(dq => dq.IsDefault)
            .ToList();

        // Download queue already set as default and no need to set it again
        if (viewModels.Count == 1 && viewModels[0].Id == downloadQueue.Id)
            return;

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
    }

    public async Task ChangeLastSelectedDownloadQueueAsync(DownloadQueueViewModel? viewModel, bool reloadData = true)
    {
        // Make sure given download queue is not null
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == viewModel?.Id);
        if (downloadQueue == null)
            return;

        // Get all download queues that are set as last choice
        var viewModels = DownloadQueues
            .Where(dq => dq.IsLastChoice)
            .ToList();

        // Download queue already set as last choice and no need to set it again
        if (viewModels.Count == 1 && viewModels[0].Id == downloadQueue.Id)
            return;

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
    }

    public void StartScheduleManagerTimer()
    {
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
        // Get all primary keys of download files
        var primaryKeys = _downloadFileService
            .DownloadFiles
            .Select(df => df.Id)
            .ToList();

        // Find download queues that has a download file that is not in the given list of primary keys
        var downloadQueues = DownloadQueues
            .Where(dq => dq.DownloadingFiles.Exists(df => !primaryKeys.Contains(df.Id)))
            .ToList();

        // Remove download files from download queues that are not in the given list of primary keys
        foreach (var downloadQueue in downloadQueues)
        {
            var downloadFiles = downloadQueue
                .DownloadingFiles
                .Where(df => !primaryKeys.Contains(df.Id))
                .ToList();

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
        // Try to find download file by id
        var downloadFile = _downloadFileService.DownloadFiles.FirstOrDefault(df => df.Id == e.Id);
        // Try to find download queue by download queue id
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == downloadFile?.DownloadQueueId);
        // Check if the download queue supports paused files and currently is running
        if (downloadQueue == null || downloadQueue.IncludePausedFiles || !downloadQueue.IsRunning)
            return;

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
            // Stop timer until operation is finished
            _scheduleManagerTimer.Stop();
            // Get all download queues that are scheduled to start
            var startScheduledQueues = DownloadQueues
                .Where(dq => dq is { StartDownloadSchedule: not null, IsScheduleEnabled: false })
                .ToList();

            // Start download queue based on it's a daily schedule or just for date schedule
            foreach (var downloadQueue in startScheduledQueues)
                _ = downloadQueue.IsDaily ? StartDailyScheduleAsync(downloadQueue) : StartJustForDateScheduleAsync(downloadQueue);

            // Get all download queues that are scheduled to stop
            var stopScheduledQueues = DownloadQueues
                .Where(dq => dq.StopDownloadSchedule != null && (dq.IsScheduleEnabled || dq.IsRunning))
                .ToList();

            // Stop download queue based on it's a daily schedule or just for date schedule
            foreach (var downloadQueue in stopScheduledQueues)
                _ = downloadQueue.IsDaily ? StopDailyScheduleAsync(downloadQueue) : StopJustForDateScheduleAsync(downloadQueue);

            // Restart the timer to check for scheduled download queues again
            _scheduleManagerTimer.Start();
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
        // Get the default download queue from the database
        var downloadQueueInDb = await _unitOfWork
            .DownloadQueueRepository
            .GetAsync(where: dq => dq.Title.ToLower() == Constants.DefaultDownloadQueueTitle.ToLower());

        // Check if the default download queue exists
        if (downloadQueueInDb != null)
            return;

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
    }

    /// <summary>
    /// Starts or continues the process of a download queue.
    /// </summary>
    /// <param name="viewModel">The download queue that should be started or continued.</param>
    private async Task ContinueDownloadQueueAsync(DownloadQueueViewModel viewModel)
    {
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
        }

        // Sort download files by download queue priority
        downloadFiles = downloadFiles.OrderBy(df => df.DownloadQueuePriority).ToList();
        // All download files are finished
        if (downloadFiles.Count == 0 && viewModel.DownloadingFiles.Count == 0)
        {
            // Check for play finished sound
            var playFinishedSound = viewModel.IsStartSoundPlayed && _settingsService.Settings.UseQueueFinishedSound;
            // Play sound when possible
            if (playFinishedSound || viewModel.ShowAlarmWhenDone)
                _ = AudioManager.PlayAsync(AppNotificationType.QueueFinished);

            // Stop download queue
            await StopDownloadQueueAsync(viewModel, playSound: !playFinishedSound);
            return;
        }

        // Set the IsRunning flag to true
        if (!viewModel.IsRunning)
            viewModel.IsRunning = true;

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
            index++;
        }

        // Stop download queue when there is no download file in the downloading files list
        if (viewModel.DownloadingFiles.Count == 0)
        {
            await StopDownloadQueueAsync(viewModel);
            return;
        }

        // Play download queue started sound if possible
        if (!viewModel.IsStartSoundPlayed && _settingsService.Settings.UseQueueStartedSound)
        {
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
        // Try to find the download queue by TempDownloadQueueId
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == viewModel?.TempDownloadQueueId);
        // Try to find the download file in the download queue
        var downloadFile = downloadQueue?.DownloadingFiles.Find(df => df.Id == viewModel?.Id);
        if (downloadFile == null)
            return;

        // Unsubscribe from DownloadPaused event
        downloadFile.DownloadPaused -= DownloadFileOnDownloadPaused;
        // Check if an error occurred during the download or the download stopped by the user when the download queue is still running.
        // In this situation we have to change the priority of the download file to the lowest priority (Change its position to the end of the queue).
        if ((downloadFile.IsError || downloadFile.IsStopped) && downloadQueue!.IsRunning)
        {
            // Get the maximum priority for the current queue.
            var maxPriority = _downloadFileService
                .DownloadFiles
                .Where(df => df.DownloadQueueId == downloadQueue.Id)
                .Max(df => df.DownloadQueuePriority);

            // Change the priority of the download file
            downloadFile.DownloadQueuePriority = maxPriority + 1;
            // Check if an error occurred during the download
            // When error occurred we have to update the count of error for the download file
            // So we can ignore this file when the download queue continues to run
            if (downloadFile.IsError)
                downloadFile.CountOfError++;
        }

        // Remove the download file from the downloading files collection
        // So when the download queue continue to run, it can choose another file to download it
        if (downloadFile.IsCompleted || downloadFile.IsError || downloadFile.IsStopped)
            downloadQueue!.DownloadingFiles.Remove(downloadFile);

        // Update the download file
        await _downloadFileService.UpdateDownloadFileAsync(downloadFile);
        // Check if the download queue still running
        // If it's running, continue to download the next file(s)
        if (downloadQueue!.IsRunning)
            _ = ContinueDownloadQueueAsync(downloadQueue);
    }

    /// <summary>
    /// Starts a download queue that scheduled to run daily.
    /// </summary>
    /// <param name="downloadQueue">The download queue that scheduled to run daily.</param>
    private async Task StartDailyScheduleAsync(DownloadQueueViewModel downloadQueue)
    {
        // Check if the current day of week is acceptable for the download queue
        if (!CheckTheDayIsAcceptable(downloadQueue))
            return;

        // Start the schedule
        await StartScheduleAsync(downloadQueue);
    }

    /// <summary>
    /// Starts a download queue that scheduled to run in a specific date.
    /// </summary>
    /// <param name="downloadQueue">The download queue that scheduled to run in a specific date.</param>
    private async Task StartJustForDateScheduleAsync(DownloadQueueViewModel downloadQueue)
    {
        // Compare current date with just for date
        if (downloadQueue.JustForDate?.Date.Equals(DateTime.Now.Date) != true)
            return;

        // Start the schedule
        await StartScheduleAsync(downloadQueue);
    }

    /// <summary>
    /// Starts the schedule.
    /// </summary>
    /// <param name="downloadQueue">The download queue that scheduled.</param>
    private async Task StartScheduleAsync(DownloadQueueViewModel downloadQueue)
    {
        // Make sure start time is equal to current time
        var startDownloadHour = downloadQueue.StartDownloadSchedule!.Value.Hours;
        var startDownloadMinute = downloadQueue.StartDownloadSchedule!.Value.Minutes;

        // Compare current time with start time
        if (startDownloadHour != DateTime.Now.Hour || startDownloadMinute != DateTime.Now.Minute)
            return;

        // Get all download files that belong to the download queue
        var downloadFiles = _downloadFileService
            .DownloadFiles
            .Where(df => df.DownloadQueueId == downloadQueue.Id)
            .ToList();

        // Check if download files collection is empty
        if (downloadFiles.Count == 0)
            return;

        // Set the IsScheduleEnabled flag to true
        downloadQueue.IsScheduleEnabled = true;
        // Start download queue
        await StartDownloadQueueAsync(downloadQueue);
    }

    /// <summary>
    /// Stops a download queue that scheduled to run daily.
    /// </summary>
    /// <param name="downloadQueue">The download queue that scheduled to run daily.</param>
    private async Task StopDailyScheduleAsync(DownloadQueueViewModel downloadQueue)
    {
        // Check if the current day of week is acceptable for the download queue
        if (!CheckTheDayIsAcceptable(downloadQueue))
            return;

        // Stop the schedule
        await StopScheduleAsync(downloadQueue);
    }

    /// <summary>
    /// Stops a download queue that scheduled to run in a specific date.
    /// </summary>
    /// <param name="downloadQueue">The download queue that scheduled to run in a specific date.</param>
    private async Task StopJustForDateScheduleAsync(DownloadQueueViewModel downloadQueue)
    {
        // Compare current date with just for date
        if (downloadQueue.JustForDate?.Date.Equals(DateTime.Now.Date) != true)
            return;

        // Stop the schedule
        await StopScheduleAsync(downloadQueue);
    }

    /// <summary>
    /// Stops the schedule.
    /// </summary>
    /// <param name="downloadQueue">The download queue that scheduled.</param>
    private async Task StopScheduleAsync(DownloadQueueViewModel downloadQueue)
    {
        // Make sure stop time is equal to current time
        var stopDownloadHour = downloadQueue.StopDownloadSchedule!.Value.Hours;
        var stopDownloadMinute = downloadQueue.StopDownloadSchedule!.Value.Minutes;

        // Compare current time with stop time
        if (DateTime.Now.Hour != stopDownloadHour || DateTime.Now.Minute != stopDownloadMinute)
            return;

        // Set the IsScheduleEnabled flag to false
        downloadQueue.IsScheduleEnabled = false;
        // Stop download queue
        await StopDownloadQueueAsync(downloadQueue);
    }

    /// <summary>
    /// Checks that the current day is an acceptable day for the schedule.
    /// </summary>
    /// <param name="downloadQueue">The download queue that scheduled to run daily.</param>
    /// <returns>Returns true if the current day is an acceptable day for the schedule, otherwise returns false.</returns>
    private static bool CheckTheDayIsAcceptable(DownloadQueueViewModel downloadQueue)
    {
        // Get days of week that sets for the download queue
        var daysOfWeek = downloadQueue.DaysOfWeek;
        // Make sure the days of week is not empty
        if (daysOfWeek.IsStringNullOrEmpty())
            return false;

        // Convert days of week from json to DaysOfWeekViewModel
        var daysOfWeekViewModel = daysOfWeek.ConvertFromJson<DaysOfWeekViewModel?>();
        // Check if view model is not null
        if (daysOfWeekViewModel == null)
            return false;

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
    }

    /// <summary>
    /// Shows the power off window and turn off the computer.
    /// </summary>
    /// <param name="downloadQueue">The download queue that stopped and should turn off the computer.</param>
    /// <exception cref="InvalidOperationException">if the mode of the turn-off computer is not valid.</exception>
    private static void TurnOffComputer(DownloadQueueViewModel downloadQueue)
    {
        // Check if the mode of the turn-off computer is valid 
        if (downloadQueue.TurnOffComputerMode == null)
            return;

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
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                // Close the program
                App.Desktop?.Shutdown();
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