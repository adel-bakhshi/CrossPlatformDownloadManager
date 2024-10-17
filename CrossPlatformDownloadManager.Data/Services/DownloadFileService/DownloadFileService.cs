using System.Collections.ObjectModel;
using AutoMapper;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;
using CrossPlatformDownloadManager.Utils;
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

        var primaryKeys = downloadFiles
            .Select(df => df.Id)
            .ToList();

        var deleteDownloadFiles = DownloadFiles
            .Where(df => !primaryKeys.Contains(df.Id))
            .ToList();

        foreach (var downloadFile in deleteDownloadFiles)
            await DeleteDownloadFileAsync(downloadFile, alsoDeleteFile: true, reloadData: false);

        var addDownloadFiles = downloadFiles
            .Where(df => DownloadFiles.All(odf => odf.Id != df.Id))
            .Select(df => _mapper.Map<DownloadFileViewModel>(df))
            .ToList();

        foreach (var downloadFile in addDownloadFiles)
            DownloadFiles.Add(downloadFile);

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

        await downloadFile.StopDownloadFileAsync(service);
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

        if (downloadFile.IsDownloading)
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

        var downloadFileTask = _downloadFileTasks.Find(task => task.Key == downloadFile.Id);
        if (downloadFileTask != null)
            _downloadFileTasks.Remove(downloadFileTask);

        await UpdateDownloadFileAsync(downloadFile);
    }

    #endregion
}