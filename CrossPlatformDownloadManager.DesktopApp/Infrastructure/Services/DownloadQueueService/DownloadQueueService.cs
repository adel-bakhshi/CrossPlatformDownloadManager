using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.PropertyChanged;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadQueueService;

public class DownloadQueueService : PropertyChangedBase, IDownloadQueueService
{
    #region Private Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IDownloadFileService _downloadFileService;

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

        _downloadQueues = [];
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

        var addDownloadQueues = downloadQueues
            .Where(dq => DownloadQueues.All(vm => vm.Id != dq.Id))
            .Select(dq => _mapper.Map<DownloadQueueViewModel>(dq))
            .ToList();

        foreach (var downloadQueue in addDownloadQueues)
            DownloadQueues.Add(downloadQueue);

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

    public async Task StopDownloadQueueAsync(DownloadQueueViewModel? viewModel)
    {
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == viewModel?.Id);
        if (downloadQueue == null)
            return;

        downloadQueue.IsRunning = false;

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

        var tasks = downloadFiles.ConvertAll(downloadFile => _downloadFileService.StopDownloadFileAsync(downloadFile));
        await Task.WhenAll(tasks);
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

        var downloadFiles = _downloadFileService
            .DownloadFiles
            .Where(df => primaryKeys.Contains(df.Id) && !df.IsCompleted && df.DownloadQueueId != downloadQueue.Id)
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

        if (downloadFiles.Count == 0 && viewModel.DownloadingFiles.Count == 0)
        {
            await StopDownloadQueueAsync(viewModel);
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

            _ = _downloadFileService.StartDownloadFileAsync(downloadFile);

            viewModel.DownloadingFiles.Add(downloadFile);
            taskIndex++;
        }

        if (viewModel.DownloadingFiles.Count == 0)
            await StopDownloadQueueAsync(viewModel);
    }

    private async Task<DownloadFinishedTaskValue?> DownloadFileFinishedTaskAsync(DownloadFileViewModel? viewModel)
    {
        try
        {
            var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == viewModel?.DownloadQueueId);
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