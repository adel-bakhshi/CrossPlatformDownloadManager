using System.Collections.ObjectModel;
using AutoMapper;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.DownloadFileService;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;
using CrossPlatformDownloadManager.Utils;
using PropertyChanged;

namespace CrossPlatformDownloadManager.Data.Services.DownloadQueueService;

[AddINotifyPropertyChangedInterface]
public class DownloadQueueService : IDownloadQueueService
{
    #region Private Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IDownloadFileService _downloadFileService;
    private readonly List<DownloadQueueTaskViewModel> _downloadQueueTasks;

    #endregion

    #region Events

    public event EventHandler? DataChanged;

    #endregion

    #region Properties

    public ObservableCollection<DownloadQueueViewModel> DownloadQueues { get; }

    #endregion

    public DownloadQueueService(IUnitOfWork unitOfWork, IMapper mapper, IDownloadFileService downloadFileService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _downloadFileService = downloadFileService;

        _downloadQueueTasks = [];

        DownloadQueues = [];
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
            await DeleteDownloadQueueAsync(downloadQueue: downloadQueue, reloadData: false);

        var downloadQueueViewModels = _mapper.Map<List<DownloadQueueViewModel>>(downloadQueues);
        foreach (var downloadQueue in downloadQueueViewModels)
        {
            var oldDownloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == downloadQueue.Id);
            if (oldDownloadQueue != null)
                oldDownloadQueue.UpdateViewModel(downloadQueue, nameof(oldDownloadQueue.Id));
            else
                DownloadQueues.Add(downloadQueue);
        }

        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task AddNewDownloadQueueAsync(DownloadQueue? downloadQueue, bool reloadData = true)
    {
        if (downloadQueue == null)
            return;

        await _unitOfWork.DownloadQueueRepository.AddAsync(downloadQueue);
        await _unitOfWork.SaveAsync();

        if (reloadData)
            await LoadDownloadQueuesAsync();
    }

    public async Task DeleteDownloadQueueAsync(DownloadQueueViewModel? downloadQueue, bool reloadData = true)
    {
        if (downloadQueue == null)
            return;

        var downloadQueueInDb = await _unitOfWork
            .DownloadQueueRepository
            .GetAsync(dq => dq.Id == downloadQueue.Id);

        if (downloadQueueInDb == null)
            return;

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

    public void StartDownloadQueue(DownloadQueueViewModel? downloadQueue)
    {
        if (downloadQueue == null || downloadQueue.DownloadFiles.Count == 0)
            return;
        
        downloadQueue.IsRunning = true;
        ContinueDownloadQueue(downloadQueue);
    }

    public async Task StopDownloadQueueAsync(DownloadQueueViewModel? downloadQueue)
    {
        if (downloadQueue == null)
            return;

        var downloadFiles = downloadQueue
            .DownloadFiles
            .Where(df => df.IsDownloading)
            .ToList();

        if (downloadFiles.Count == 0)
            return;

        foreach (var downloadFile in downloadFiles)
            await _downloadFileService.StopDownloadFileAsync(downloadFile);

        downloadQueue.IsRunning = false;
    }

    public async Task AddDownloadFileToDownloadQueueAsync(DownloadQueueViewModel? downloadQueue,
        DownloadFileViewModel? downloadFile)
    {
        if (downloadQueue == null || downloadFile == null)
            return;
    }

    public async Task RemoveDownloadFileFromDownloadQueueAsync(DownloadQueueViewModel? downloadQueue,
        DownloadFileViewModel? downloadFile)
    {
        if (downloadQueue == null || downloadFile == null)
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
        };

        await AddNewDownloadQueueAsync(downloadQueue, reloadData: false);
    }

    private void ContinueDownloadQueue(DownloadQueueViewModel? downloadQueue)
    {
        if (downloadQueue == null || downloadQueue.DownloadFiles.Count == 0)
            return;

        var downloadFiles = downloadQueue
            .DownloadFiles
            .Where(df => !df.IsDownloading)
            .OrderBy(df => df.DownloadQueuePriority)
            .ToList();

        var taskIndex = 0;
        while (_downloadQueueTasks.Count < downloadQueue.DownloadCountAtSameTime)
        {
            if (taskIndex == downloadFiles.Count)
                break;

            var downloadFile = downloadFiles[taskIndex];
            downloadFile.DownloadFinished += DownloadFileOnDownloadFinished;

            _downloadFileService.StartDownloadFileAsync(downloadFile);
            _downloadQueueTasks.Add(new DownloadQueueTaskViewModel
            {
                Key = downloadFile.Id,
                DownloadFile = downloadFile,
                DownloadQueueId = downloadQueue.Id,
            });

            taskIndex++;
        }
    }

    private async void DownloadFileOnDownloadFinished(object? sender, DownloadFileEventArgs e)
    {
        var task = _downloadQueueTasks.FirstOrDefault(t => t.Key == e.Id);
        if (task == null)
            return;

        task.DownloadFile!.DownloadFinished -= DownloadFileOnDownloadFinished;

        task.DownloadFile!.DownloadQueueId = null;
        await _downloadFileService.UpdateDownloadFileAsync(task.DownloadFile!);

        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == task.DownloadQueueId);
        ContinueDownloadQueue(downloadQueue);

        _downloadQueueTasks.Remove(task);
    }

    #endregion
}