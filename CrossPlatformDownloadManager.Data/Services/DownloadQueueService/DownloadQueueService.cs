using System.Collections.ObjectModel;
using AutoMapper;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.DownloadFileService;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;
using CrossPlatformDownloadManager.Data.ViewModels.DbViewModels;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.PropertyChanged;

namespace CrossPlatformDownloadManager.Data.Services.DownloadQueueService;

public class DownloadQueueService : PropertyChangedBase, IDownloadQueueService
{
    #region Private Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IDownloadFileService _downloadFileService;
    private readonly List<DownloadQueueTaskViewModel> _downloadQueueTasks;

    private ObservableCollection<DownloadQueueViewModel> _downloadQueues;

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

        _downloadQueueTasks = [];

        _downloadQueues = [];
    }

    public async Task LoadDownloadQueuesAsync(bool addDefaultDownloadQueue = false)
    {
        if (addDefaultDownloadQueue)
            await AddDefaultDownloadQueueAsync();

        var downloadQueues = await _unitOfWork
            .DownloadQueueRepository
            .GetAllAsync(includeProperties: ["DownloadFiles"]);

        var primaryKeys = downloadQueues
            .Select(df => df.Id)
            .ToList();

        var exceptDownloadQueues = DownloadQueues
            .Where(df => !primaryKeys.Contains(df.Id))
            .ToList();

        foreach (var downloadQueue in exceptDownloadQueues)
            await DeleteDownloadQueueAsync(viewModel: downloadQueue, reloadData: false);

        var downloadQueueViewModels = _mapper.Map<List<DownloadQueueViewModel>>(downloadQueues);
        foreach (var downloadQueue in downloadQueueViewModels)
        {
            var oldDownloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == downloadQueue.Id);
            if (oldDownloadQueue != null)
                oldDownloadQueue.UpdateViewModel(downloadQueue);
            else
                DownloadQueues.Add(downloadQueue);
        }

        OnPropertyChanged(nameof(DownloadQueues));
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task AddNewDownloadQueueAsync(DownloadQueue? downloadQueue, bool reloadData = true)
    {
        if (downloadQueue is not { Id: 0 })
            return;

        await _unitOfWork.DownloadQueueRepository.AddAsync(downloadQueue);
        await _unitOfWork.SaveAsync();

        if (reloadData)
            await LoadDownloadQueuesAsync();
    }

    public async Task DeleteDownloadQueueAsync(DownloadQueueViewModel? viewModel, bool reloadData = true)
    {
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == viewModel?.Id);
        if (downloadQueue == null)
            return;

        var downloadQueueInDb = await _unitOfWork
            .DownloadQueueRepository
            .GetAsync(dq => dq.Id == downloadQueue.Id, includeProperties: "DownloadFiles");

        if (downloadQueueInDb == null)
            return;

        foreach (var downloadFile in downloadQueueInDb.DownloadFiles)
        {
            downloadFile.DownloadQueueId = null;
            downloadFile.DownloadQueuePriority = null;
        }

        await _downloadFileService.UpdateDownloadFilesAsync(downloadQueueInDb.DownloadFiles.ToList());

        _unitOfWork.DownloadQueueRepository.Delete(downloadQueueInDb);
        await _unitOfWork.SaveAsync();

        if (reloadData)
            await LoadDownloadQueuesAsync();
    }

    public async Task UpdateDownloadQueueAsync(DownloadQueue? downloadQueue)
    {
        if (downloadQueue == null)
            return;

        await _unitOfWork.DownloadQueueRepository.UpdateAsync(downloadQueue);
        await _unitOfWork.SaveAsync();
        await LoadDownloadQueuesAsync();
    }

    public async Task StartDownloadQueueAsync(DownloadQueueViewModel? viewModel)
    {
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == viewModel?.Id);
        if (downloadQueue == null)
            return;

        var primaryKeys = await _unitOfWork
            .DownloadFileRepository
            .GetAllAsync(where: df => df.DownloadQueueId == downloadQueue.Id, select: df => df.Id, distinct: true);

        var downloadFiles = _downloadFileService
            .DownloadFiles
            .Where(df => df.DownloadQueueId == downloadQueue.Id
                         && primaryKeys.Contains(df.Id)
                         && df is { IsDownloading: false, IsPaused: false }
                         && (!downloadQueue.RetryOnDownloadingFailed || df.CountOfError < downloadQueue.RetryCount))
            .ToList();

        if (downloadQueue.IncludePausedFiles)
        {
            var pausedDownloadFiles = _downloadFileService
                .DownloadFiles
                .Where(df => df.DownloadQueueId == downloadQueue.Id
                             && primaryKeys.Contains(df.Id)
                             && df.IsPaused
                             && (!downloadQueue.RetryOnDownloadingFailed || df.CountOfError < downloadQueue.RetryCount))
                .ToList();

            downloadFiles = downloadFiles
                .Union(pausedDownloadFiles)
                .ToList();
        }

        downloadFiles = downloadFiles
            .OrderBy(df => df.DownloadQueuePriority)
            .ToList();

        if (downloadFiles.Count == 0)
        {
            await StopDownloadQueueAsync(downloadQueue);
            return;
        }

        if (!downloadQueue.IsRunning)
            downloadQueue.IsRunning = true;

        var downloadQueueTasks = _downloadQueueTasks
            .Where(task => task.DownloadFile?.IsCompleted == true
                           || task.DownloadFile?.IsError == true
                           || task.DownloadFile?.IsStopped == true)
            .ToList();

        foreach (var task in downloadQueueTasks)
            _downloadQueueTasks.Remove(task);

        var taskIndex = 0;
        while (_downloadQueueTasks.Count < downloadQueue.DownloadCountAtSameTime && taskIndex < downloadFiles.Count)
        {
            var downloadFile = downloadFiles[taskIndex];
            downloadFile.DownloadFinished += DownloadFileOnDownloadFinished;
            downloadFile.DownloadPaused += DownloadFileOnDownloadPaused;

            _ = _downloadFileService.StartDownloadFileAsync(downloadFile);

            _downloadQueueTasks.Add(new DownloadQueueTaskViewModel
            {
                DownloadQueueId = downloadQueue.Id,
                DownloadFileId = downloadFile.Id,
                DownloadFile = downloadFile,
            });

            taskIndex++;
        }

        if (_downloadQueueTasks.Count == 0)
            await StopDownloadQueueAsync(downloadQueue);
    }

    public async Task StopDownloadQueueAsync(DownloadQueueViewModel? viewModel)
    {
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == viewModel?.Id);
        if (downloadQueue == null)
            return;

        downloadQueue.IsRunning = false;

        var downloadFiles = _downloadFileService
            .DownloadFiles
            .Where(df => df.DownloadQueueId == downloadQueue.Id && df.IsDownloading)
            .ToList();

        foreach (var downloadFile in downloadFiles)
        {
            await _downloadFileService.StopDownloadFileAsync(downloadFile);
            await Task.Delay(500);
        }

        downloadFiles = downloadFiles
            .Select(df =>
            {
                df.CountOfError = 0;
                return df;
            })
            .ToList();

        await _downloadFileService.UpdateDownloadFilesAsync(downloadFiles);
    }

    public async Task AddDownloadFileToDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel,
        DownloadFileViewModel? downloadFileViewModel)
    {
        if (downloadQueueViewModel == null || downloadFileViewModel == null)
            return;
    }

    public async Task RemoveDownloadFileFromDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel,
        DownloadFileViewModel? downloadFileViewModel)
    {
        if (downloadQueueViewModel == null || downloadFileViewModel == null)
            return;
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

    private async void DownloadFileOnDownloadFinished(object? sender, DownloadFileEventArgs e)
    {
        if (sender is DownloadFileViewModel originalDownloadFile)
        {
            originalDownloadFile.DownloadFinished -= DownloadFileOnDownloadFinished;
            originalDownloadFile.DownloadPaused -= DownloadFileOnDownloadPaused;
        }

        var downloadQueueTask = _downloadQueueTasks.Find(t => t.DownloadFileId == e.Id);
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == downloadQueueTask?.DownloadQueueId);
        if (downloadQueue == null || downloadQueueTask?.DownloadFile == null)
            return;

        if (downloadQueueTask.DownloadFile.IsCompleted)
        {
            downloadQueueTask.DownloadFile.DownloadQueueId = null;
            downloadQueueTask.DownloadFile.DownloadQueuePriority = null;
        }
        else if ((downloadQueueTask.DownloadFile.IsError || downloadQueueTask.DownloadFile.IsStopped)
                 && downloadQueue.IsRunning)
        {
            var maxPriority = await _unitOfWork
                .DownloadFileRepository
                .GetMaxAsync(selector: df => df.DownloadQueuePriority,
                    where: df => df.DownloadQueueId == downloadQueue.Id);

            downloadQueueTask.DownloadFile.DownloadQueuePriority = maxPriority + 1;

            if (downloadQueueTask.DownloadFile.IsError)
                downloadQueueTask.DownloadFile.CountOfError++;
        }

        await _downloadFileService.UpdateDownloadFileAsync(downloadQueueTask.DownloadFile);

        downloadQueueTask.ContinueDownloadQueueTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        downloadQueueTask.ContinueDownloadQueueTimer.Tick += ContinueDownloadQueueTimerOnTick;
        downloadQueueTask.ContinueDownloadQueueTimer.Tag = downloadQueueTask;
        downloadQueueTask.ContinueDownloadQueueTimer.Start();
    }

    private void ContinueDownloadQueueTimerOnTick(object? sender, EventArgs e)
    {
        if (sender is not DispatcherTimer { Tag: DownloadQueueTaskViewModel downloadQueueTask } timer
            || !timer.Equals(downloadQueueTask.ContinueDownloadQueueTimer)
            || downloadQueueTask.DownloadFile == null)
        {
            return;
        }

        downloadQueueTask.ContinueDownloadQueueTimer.Stop();
        downloadQueueTask.ContinueDownloadQueueTimer.Tick -= ContinueDownloadQueueTimerOnTick;
        downloadQueueTask.ContinueDownloadQueueTimer = null;

        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == downloadQueueTask.DownloadQueueId);
        if (downloadQueue?.IsRunning == true)
            _ = StartDownloadQueueAsync(downloadQueue);
    }

    private void DownloadFileOnDownloadPaused(object? sender, DownloadFileEventArgs e)
    {
        var downloadQueueTask = _downloadQueueTasks.Find(task => task.DownloadFileId == e.Id);
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == downloadQueueTask?.DownloadQueueId);
        switch (downloadQueue)
        {
            case null:
                return;

            case { IncludePausedFiles: false, IsRunning: true }:
                _ = StartDownloadQueueAsync(downloadQueue);
                break;
        }
    }

    private async void DownloadFileServiceOnDataChanged(object? sender, EventArgs e)
    {
        // Update DownloadQueue data
        await LoadDownloadQueuesAsync(addDefaultDownloadQueue: false);

        // Remove DownloadFile from DownloadQueueTasks list when DownloadFile does not exist anymore
        var downloadFiles = _downloadFileService
            .DownloadFiles
            .ToList();

        var primaryKeys = downloadFiles
            .Select(df => df.Id)
            .ToList();

        var exceptDownloadQueueTasks = _downloadQueueTasks
            .Where(task => !primaryKeys.Contains(task.DownloadFileId))
            .ToList();

        foreach (var downloadQueueTask in exceptDownloadQueueTasks)
            _downloadQueueTasks.Remove(downloadQueueTask);
    }

    #endregion
}