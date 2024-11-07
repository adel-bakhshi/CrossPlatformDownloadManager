using System.Collections.ObjectModel;
using AutoMapper;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.DownloadFileService;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.PropertyChanged;

namespace CrossPlatformDownloadManager.Data.Services.DownloadQueueService;

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
            .GetAllAsync(includeProperties: ["DownloadFiles"]);

        var primaryKeys = downloadQueues
            .Select(df => df.Id)
            .ToList();

        var deletedDownloadQueues = DownloadQueues
            .Where(df => !primaryKeys.Contains(df.Id))
            .ToList();

        foreach (var downloadQueue in deletedDownloadQueues)
            await DeleteDownloadQueueAsync(viewModel: downloadQueue, reloadData: false);

        var addDownloadQueues = downloadQueues
            .Where(dq => DownloadQueues.All(odq => odq.Id != dq.Id))
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
            .GetAsync(dq => dq.Id == downloadQueue.Id, includeProperties: "DownloadFiles");

        if (downloadQueueInDb == null)
            return;

        var downloadFiles = _downloadFileService
            .DownloadFiles
            .Where(df => df.DownloadQueueId == downloadQueueInDb.Id)
            .ToList();

        foreach (var downloadFile in downloadFiles)
        {
            downloadFile.DownloadQueueId = null;
            downloadFile.DownloadQueueName = null;
            downloadFile.DownloadQueuePriority = null;
        }

        await _downloadFileService.UpdateDownloadFilesAsync(downloadFiles);

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

    public async Task UpdateDownloadQueueAsync(DownloadQueueViewModel? viewModel)
    {
        if (viewModel == null)
            return;

        var downloadQueue = _mapper.Map<DownloadQueue>(viewModel);
        await UpdateDownloadQueueAsync(downloadQueue);
    }

    public async Task UpdateDownloadQueuesAsync(List<DownloadQueue>? downloadQueues)
    {
        if (downloadQueues == null || downloadQueues.Count == 0)
            return;

        await _unitOfWork.DownloadQueueRepository.UpdateAllAsync(downloadQueues);
        await _unitOfWork.SaveAsync();

        await LoadDownloadQueuesAsync();
    }

    public async Task UpdateDownloadQueuesAsync(List<DownloadQueueViewModel>? viewModels)
    {
        if (viewModels == null || viewModels.Count == 0)
            return;

        var downloadQueues = _mapper.Map<List<DownloadQueue>>(viewModels);
        await UpdateDownloadQueuesAsync(downloadQueues);
    }

    public async Task StartDownloadQueueAsync(DownloadQueueViewModel? viewModel)
    {
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == viewModel?.Id);
        if (downloadQueue == null || downloadQueue.IsRunning)
            return;

        await downloadQueue.StartDownloadQueueAsync(_unitOfWork, _downloadFileService);
    }

    public async Task StopDownloadQueueAsync(DownloadQueueViewModel? viewModel)
    {
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == viewModel?.Id);
        if (downloadQueue == null)
            return;

        await downloadQueue.StopDownloadQueueAsync();
    }

    public async Task AddDownloadFileToDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel,
        DownloadFileViewModel? downloadFileViewModel)
    {
        var downloadQueue = DownloadQueues.FirstOrDefault(dq => dq.Id == downloadQueueViewModel?.Id);
        var downloadFile = _downloadFileService.DownloadFiles.FirstOrDefault(df => df.Id == downloadFileViewModel?.Id);
        if (downloadQueue == null || downloadFile == null || downloadFile.IsCompleted)
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
        var downloadFiles = _downloadFileService
            .DownloadFiles
            .Where(df => primaryKeys.Contains(df.Id) && !df.IsCompleted)
            .ToList();

        if (downloadQueue == null || downloadFiles.Count == 0)
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

        downloadFile.DownloadQueueId = null;
        await _downloadFileService.UpdateDownloadFileAsync(downloadFile);
        await LoadDownloadQueuesAsync(addDefaultDownloadQueue: false);
    }

    public async Task ChangeDefaultDownloadQueueAsync(DownloadQueueViewModel? viewModel)
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
        await UpdateDownloadQueuesAsync(viewModels);

        // Set given download queue as default and update it
        downloadQueue.IsDefault = true;
        await UpdateDownloadQueueAsync(downloadQueue);
    }

    public async Task ChangeLastSelectedDownloadQueueAsync(DownloadQueueViewModel? viewModel)
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
        await UpdateDownloadQueuesAsync(viewModels);

        // Set given download queue as last choice and update it
        downloadQueue.IsLastChoice = true;
        await UpdateDownloadQueueAsync(downloadQueue);
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

    private async void DownloadFileServiceOnDataChanged(object? sender, EventArgs e)
    {
        // Update DownloadQueue data
        await LoadDownloadQueuesAsync(addDefaultDownloadQueue: false);

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
                _ = downloadQueue.ContinueDownloadQueueAsync();
        }
    }

    #endregion
}