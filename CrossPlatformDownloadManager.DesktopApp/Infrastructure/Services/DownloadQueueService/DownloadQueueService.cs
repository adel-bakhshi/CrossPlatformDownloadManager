using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Audio;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Audio.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService.ViewModels;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.CustomEventArgs;
using CrossPlatformDownloadManager.Utils.PropertyChanged;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadQueueService;

public class DownloadQueueService : PropertyChangedBase, IDownloadQueueService
{
    #region Private Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IDownloadFileService _downloadFileService;

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

    public DownloadQueueService(IUnitOfWork unitOfWork, IMapper mapper, IDownloadFileService downloadFileService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _downloadFileService = downloadFileService;
        _downloadFileService.DataChanged += DownloadFileServiceOnDataChanged;

        _downloadQueues = [];

        _scheduleManagerTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _scheduleManagerTimer.Tick += ScheduleManagerTimerOnTick;
    }

    private void ScheduleManagerTimerOnTick(object? sender, EventArgs e)
    {
        try
        {
            // Stop timer until operation is finished
            _scheduleManagerTimer.Stop();

            // Start scheduled queues
            var startScheduledQueues = DownloadQueues
                .Where(dq => dq is { StartDownloadSchedule: not null, IsScheduleEnabled: false })
                .ToList();

            foreach (var downloadQueue in startScheduledQueues)
            {
                _ = downloadQueue.IsDaily ? StartDailyScheduleAsync(downloadQueue) : StartJustForDateScheduleAsync(downloadQueue);
            }

            // Stop scheduled queues
            var stopScheduledQueues = DownloadQueues
                .Where(dq => dq.StopDownloadSchedule != null && (dq.IsScheduleEnabled || dq.IsRunning))
                .ToList();

            foreach (var downloadQueue in stopScheduledQueues)
            {
                _ = downloadQueue.IsDaily ? StopDailyScheduleAsync(downloadQueue) : StopJustForDateScheduleAsync(downloadQueue);
            }

            _scheduleManagerTimer.Start();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while scheduling manager. Error message: {ErrorMessage}", ex.Message);

            if (!_scheduleManagerTimer.IsEnabled)
                _scheduleManagerTimer.Start();
        }
    }

    private async Task StartDailyScheduleAsync(DownloadQueueViewModel downloadQueue)
    {
        var daysOfWeek = downloadQueue.DaysOfWeek;
        if (daysOfWeek.IsNullOrEmpty())
            return;

        var daysOfWeekViewModel = daysOfWeek.ConvertFromJson<DaysOfWeekViewModel?>();
        if (daysOfWeekViewModel == null)
            return;

        var currentDayOfWeek = DateTime.Now.DayOfWeek;

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

        if (!dayOfWeekAcceptable)
            return;

        await StartScheduleAsync(downloadQueue);
    }

    private async Task StartJustForDateScheduleAsync(DownloadQueueViewModel downloadQueue)
    {
        // Compare current date with just for date
        if (downloadQueue.JustForDate?.Date.Equals(DateTime.Now.Date) != true)
            return;

        await StartScheduleAsync(downloadQueue);
    }

    private async Task StartScheduleAsync(DownloadQueueViewModel downloadQueue)
    {
        // Make sure start time is equal to current time
        var startDownloadHour = downloadQueue.StartDownloadSchedule!.Value.Hours;
        var startDownloadMinute = downloadQueue.StartDownloadSchedule!.Value.Minutes;

        // Compare current time with start time
        if (startDownloadHour != DateTime.Now.Hour || startDownloadMinute != DateTime.Now.Minute)
            return;

        var downloadFiles = _downloadFileService
            .DownloadFiles
            .Where(df => df.DownloadQueueId == downloadQueue.Id)
            .ToList();

        if (downloadFiles.Count == 0)
            return;

        downloadQueue.IsScheduleEnabled = true;
        // Start download queue
        await StartDownloadQueueAsync(downloadQueue);
    }

    private async Task StopDailyScheduleAsync(DownloadQueueViewModel downloadQueue)
    {
        var daysOfWeek = downloadQueue.DaysOfWeek;
        if (daysOfWeek.IsNullOrEmpty())
            return;

        var daysOfWeekViewModel = daysOfWeek.ConvertFromJson<DaysOfWeekViewModel?>();
        if (daysOfWeekViewModel == null)
            return;

        var currentDayOfWeek = DateTime.Now.DayOfWeek;

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

        if (!dayOfWeekAcceptable)
            return;

        await StopScheduleAsync(downloadQueue);
    }

    private async Task StopJustForDateScheduleAsync(DownloadQueueViewModel downloadQueue)
    {
        // Compare current date with just for date
        if (downloadQueue.JustForDate?.Date.Equals(DateTime.Now.Date) != true)
            return;

        await StopScheduleAsync(downloadQueue);
    }

    private async Task StopScheduleAsync(DownloadQueueViewModel downloadQueue)
    {
        // Make sure stop time is equal to current time
        var stopDownloadHour = downloadQueue.StopDownloadSchedule!.Value.Hours;
        var stopDownloadMinute = downloadQueue.StopDownloadSchedule!.Value.Minutes;

        // Compare current time with stop time
        if (DateTime.Now.Hour != stopDownloadHour || DateTime.Now.Minute != stopDownloadMinute)
            return;

        downloadQueue.IsScheduleEnabled = false;
        // Stop download queue
        await StopDownloadQueueAsync(downloadQueue);
    }

    public async Task LoadDownloadQueuesAsync(bool addDefaultDownloadQueue = false)
    {
        if (addDefaultDownloadQueue)
            await AddDefaultDownloadQueueAsync();

        var downloadQueues = await _unitOfWork
            .DownloadQueueRepository
            .GetAllAsync();

        var deletedDownloadQueues = DownloadQueues
            .Where(vm => !downloadQueues.Exists(dq => dq.Id == vm.Id))
            .ToList();

        foreach (var downloadQueue in deletedDownloadQueues)
            DownloadQueues.Remove(downloadQueue);

        var addedDownloadQueues = downloadQueues
            .Where(dq => DownloadQueues.All(vm => vm.Id != dq.Id))
            .Select(dq => _mapper.Map<DownloadQueueViewModel>(dq))
            .ToList();

        foreach (var downloadQueue in addedDownloadQueues)
            DownloadQueues.Add(downloadQueue);

        OnPropertyChanged(nameof(DownloadQueues));
        DataChanged?.Invoke(this, EventArgs.Empty);
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
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == viewModel?.Id);
        if (downloadQueue == null || downloadQueue.IsRunning)
            return;

        await ContinueDownloadQueueAsync(downloadQueue);
    }

    public async Task StopDownloadQueueAsync(DownloadQueueViewModel? viewModel, bool playSound = true)
    {
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == viewModel?.Id);
        if (downloadQueue == null)
            return;

        downloadQueue.IsRunning = false;
        downloadQueue.IsStartSoundPlayed = false;

        var downloadFiles = _downloadFileService
            .DownloadFiles
            .Where(df => df.DownloadQueueId == downloadQueue.Id && (df.IsDownloading || df.IsPaused))
            .ToList();

        downloadFiles.ForEach(df =>
        {
            _downloadFileService.DownloadFinishedSyncTasks.TryAdd(df.Id, []);
            _downloadFileService.DownloadFinishedSyncTasks[df.Id].Add(downloadFile =>
            {
                try
                {
                    ArgumentNullException.ThrowIfNull(downloadFile);

                    downloadFile.CountOfError = 0;
                    return new DownloadFinishedTaskValue { UpdateDownloadFile = true };
                }
                catch (Exception ex)
                {
                    return new DownloadFinishedTaskValue { UpdateDownloadFile = true, Exception = ex };
                }
            });
        });

        var tasks = downloadFiles.ConvertAll(downloadFile => _downloadFileService.StopDownloadFileAsync(downloadFile, playSound: false));
        await Task.WhenAll(tasks);

        if (playSound)
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

    #region Helpers

    private async Task AddDefaultDownloadQueueAsync()
    {
        var downloadQueueInDb = await _unitOfWork
            .DownloadQueueRepository
            .GetAsync(where: dq => dq.Title.ToLower() == Constants.DefaultDownloadQueueTitle.ToLower());

        if (downloadQueueInDb != null)
            return;

        var downloadQueue = new DownloadQueue
        {
            Title = Constants.DefaultDownloadQueueTitle,
            RetryOnDownloadingFailed = true,
            RetryCount = 3,
            ShowAlarmWhenDone = true,
            DownloadCountAtSameTime = 2,
            IncludePausedFiles = false,
        };

        await AddNewDownloadQueueAsync(downloadQueue, reloadData: false);
    }

    private void DownloadFileServiceOnDataChanged(object? sender, EventArgs e)
    {
        // Remove DownloadFile from DownloadQueueTasks list when DownloadFile does not exist anymore
        var primaryKeys = _downloadFileService
            .DownloadFiles
            .Select(df => df.Id)
            .ToList();

        var downloadQueues = DownloadQueues
            .Where(dq => dq.DownloadingFiles.Exists(df => !primaryKeys.Contains(df.Id)))
            .ToList();

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

    private async Task ContinueDownloadQueueAsync(DownloadQueueViewModel viewModel)
    {
        var primaryKeys = await _unitOfWork
            .DownloadFileRepository
            .GetAllAsync(where: df => df.DownloadQueueId == viewModel.Id, select: df => df.Id, distinct: true);

        var downloadFiles = _downloadFileService
            .DownloadFiles
            .Where(df => df.DownloadQueueId == viewModel.Id
                         && primaryKeys.Contains(df.Id)
                         && df is { IsDownloading: false, IsPaused: false, IsCompleted: false }
                         && (!viewModel.RetryOnDownloadingFailed || df.CountOfError < viewModel.RetryCount))
            .ToList();

        if (viewModel.IncludePausedFiles)
        {
            var pausedDownloadFiles = _downloadFileService
                .DownloadFiles
                .Where(df => df.DownloadQueueId == viewModel.Id
                             && primaryKeys.Contains(df.Id)
                             && df.IsPaused
                             && (!viewModel.RetryOnDownloadingFailed || df.CountOfError < viewModel.RetryCount))
                .ToList();

            downloadFiles = downloadFiles
                .Union(pausedDownloadFiles)
                .Distinct()
                .ToList();
        }

        downloadFiles = downloadFiles
            .OrderBy(df => df.DownloadQueuePriority)
            .ToList();

        // All download files are finished
        if (downloadFiles.Count == 0 && viewModel.DownloadingFiles.Count == 0)
        {
            // Play queue finished sound when start queue sound is already played
            if (viewModel.IsStartSoundPlayed)
                _ = AudioManager.PlayAsync(AppNotificationType.QueueFinished);

            await StopDownloadQueueAsync(viewModel, playSound: false);
            return;
        }

        if (!viewModel.IsRunning)
            viewModel.IsRunning = true;

        var taskIndex = 0;
        while (viewModel.DownloadingFiles.Count < viewModel.DownloadCountAtSameTime && taskIndex < downloadFiles.Count)
        {
            var downloadFile = downloadFiles[taskIndex];

            _downloadFileService.DownloadFinishedAsyncTasks.TryAdd(downloadFile.Id, []);
            _downloadFileService.DownloadFinishedAsyncTasks[downloadFile.Id].Add(DownloadFileFinishedTaskAsync);

            downloadFile.DownloadPaused += DownloadFileOnDownloadPaused;

            _ = _downloadFileService.StartDownloadFileAsync(downloadFile, showWindow: false);

            viewModel.DownloadingFiles.Add(downloadFile);
            taskIndex++;
        }

        if (viewModel.DownloadingFiles.Count == 0)
        {
            await StopDownloadQueueAsync(viewModel);
            return;
        }

        if (!viewModel.IsStartSoundPlayed)
        {
            _ = AudioManager.PlayAsync(AppNotificationType.QueueStarted);
            viewModel.IsStartSoundPlayed = true;
        }
    }

    private async Task<DownloadFinishedTaskValue?> DownloadFileFinishedTaskAsync(DownloadFileViewModel? viewModel)
    {
        try
        {
            var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == viewModel?.TempDownloadQueueId);
            var downloadFile = downloadQueue?.DownloadingFiles.Find(df => df.Id == viewModel?.Id);
            if (downloadFile == null)
                return null;

            downloadFile.DownloadPaused -= DownloadFileOnDownloadPaused;

            if ((downloadFile.IsError || downloadFile.IsStopped) && downloadQueue!.IsRunning)
            {
                var maxPriority = await _unitOfWork
                    .DownloadFileRepository
                    .GetMaxAsync(selector: df => df.DownloadQueuePriority, where: df => df.DownloadQueueId == downloadQueue.Id);

                downloadFile.DownloadQueuePriority = maxPriority + 1;

                if (downloadFile.IsError)
                    downloadFile.CountOfError++;
            }

            if (downloadFile.IsCompleted || downloadFile.IsError || downloadFile.IsStopped)
                downloadQueue!.DownloadingFiles.Remove(downloadFile);

            await _downloadFileService.UpdateDownloadFileAsync(downloadFile);

            if (downloadQueue!.IsRunning)
                _ = ContinueDownloadQueueAsync(downloadQueue);

            return new DownloadFinishedTaskValue { UpdateDownloadFile = false };
        }
        catch (Exception ex)
        {
            return new DownloadFinishedTaskValue { Exception = ex };
        }
    }

    private void DownloadFileOnDownloadPaused(object? sender, DownloadFileEventArgs e)
    {
        var downloadFile = _downloadFileService.DownloadFiles.FirstOrDefault(df => df.Id == e.Id);
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == downloadFile?.DownloadQueueId);
        if (downloadQueue == null || downloadQueue.IncludePausedFiles || !downloadQueue.IsRunning)
            return;

        _ = ContinueDownloadQueueAsync(downloadQueue);
    }

    #endregion
}