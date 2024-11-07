using System.Collections.ObjectModel;
using AutoMapper;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using CrossPlatformDownloadManager.Utils.PropertyChanged;
using Downloader;

namespace CrossPlatformDownloadManager.Data.Services.DownloadFileService;

public class DownloadFileService : PropertyChangedBase, IDownloadFileService
{
    #region Private Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    private readonly List<DownloadFileTaskViewModel> _downloadFileTasks;

    private ObservableCollection<DownloadFileViewModel> _downloadFiles;

    #endregion

    #region Events

    public event EventHandler? DataChanged;

    #endregion

    #region Properties

    public ObservableCollection<DownloadFileViewModel> DownloadFiles
    {
        get => _downloadFiles;
        private set => SetField(ref _downloadFiles, value);
    }

    #endregion

    public DownloadFileService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;

        _downloadFileTasks = [];

        _downloadFiles = [];
    }

    public async Task LoadDownloadFilesAsync()
    {
        var downloadFiles = await _unitOfWork.DownloadFileRepository
            .GetAllAsync(includeProperties: ["Category.FileExtensions", "DownloadQueue"]);

        var viewModels = _mapper.Map<List<DownloadFileViewModel>>(downloadFiles);
        var primaryKeys = viewModels
            .Select(df => df.Id)
            .ToList();

        var deletedDownloadFiles = DownloadFiles
            .Where(df => !primaryKeys.Contains(df.Id))
            .ToList();

        foreach (var downloadFile in deletedDownloadFiles)
            await DeleteDownloadFileAsync(downloadFile, alsoDeleteFile: true, reloadData: false);

        primaryKeys = DownloadFiles
            .Select(df => df.Id)
            .ToList();

        var addedDownloadFiles = viewModels
            .Where(df => !primaryKeys.Contains(df.Id))
            .ToList();

        foreach (var downloadFile in addedDownloadFiles)
            DownloadFiles.Add(downloadFile);

        var previousDownloadFiles = viewModels
            .Except(addedDownloadFiles)
            .ToList();

        foreach (var downloadFile in previousDownloadFiles)
        {
            var previousDownloadFile = DownloadFiles.FirstOrDefault(df => df.Id == downloadFile.Id);
            if (previousDownloadFile == null)
                continue;

            previousDownloadFile.DownloadQueueId = downloadFile.DownloadQueueId;
            previousDownloadFile.DownloadQueueName = downloadFile.DownloadQueueName;
            previousDownloadFile.DownloadQueuePriority = downloadFile.DownloadQueuePriority;
        }

        OnPropertyChanged(nameof(DownloadFiles));
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task AddDownloadFileAsync(DownloadFile downloadFile)
    {
        await _unitOfWork.DownloadFileRepository.AddAsync(downloadFile);
        await _unitOfWork.SaveAsync();

        await LoadDownloadFilesAsync();
    }

    public async Task UpdateDownloadFileAsync(DownloadFile downloadFile)
    {
        await _unitOfWork.DownloadFileRepository.UpdateAsync(downloadFile);
        await _unitOfWork.SaveAsync();

        await LoadDownloadFilesAsync();
    }

    public async Task UpdateDownloadFileAsync(DownloadFileViewModel viewModel)
    {
        var downloadFileViewModel = DownloadFiles.FirstOrDefault(df => df.Id == viewModel.Id);
        if (downloadFileViewModel == null)
            return;

        var downloadFile = _mapper.Map<DownloadFile>(downloadFileViewModel);
        await UpdateDownloadFileAsync(downloadFile);
    }

    public async Task UpdateDownloadFilesAsync(List<DownloadFile> downloadFiles)
    {
        await _unitOfWork.DownloadFileRepository.UpdateAllAsync(downloadFiles);
        await _unitOfWork.SaveAsync();

        await LoadDownloadFilesAsync();
    }

    public async Task UpdateDownloadFilesAsync(List<DownloadFileViewModel> viewModels)
    {
        var downloadFileViewModels = viewModels
            .Select(vm => DownloadFiles.FirstOrDefault(df => df.Id == vm.Id))
            .Where(df => df != null)
            .ToList();

        var downloadFiles = _mapper.Map<List<DownloadFile>>(downloadFileViewModels);
        await UpdateDownloadFilesAsync(downloadFiles);
    }

    public async Task StartDownloadFileAsync(DownloadFileViewModel? viewModel)
    {
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == viewModel?.Id);
        if (downloadFile == null || downloadFile.IsCompleted || downloadFile.IsDownloading || downloadFile.IsPaused)
        {
            if (downloadFile is { IsPaused: true })
                ResumeDownloadFile(downloadFile);

            return;
        }

        var configuration = new DownloadConfiguration
        {
            ChunkCount = 8,
            MaximumBytesPerSecond = 64 * 1024,
            ParallelDownload = true,
        };

        var service = new DownloadService(configuration);

        _downloadFileTasks.Add(new DownloadFileTaskViewModel
        {
            Key = downloadFile.Id,
            Configuration = configuration,
            Service = service,
        });

        downloadFile.DownloadFinished += DownloadFileOnDownloadFinished;
        await downloadFile.StartDownloadFileAsync(service, configuration, _unitOfWork);
    }

    public async Task StopDownloadFileAsync(DownloadFileViewModel? viewModel)
    {
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == viewModel?.Id);
        if (downloadFile == null)
            return;

        var downloadFileTask = _downloadFileTasks.Find(task => task.Key == downloadFile.Id);
        var service = downloadFileTask?.Service;
        if (service == null || service.Status == DownloadStatus.Stopped)
            return;

        // Set the stopping flag to true
        downloadFileTask!.Stopping = true;
        await downloadFile.StopDownloadFileAsync(service);
        // Wait for the service to stop
        await EnsureStopDownloadFileCompletedAsync(downloadFile.Id);
    }

    public void ResumeDownloadFile(DownloadFileViewModel? viewModel)
    {
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == viewModel?.Id);
        if (downloadFile == null)
            return;

        var service = _downloadFileTasks.Find(task => task.Key == downloadFile.Id)?.Service;
        if (service == null)
            return;

        downloadFile.ResumeDownloadFile(service);
    }

    public void PauseDownloadFile(DownloadFileViewModel? viewModel)
    {
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == viewModel?.Id);
        if (downloadFile == null)
            return;

        var service = _downloadFileTasks.Find(task => task.Key == downloadFile.Id)?.Service;
        if (service == null)
            return;

        downloadFile.PauseDownloadFile(service);
    }

    public void LimitDownloadFileSpeed(DownloadFileViewModel? viewModel, long speed)
    {
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == viewModel?.Id);
        if (downloadFile == null)
            return;

        var configuration = _downloadFileTasks.Find(task => task.Key == downloadFile.Id)?.Configuration;
        if (configuration == null)
            return;

        configuration.MaximumBytesPerSecond = speed;
    }

    public async Task DeleteDownloadFileAsync(DownloadFileViewModel? viewModel, bool alsoDeleteFile,
        bool reloadData = true)
    {
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == viewModel?.Id);
        if (downloadFile == null)
            return;

        var downloadFileInDb = await _unitOfWork
            .DownloadFileRepository
            .GetAsync(where: df => df.Id == downloadFile.Id);

        if (downloadFile.IsDownloading || downloadFile.IsPaused)
            await StopDownloadFileAsync(downloadFile);

        var shouldReturn = false;
        if (downloadFileInDb == null)
        {
            DownloadFiles.Remove(downloadFile);
            OnPropertyChanged(nameof(DownloadFiles));

            shouldReturn = true;
        }

        if (alsoDeleteFile)
        {
            var saveLocation = downloadFileInDb?.SaveLocation ?? downloadFile.SaveLocation ?? string.Empty;
            var fileName = downloadFileInDb?.FileName ?? downloadFile.FileName ?? string.Empty;

            if (!saveLocation.IsNullOrEmpty() && !fileName.IsNullOrEmpty())
            {
                var filePath = Path.Combine(saveLocation, fileName);
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }

        if (shouldReturn)
            return;

        _unitOfWork.DownloadFileRepository.Delete(downloadFileInDb);
        await _unitOfWork.SaveAsync();

        if (reloadData)
            await LoadDownloadFilesAsync();
    }

    public async Task DeleteDownloadFilesAsync(List<DownloadFileViewModel>? viewModels, bool alsoDeleteFile,
        bool reloadData = true)
    {
        if (viewModels == null || viewModels.Count == 0)
            return;

        foreach (var viewModel in viewModels)
            await DeleteDownloadFileAsync(viewModel, alsoDeleteFile, reloadData: false);

        if (reloadData)
            await LoadDownloadFilesAsync();
    }

    public async Task RedownloadDownloadFileAsync(DownloadFileViewModel? viewModel)
    {
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == viewModel?.Id);
        if (downloadFile == null)
            return;

        downloadFile.Status = DownloadFileStatus.None;
        downloadFile.LastTryDate = null;
        downloadFile.DownloadProgress = 0;
        downloadFile.ElapsedTime = null;
        downloadFile.TimeLeft = null;
        downloadFile.TransferRate = null;
        downloadFile.DownloadPackage = null;

        if (!downloadFile.SaveLocation.IsNullOrEmpty() && !downloadFile.FileName.IsNullOrEmpty())
        {
            var filePath = Path.Combine(downloadFile.SaveLocation!, downloadFile.FileName!);
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        await UpdateDownloadFileAsync(downloadFile);
        _ = StartDownloadFileAsync(downloadFile);
    }

    #region Helpers

    private async void DownloadFileOnDownloadFinished(object? sender, DownloadFileEventArgs e)
    {
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == e.Id);
        if (downloadFile == null)
            return;

        downloadFile.DownloadFinished -= DownloadFileOnDownloadFinished;

        downloadFile.TimeLeft = null;
        downloadFile.TransferRate = null;

        if (downloadFile.IsCompleted)
        {
            downloadFile.DownloadPackage = null;
            downloadFile.DownloadQueueId = null;
            downloadFile.DownloadQueueName = null;
            downloadFile.DownloadQueuePriority = null;
        }

        await UpdateDownloadFileAsync(downloadFile);

        // Find task from the list
        var downloadFileTask = _downloadFileTasks.Find(task => task.Key == downloadFile.Id);
        if (downloadFileTask == null)
            return;

        // Check if the task is stopping
        if (downloadFileTask.Stopping)
        {
            // Set the stopping flag to false
            downloadFileTask.Stopping = false;
            // Set the stop operation finished flag to true
            downloadFileTask.StopOperationFinished = true;
        }
        else
        {
            // Remove the task from the list
            _downloadFileTasks.Remove(downloadFileTask);
        }
    }

    private async Task EnsureStopDownloadFileCompletedAsync(int downloadFileId)
    {
        // Find tasks from the list
        var downloadFileTask = _downloadFileTasks.Find(task => task.Key == downloadFileId);

        // Wait for the service to stop
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            // Check if the stop operation is finished
            while (downloadFileTask?.StopOperationFinished != true)
            {
                await Task.Delay(20);
                downloadFileTask = _downloadFileTasks.Find(task => task.Key == downloadFileId);
            }
        });

        // Remove the task
        if (downloadFileTask != null)
            _downloadFileTasks.Remove(downloadFileTask);
    }

    #endregion
}