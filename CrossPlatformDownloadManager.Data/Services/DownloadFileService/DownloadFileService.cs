using System.Collections.ObjectModel;
using System.Net;
using AutoMapper;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.SettingsService;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;
using CrossPlatformDownloadManager.Data.ViewModels.Services;
using CrossPlatformDownloadManager.Data.ViewModels.Services.DownloadFileService;
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
    private readonly ISettingsService _settingsService;

    private readonly List<DownloadFileTaskViewModel> _downloadFileTasks;
    private bool _stopOperationIsRunning;
    private readonly List<int> _stopOperations;
    private readonly List<DownloadFileErrorEventArgs> _stopOperationExceptions;
    private readonly DispatcherTimer _downloadFinishedTimer;

    private ObservableCollection<DownloadFileViewModel> _downloadFiles = [];

    #endregion

    #region Properties

    public ObservableCollection<DownloadFileViewModel> DownloadFiles
    {
        get => _downloadFiles;
        private set => SetField(ref _downloadFiles, value);
    }

    public Dictionary<int, List<Func<DownloadFileViewModel?, Task<DownloadFinishedTaskValue?>>>> DownloadFinishedAsyncTasks { get; }

    public Dictionary<int, List<Func<DownloadFileViewModel?, DownloadFinishedTaskValue?>>> DownloadFinishedSyncTasks { get; }

    #endregion

    #region Events

    public event EventHandler? DataChanged;
    public event EventHandler<DownloadFileErrorEventArgs>? ErrorOccurred;

    #endregion

    public DownloadFileService(IUnitOfWork unitOfWork, IMapper mapper, ISettingsService settingsService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _settingsService = settingsService;

        _downloadFileTasks = [];
        _stopOperations = [];
        _stopOperationExceptions = [];
        _downloadFinishedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _downloadFinishedTimer.Tick += DownloadFinishedTimerOnTick;

        DownloadFiles = [];
        DownloadFinishedAsyncTasks = [];
        DownloadFinishedSyncTasks = [];
    }

    public async Task LoadDownloadFilesAsync()
    {
        var downloadFiles = await _unitOfWork
            .DownloadFileRepository
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

    public async Task<ServiceResultViewModel<DownloadFileViewModel>> AddDownloadFileAsync(DownloadFileViewModel viewModel)
    {
        var result = new ServiceResultViewModel<DownloadFileViewModel>();

        var category = await _unitOfWork
            .CategoryRepository
            .GetAsync(where: c => c.Id == viewModel.CategoryId, includeProperties: ["CategorySaveDirectory"]);

        var downloadFile = new DownloadFile
        {
            Url = viewModel.Url!,
            FileName = viewModel.FileName!,
            DownloadQueueId = null,
            Size = viewModel.Size!.Value,
            Description = viewModel.Description,
            Status = DownloadFileStatus.None,
            LastTryDate = null,
            DateAdded = DateTime.Now,
            DownloadQueuePriority = null,
            CategoryId = category!.Id,
            SaveLocation = category.CategorySaveDirectory!.SaveDirectory,
        };

        await _unitOfWork.DownloadFileRepository.AddAsync(downloadFile);
        await _unitOfWork.SaveAsync();

        await LoadDownloadFilesAsync();

        result.IsSuccess = true;
        result.Result = DownloadFiles.FirstOrDefault(df => df.Id == downloadFile.Id);
        return result;
    }

    public async Task<ServiceResultViewModel<UrlDetailsViewModel>> GetUrlDetailsAsync(string? url)
    {
        var result = new ServiceResultViewModel<UrlDetailsViewModel> { Result = new UrlDetailsViewModel() };

        // Make sure url is valid
        url = url?.Replace("\\", "/").Trim();
        if (!url.CheckUrlValidation())
        {
            result.Result.IsUrlValid = false;
            return result;
        }

        result.Result.Url = url!;

        // Find a download file with the same url
        var duplicateDownloadFile = DownloadFiles.FirstOrDefault(df => !df.Url.IsNullOrEmpty() && df.Url!.Equals(url));
        result.Result.IsUrlDuplicate = duplicateDownloadFile != null;

        // Create an instance of HttpClient
        using var httpClient = new HttpClient();
        // Send a HEAD request to get the headers only
        using var request = new HttpRequestMessage(HttpMethod.Head, url);
        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        // Set file flag to true
        var isFile = true;
        // Check if the Content-Type indicates a file
        var contentType = response.Content.Headers.ContentType?.MediaType?.ToLower() ?? string.Empty;
        if (!contentType.StartsWith("application/") &&
            !contentType.StartsWith("image/") &&
            !contentType.StartsWith("video/") &&
            !contentType.StartsWith("audio/") &&
            !contentType.StartsWith("text/"))
        {
            isFile = false;

            // Check Content-Disposition header
            if (response.Content.Headers.ContentDisposition != null)
            {
                var dispositionType = response.Content.Headers.ContentDisposition.DispositionType;
                if (dispositionType.Equals("attachment", StringComparison.OrdinalIgnoreCase))
                    isFile = true;
            }
        }

        // Save is file flag
        result.Result.IsFile = isFile;
        // Make sure the url point to a file
        if (!isFile)
            return result;

        string? fileName = null;
        // Get file name from header if possible
        if (response.Content.Headers.ContentDisposition != null)
            fileName = response.Content.Headers.ContentDisposition.FileName?.Trim('\"') ?? string.Empty;

        // Get file name from x-suggested-filename header if possible
        if (fileName.IsNullOrEmpty())
            fileName = response.Content.Headers.TryGetValues("x-suggested-filename", out var suggestedFileNames) ? suggestedFileNames.FirstOrDefault() : string.Empty;

        // Fallback to using the URL to guess the file name if Content-Disposition is not present
        if (fileName.IsNullOrEmpty())
            fileName = url!.GetFileName();

        // Set file name
        result.Result.FileName = fileName?.Trim() ?? string.Empty;
        // Set file size
        result.Result.FileSize = response.Content.Headers.ContentLength ?? 0;

        // find category item by file extension
        var extension = Path.GetExtension(result.Result.FileName);
        // Get all custom categories
        var customCategories = await _unitOfWork
            .CategoryRepository
            .GetAllAsync(where: c => !c.IsDefault, includeProperties: "FileExtensions");

        // Find file extension by extension
        CategoryFileExtension? fileExtension;
        var customCategory = customCategories.Find(c => c.FileExtensions.Any(fe => fe.Extension.Equals(extension, StringComparison.CurrentCultureIgnoreCase)));
        if (customCategory != null)
        {
            fileExtension = customCategory
                .FileExtensions
                .FirstOrDefault(fe => fe.Extension.Equals(extension, StringComparison.CurrentCultureIgnoreCase));
        }
        else
        {
            fileExtension = await _unitOfWork
                .CategoryFileExtensionRepository
                .GetAsync(where: fe => fe.Extension.ToLower() == extension.ToLower(), includeProperties: "Category");
        }

        // Find category by category file extension or choose general category
        if (fileExtension != null)
        {
            var category = customCategory ?? fileExtension.Category;
            result.Result.Category = category == null ? null : _mapper.Map<CategoryViewModel>(category);
        }
        else
        {
            var category = await _unitOfWork
                .CategoryRepository
                .GetAsync(where: c => c.Title.ToLower() == Constants.GeneralCategoryTitle.ToLower());

            result.Result.Category = category == null ? null : _mapper.Map<CategoryViewModel>(category);
        }

        result.IsSuccess = true;
        return result;
    }

    public async Task<ServiceResultViewModel> ValidateDownloadFileAsync(DownloadFileViewModel viewModel)
    {
        var result = new ServiceResultViewModel();

        // Check url validation
        if (viewModel.Url.IsNullOrEmpty() || !viewModel.Url.CheckUrlValidation())
        {
            result.Header = "Url";
            result.Message = "Please enter a valid url.";
            return result;
        }

        // Get category from database
        var category = await _unitOfWork
            .CategoryRepository
            .GetAsync(where: c => c.Id == viewModel.CategoryId, includeProperties: "CategorySaveDirectory");

        // Check category
        if (category == null)
        {
            result.Header = "Category";
            result.Message = "Please choose a category for your file.";
            return result;
        }

        // Check category save directory
        if (category.CategorySaveDirectory == null)
        {
            result.Header = "Save location";
            result.Message = "Can't find save location for this category.";
            return result;
        }

        // Check file name
        if (viewModel.FileName.IsNullOrEmpty())
        {
            result.Header = "File name";
            result.Message = "Please enter a file name.";
            return result;
        }

        // Check file extension
        if (!viewModel.FileName.HasFileExtension())
        {
            result.Header = "File name";
            result.Message = "File type is null or invalid. Please choose correct file type like .exe or .zip.";
            return result;
        }

        // Check Url point to a file and file size is greater than 0
        if (viewModel.Size is null or <= 0)
        {
            result.Header = "No file detected";
            result.Message = "It seems the URL does not point to a file. Make sure the URL points to a file and try again.";
            return result;
        }

        result.IsSuccess = true;
        return result;
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
        if (downloadFiles.Count == 0)
            return;

        await _unitOfWork.DownloadFileRepository.UpdateAllAsync(downloadFiles);
        await _unitOfWork.SaveAsync();

        await LoadDownloadFilesAsync();
    }

    public async Task UpdateDownloadFilesAsync(List<DownloadFileViewModel> viewModels)
    {
        if (viewModels.Count == 0)
            return;

        var downloadFileViewModels = viewModels
            .Select(vm => DownloadFiles.FirstOrDefault(df => df.Id == vm.Id))
            .Where(df => df != null)
            .ToList();

        if (downloadFileViewModels.Count == 0)
            return;

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
            ParallelDownload = true
        };

        switch (_settingsService.Settings.ProxyMode)
        {
            case ProxyMode.DisableProxy:
                break;

            case ProxyMode.UseSystemProxySettings:
            {
                var systemProxy = WebRequest.DefaultWebProxy;
                if (systemProxy == null)
                    break;

                configuration.RequestConfiguration.Proxy = systemProxy;
                break;
            }

            case ProxyMode.UseCustomProxy:
            {
                var activeProxy = _settingsService
                    .Settings
                    .Proxies
                    .FirstOrDefault(p => p.IsActive);

                if (activeProxy == null)
                    break;

                configuration.RequestConfiguration.Proxy = new WebProxy
                {
                    Address = new Uri(activeProxy.GetProxyUri()),
                    Credentials = new NetworkCredential(activeProxy.Username, activeProxy.Password),
                };

                break;
            }

            default:
                throw new InvalidOperationException("Invalid proxy mode.");
        }

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

    public async Task StopDownloadFileAsync(DownloadFileViewModel? viewModel, bool ensureStopped = false)
    {
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == viewModel?.Id);
        if (downloadFile == null)
            return;

        var downloadFileTask = _downloadFileTasks.Find(task => task.Key == downloadFile.Id);
        var service = downloadFileTask?.Service;
        if (service == null || service.Status == DownloadStatus.Stopped)
            return;

        downloadFile.StopDownloadFile(service);

        // Wait for the download to stop
        if (ensureStopped)
        {
            // Set the stopping flag to true
            downloadFileTask!.Stopping = true;
            await EnsureDownloadFileStoppedAsync(downloadFile.Id);
        }
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
            await StopDownloadFileAsync(downloadFile, ensureStopped: true);

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

        await _unitOfWork.DownloadFileRepository.DeleteAsync(downloadFileInDb);
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

    public string GetDownloadSpeed()
    {
        var downloadSpeed = DownloadFiles
            .Where(df => df.IsDownloading)
            .Sum(df => df.TransferRate ?? 0);

        return downloadSpeed.ToFileSize();
    }

    #region Helpers

    private void DownloadFileOnDownloadFinished(object? sender, DownloadFileEventArgs e)
    {
        try
        {
            // Add download file id to stop operations
            _stopOperations.Add(e.Id);

            // Check if stop operation is running or not
            // If stop operation is not running, start it
            if (!_stopOperationIsRunning)
            {
                // Restart download finished timer
                _downloadFinishedTimer.Stop();
                _downloadFinishedTimer.Start();
            }

            if (e.Error != null)
                throw e.Error;
        }
        catch (Exception ex)
        {
            var eventArgs = new DownloadFileErrorEventArgs
            {
                Id = e.Id,
                Error = ex
            };

            ErrorOccurred?.Invoke(this, eventArgs);
        }
    }

    private async void DownloadFinishedTimerOnTick(object? sender, EventArgs e)
    {
        try
        {
            // Stop download finished timer
            _downloadFinishedTimer.Stop();
            if (_stopOperationIsRunning)
                return;

            // Start stop operations
            await StopOperationsAsync();

            if (_stopOperationExceptions.Count <= 0)
                return;

            foreach (var eventArgs in _stopOperationExceptions)
                ErrorOccurred?.Invoke(this, eventArgs);

            _stopOperationExceptions.Clear();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async Task StopOperationsAsync()
    {
        _stopOperationIsRunning = true;
        while (_stopOperations.Count > 0)
        {
            var id = _stopOperations[0];
            var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == id);
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

            List<DownloadFinishedTaskValue?> taskValues = [];
            if (DownloadFinishedAsyncTasks.TryGetValue(downloadFile.Id, out var asyncTasks))
            {
                foreach (var task in asyncTasks)
                {
                    var value = await task.Invoke(downloadFile);
                    taskValues.Add(value);
                }
            }

            DownloadFinishedAsyncTasks.Remove(downloadFile.Id);

            if (DownloadFinishedSyncTasks.TryGetValue(downloadFile.Id, out var syncTasks))
                taskValues.AddRange(syncTasks.Select(task => task.Invoke(downloadFile)));

            DownloadFinishedSyncTasks.Remove(downloadFile.Id);

            var exception = taskValues.Find(v => v?.Exception != null)?.Exception;
            if (exception != null)
            {
                var eventArgs = new DownloadFileErrorEventArgs
                {
                    Id = downloadFile.Id,
                    Error = exception
                };

                _stopOperationExceptions.Add(eventArgs);
            }

            var allTasksAreNull = taskValues.TrueForAll(v => v == null);
            var updateNeededTaskIsExist = taskValues.Exists(v => v?.UpdateDownloadFile == true);
            if (allTasksAreNull || updateNeededTaskIsExist)
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

            _stopOperations.Remove(id);
        }

        _stopOperationIsRunning = false;
    }

    private async Task EnsureDownloadFileStoppedAsync(int downloadFileId)
    {
        // Wait for the service to stop
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            // Find tasks from the list
            var downloadFileTask = _downloadFileTasks.Find(task => task.Key == downloadFileId);

            // Check if the stop operation is finished
            while (downloadFileTask?.StopOperationFinished != true)
            {
                await Task.Delay(100);
                downloadFileTask = _downloadFileTasks.Find(task => task.Key == downloadFileId);
            }

            // Remove the task
            _downloadFileTasks.Remove(downloadFileTask);
        });
    }

    #endregion
}