using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
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
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.CategoryService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService.Models;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.SettingsService;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.CustomEventArgs;
using CrossPlatformDownloadManager.Utils.Enums;
using CrossPlatformDownloadManager.Utils.PropertyChanged;
using Emik;
using Microsoft.Extensions.DependencyInjection;
using MultipartDownloader.Core;
using RolandK.AvaloniaExtensions.DependencyInjection;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService;

public class DownloadFileService : PropertyChangedBase, IDownloadFileService
{
    #region Private Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ISettingsService _settingsService;
    private readonly ICategoryService _categoryService;

    private readonly List<DownloadFileTaskViewModel> _downloadFileTasks;
    private bool _stopOperationIsRunning;
    private readonly List<int> _stopOperations;
    private readonly DispatcherTimer _downloadFinishedTimer;

    private ObservableCollection<DownloadFileViewModel> _downloadFiles = [];

    #endregion

    #region Properties

    public ObservableCollection<DownloadFileViewModel> DownloadFiles
    {
        get => _downloadFiles;
        private set => SetField(ref _downloadFiles, value);
    }

    public Dictionary<int, List<Func<DownloadFileViewModel?, Task<DownloadFinishedTaskValue?>>>>
        DownloadFinishedAsyncTasks { get; }

    public Dictionary<int, List<Func<DownloadFileViewModel?, DownloadFinishedTaskValue?>>> DownloadFinishedSyncTasks { get; }

    #endregion

    #region Events

    public event EventHandler? DataChanged;

    #endregion

    public DownloadFileService(IUnitOfWork unitOfWork, IMapper mapper, ISettingsService settingsService,
        ICategoryService categoryService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _settingsService = settingsService;
        _categoryService = categoryService;

        _downloadFileTasks = [];
        _stopOperations = [];
        _downloadFinishedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _downloadFinishedTimer.Tick += DownloadFinishedTimerOnTick;

        DownloadFiles = [];
        DownloadFinishedAsyncTasks = [];
        DownloadFinishedSyncTasks = [];
    }

    public async Task LoadDownloadFilesAsync()
    {
        // Get all download files from database
        var downloadFiles = await _unitOfWork
            .DownloadFileRepository
            .GetAllAsync(includeProperties: ["Category.FileExtensions", "DownloadQueue"]);

        // Convert download files to view models
        var viewModels = _mapper.Map<List<DownloadFileViewModel>>(downloadFiles);
        var primaryKeys = viewModels.ConvertAll(df => df.Id);

        // Find download files that removed
        var deletedDownloadFiles = DownloadFiles
            .Where(df => !primaryKeys.Contains(df.Id))
            .ToList();

        // Remove download files from list
        foreach (var downloadFile in deletedDownloadFiles)
            await DeleteDownloadFileAsync(downloadFile, alsoDeleteFile: true, reloadData: false);

        // Find primary keys
        primaryKeys = DownloadFiles.Select(df => df.Id).ToList();
        // Find new download files
        var addedDownloadFiles = viewModels.Where(df => !primaryKeys.Contains(df.Id)).ToList();
        // Add new download files
        foreach (var downloadFile in addedDownloadFiles)
            DownloadFiles.Add(downloadFile);

        // Find old download files
        var previousDownloadFiles = viewModels.Except(addedDownloadFiles).ToList();
        // Update required data in old download files
        foreach (var downloadFile in previousDownloadFiles)
        {
            // Make sure old download file exists
            var previousDownloadFile = DownloadFiles.FirstOrDefault(df => df.Id == downloadFile.Id);
            if (previousDownloadFile == null)
                continue;

            // Update data
            previousDownloadFile.DownloadQueueId = downloadFile.DownloadQueueId;
            previousDownloadFile.DownloadQueueName = downloadFile.DownloadQueueName;
            previousDownloadFile.DownloadQueuePriority = downloadFile.DownloadQueuePriority;
        }

        // Notify download files changed
        OnPropertyChanged(nameof(DownloadFiles));
        // Raise changed event
        DataChanged?.Invoke(this, EventArgs.Empty);
        // Log information
        Log.Information("Download files loaded successfully.");
    }

    public async Task<DownloadFileViewModel?> AddDownloadFileAsync(DownloadFileViewModel viewModel,
        bool isUrlDuplicate = false,
        DuplicateDownloadLinkAction? duplicateAction = null,
        bool isFileNameDuplicate = false,
        bool startDownloading = false)
    {
        // Validate download file
        var isValid = await ValidateDownloadFileAsync(viewModel);
        if (!isValid)
            return null;

        // Find category
        var category = _categoryService
            .Categories
            .FirstOrDefault(c => c.Id == viewModel.CategoryId);

        // Set save location
        var saveLocation = _settingsService.Settings.DisableCategories
            ? _settingsService.Settings.GlobalSaveLocation ?? string.Empty
            : category?.CategorySaveDirectory?.SaveDirectory ?? string.Empty;

        // Make sure category and save location are not empty
        if (category == null || saveLocation.IsStringNullOrEmpty())
            throw new InvalidOperationException("Unable to find the file category or storage location. Please try again.");

        // Create an instance of DownloadFile
        var downloadFile = new DownloadFile
        {
            Url = viewModel.Url!,
            FileName = viewModel.FileName!,
            DownloadQueueId = viewModel.DownloadQueueId,
            Size = viewModel.Size!.Value,
            IsSizeUnknown = viewModel.IsSizeUnknown,
            Description = viewModel.Description,
            Status = viewModel.Status ?? DownloadFileStatus.None,
            LastTryDate = null,
            DateAdded = DateTime.Now,
            DownloadQueuePriority = viewModel.DownloadQueuePriority,
            CategoryId = category.Id,
            SaveLocation = saveLocation,
            DownloadProgress = viewModel.DownloadProgress is > 0 ? viewModel.DownloadProgress.Value : 0,
            DownloadPackage = viewModel.DownloadPackage
        };

        // Handle duplicate download links
        DownloadFileViewModel? result = null;
        if (isUrlDuplicate)
        {
            // Make sure duplicate action has value
            if (duplicateAction is null or DuplicateDownloadLinkAction.LetUserChoose)
                throw new InvalidOperationException("When you want to add a duplicate URL, you must specify how to handle this URL.");

            // Handle duplicate
            switch (duplicateAction.Value)
            {
                // Handle duplicate with a number at the end of the file name
                case DuplicateDownloadLinkAction.DuplicateWithNumber:
                {
                    // Get new file name with number at the end
                    var newFileName = GetNewFileName(downloadFile.Url, downloadFile.FileName, downloadFile.SaveLocation);
                    // Make sure file name is not empty
                    if (newFileName.IsStringNullOrEmpty())
                        throw new InvalidOperationException("New file name for duplicate link is null or empty.");

                    // Change download file name
                    downloadFile.FileName = newFileName;
                    break;
                }

                // Handle duplicate and overwrite existing file
                case DuplicateDownloadLinkAction.OverwriteExisting:
                {
                    // Delete previous file and replace new one
                    await DeleteDuplicateDownloadFilesAsync(downloadFile.Url);
                    break;
                }

                // Handle duplicate with showing complete download dialog or resume file
                case DuplicateDownloadLinkAction.ShowCompleteDialogOrResume:
                {
                    // Show complete dialog or resume file
                    result = await ShowCompleteDownloadDialogOrResumeAsync(downloadFile.Url);
                    break;
                }

                // At this point, duplicate download action is invalid
                case DuplicateDownloadLinkAction.LetUserChoose:
                default:
                    throw new InvalidOperationException("Duplicate download link action is invalid.");
            }
        }

        // If duplicate download action sets to ShowCompleteDialogOrResume, we have to pass the existing download file
        if (result != null)
            return result;

        // Make sure each file has a unique name
        if (isFileNameDuplicate)
        {
            // Find duplicate download file
            var duplicateDownloadFile = DownloadFiles
                .FirstOrDefault(df => !df.FileName.IsStringNullOrEmpty()
                                      && df.FileName!.Equals(downloadFile.FileName)
                                      && !df.SaveLocation.IsStringNullOrEmpty()
                                      && df.SaveLocation!.Equals(downloadFile.SaveLocation));

            // Find download file on system and if it exists, we have to choose another name for download file
            var duplicateFilePath = Path.Combine(downloadFile.SaveLocation, downloadFile.FileName);
            if (duplicateDownloadFile != null || File.Exists(duplicateFilePath))
            {
                // Get new file name and make sure it's not empty
                var newFileName = GetNewFileName(downloadFile.FileName, downloadFile.SaveLocation);
                if (newFileName.IsStringNullOrEmpty())
                    throw new InvalidOperationException("New file name for duplicate file is null or empty.");

                // Change file name
                downloadFile.FileName = newFileName;
            }
        }

        // Add new download file and save it
        await _unitOfWork.DownloadFileRepository.AddAsync(downloadFile);
        await _unitOfWork.SaveAsync();

        // Reload data
        await LoadDownloadFilesAsync();

        // Find download file in data
        result = DownloadFiles.FirstOrDefault(df => df.Id == downloadFile.Id);
        // Start download when necessary
        if (startDownloading)
            _ = StartDownloadFileAsync(result);

        // Return new download file
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

    public async Task StartDownloadFileAsync(DownloadFileViewModel? viewModel, bool showWindow = true)
    {
        try
        {
            // Find download file
            var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == viewModel?.Id);
            // Make sure download file is in a valid state
            if (downloadFile == null || downloadFile.IsCompleted || downloadFile.IsDownloading || downloadFile.IsPaused || downloadFile.IsStopping)
            {
                // If the download file is paused, resume it
                if (downloadFile is { IsPaused: true, IsStopping: false })
                    ResumeDownloadFile(downloadFile);

                return;
            }

            // Check disk size before starting download
            var hasEnoughSpace = await CheckDiskSpaceAsync(downloadFile);
            if (!hasEnoughSpace)
                return;

            // Create download configurations
            var configuration = new DownloadConfiguration
            {
                ChunkCount = _settingsService.Settings.MaximumConnectionsCount,
                MaximumBytesPerSecond = GetMaximumBytesPerSecond(),
                ParallelDownload = true,
                MaximumMemoryBufferBytes = Constants.MaximumMemoryBufferBytes,
                ReserveStorageSpaceBeforeStartingDownload = true,
                ChunkFilesOutputDirectory = string.Empty, // TODO: Set chunk files output directory
                MaxRestartWithoutClearTempFile = 5
            };

            if (configuration.ChunkFilesOutputDirectory.IsStringNullOrEmpty())
                throw new InvalidOperationException();

            // Get proxy and if possible, use it
            var proxy = _settingsService.GetProxy();
            if (proxy != null)
                configuration.RequestConfiguration.Proxy = proxy;

            // Create download service with configuration
            var service = new DownloadService(configuration);
            // Create download file task
            var downloadFileTask = new DownloadFileTaskViewModel
            {
                Key = downloadFile.Id,
                Configuration = configuration,
                Service = service
            };

            // Create download window
            downloadFileTask.CreateDownloadWindow(downloadFile, showWindow);
            // Add download file task to tasks
            _downloadFileTasks.Add(downloadFileTask);

            // Bind events
            downloadFile.DownloadFinished += DownloadFileOnDownloadFinished;
            downloadFile.DownloadStopped += DownloadFileOnDownloadStopped;
            // Start download
            await downloadFile.StartDownloadFileAsync(service, configuration, _unitOfWork, proxy);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while downloading a file. Error message: {ErrorMessage}", ex.Message);
        }
    }

    public async Task StopDownloadFileAsync(DownloadFileViewModel? viewModel, bool ensureStopped = false,
        bool playSound = true)
    {
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == viewModel?.Id);
        if (downloadFile == null)
            return;

        var downloadFileTask = _downloadFileTasks.Find(task => task.Key == downloadFile.Id);
        var service = downloadFileTask?.Service;
        if (service == null || service.Status == DownloadStatus.Stopped || downloadFile.IsStopping)
            return;

        downloadFile.PlayStopSound = playSound;
        await downloadFile.StopDownloadFileAsync(service);

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

        var downloadFileTask = _downloadFileTasks.Find(task => task.Key == downloadFile.Id);
        if (downloadFileTask?.Service == null)
            return;

        downloadFile.ResumeDownloadFile(downloadFileTask.Service);
        ShowOrFocusDownloadWindow(downloadFile);
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

    public async Task DeleteDownloadFileAsync(DownloadFileViewModel? viewModel, bool alsoDeleteFile, bool reloadData = true)
    {
        // Find download file
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == viewModel?.Id);
        if (downloadFile == null)
            return;

        // Find original download file in database
        var downloadFileInDb = await _unitOfWork
            .DownloadFileRepository
            .GetAsync(where: df => df.Id == downloadFile.Id);

        // Stop download file if downloading or paused
        if (downloadFile.IsDownloading || downloadFile.IsPaused)
            await StopDownloadFileAsync(downloadFile, ensureStopped: true);

        // If the download file does not exist in the database but does exist in the application,
        // only the file in the application needs to be deleted and there is no need to delete the file in the database as well
        var shouldReturn = false;
        if (downloadFileInDb == null)
        {
            DownloadFiles.Remove(downloadFile);
            OnPropertyChanged(nameof(DownloadFiles));

            shouldReturn = true;
        }

        // Remove download file from storage
        if (alsoDeleteFile)
        {
            var saveLocation = downloadFileInDb?.SaveLocation ?? downloadFile.SaveLocation ?? string.Empty;
            var fileName = downloadFileInDb?.FileName ?? downloadFile.FileName ?? string.Empty;

            if (!saveLocation.IsStringNullOrEmpty() && !fileName.IsStringNullOrEmpty())
            {
                var filePath = Path.Combine(saveLocation, fileName);
                if (File.Exists(filePath))
                    await Rubbish.MoveAsync(filePath);
            }
        }

        // Remove backup file
        downloadFile.RemoveBackup();
        // Check if the download file needs to be deleted from the database
        if (shouldReturn)
            return;

        // Remove download file from database
        await _unitOfWork.DownloadFileRepository.DeleteAsync(downloadFileInDb);
        await _unitOfWork.SaveAsync();

        // Update downloads list when necessary
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

    public async Task RedownloadDownloadFileAsync(DownloadFileViewModel? viewModel, bool showWindow = true)
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

        if (!downloadFile.SaveLocation.IsStringNullOrEmpty() && !downloadFile.FileName.IsStringNullOrEmpty())
        {
            var filePath = Path.Combine(downloadFile.SaveLocation!, downloadFile.FileName!);
            if (File.Exists(filePath))
                await Rubbish.MoveAsync(filePath);
        }

        await UpdateDownloadFileAsync(downloadFile);
        _ = StartDownloadFileAsync(downloadFile, showWindow);
    }

    public string GetDownloadSpeed()
    {
        var downloadSpeed = DownloadFiles
            .Where(df => df.IsDownloading)
            .Sum(df => df.TransferRate ?? 0);

        return downloadSpeed.ToFileSize();
    }

    public async Task<UrlDetailsResultViewModel> GetUrlDetailsAsync(string? url, CancellationToken cancellationToken)
    {
        var result = new UrlDetailsResultViewModel();

        // Make sure url is valid
        url = url?.Replace("\\", "/").Trim();
        result.IsUrlValid = url.CheckUrlValidation();
        if (!result.IsUrlValid)
            return result;

        result.Url = url!;

        // Create an instance of HttpClient
        using var handler = new HttpClientHandler();
        var proxy = _settingsService.GetProxy();
        if (proxy != null)
        {
            handler.Proxy = proxy;
            handler.UseProxy = true;
        }

        using var httpClient = new HttpClient(handler);
        // Send a HEAD request to get the headers only
        using var request = new HttpRequestMessage(HttpMethod.Head, url);
        using var response = await httpClient.SendAsync(request, cancellationToken);
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
        result.IsFile = isFile;
        // Make sure the url point to a file
        if (!isFile)
            return result;

        string? fileName = null;
        // Get file name from header if possible
        if (response.Content.Headers.ContentDisposition != null)
            fileName = response.Content.Headers.ContentDisposition.FileName?.Trim('\"') ?? string.Empty;

        // Get file name from x-suggested-filename header if possible
        if (fileName.IsStringNullOrEmpty())
        {
            fileName = response.Content.Headers.TryGetValues("x-suggested-filename", out var suggestedFileNames)
                ? suggestedFileNames.FirstOrDefault()
                : string.Empty;
        }

        // Fallback to using the URL to guess the file name if Content-Disposition is not present
        if (fileName.IsStringNullOrEmpty())
            fileName = url!.GetFileName();

        // Set file name
        result.FileName = fileName?.Trim() ?? string.Empty;
        // Check if file size unknown
        result.IsFileSizeUnknown = response.Content.Headers.ContentLength == null;
        // Set file size
        result.FileSize = result.IsFileSizeUnknown ? 0 : response.Content.Headers.ContentLength ?? 0;

        // find category item by file extension
        var extension = Path.GetExtension(result.FileName);
        // Get all custom categories
        var customCategories = _categoryService
            .Categories
            .Where(c => !c.IsDefault)
            .ToList();

        // Find file extension by extension
        CategoryFileExtensionViewModel? fileExtension;
        var customCategory = customCategories
            .Find(c => c.FileExtensions.Any(fe => fe.Extension.Equals(extension, StringComparison.CurrentCultureIgnoreCase)));

        if (customCategory != null)
        {
            fileExtension = customCategory
                .FileExtensions
                .FirstOrDefault(fe => fe.Extension.Equals(extension, StringComparison.CurrentCultureIgnoreCase));
        }
        else
        {
            fileExtension = _categoryService
                .Categories
                .SelectMany(c => c.FileExtensions)
                .FirstOrDefault(fe => fe.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase));
        }

        // Find category by domain
        var siteDomain = result.Url.GetDomainFromUrl();
        if (!siteDomain.IsStringNullOrEmpty())
        {
            result.Category = _categoryService
                .Categories
                .FirstOrDefault(c => c.AutoAddedLinksFromSitesList.Contains(siteDomain!));
        }

        // Find category by file extension
        if (result.Category == null)
        {
            // Find category by category file extension or choose general category
            if (fileExtension != null)
            {
                result.Category = customCategory ?? fileExtension.Category;
            }
            else
            {
                result.Category = _categoryService
                    .Categories
                    .FirstOrDefault(c =>
                        c.Title.Equals(Constants.GeneralCategoryTitle, StringComparison.OrdinalIgnoreCase));
            }
        }

        // Find a download file with the same url and handle it
        result.IsUrlDuplicate = DownloadFiles.FirstOrDefault(df => !df.Url.IsStringNullOrEmpty() && df.Url!.Equals(url)) != null;
        result.IsFileNameDuplicate = DownloadFiles
            .FirstOrDefault(df => !df.FileName.IsStringNullOrEmpty()
                                  && df.FileName!.Equals(fileName)
                                  && !df.SaveLocation.IsStringNullOrEmpty() &&
                                  df.SaveLocation!.Equals(result.Category!.CategorySaveDirectory!.SaveDirectory)) != null;

        return result;
    }

    public ValidateUrlDetailsViewModel ValidateUrlDetails(UrlDetailsResultViewModel viewModel)
    {
        var isValid = true;
        string title = string.Empty, message = string.Empty;
        if (!viewModel.IsUrlValid)
        {
            isValid = false;
            title = "Url is not valid";
            message = "The link you are trying to download is not valid. Please check the link and try again.";
        }
        else if (!viewModel.IsFile || viewModel is { FileSize: 0, IsFileSizeUnknown: false })
        {
            isValid = false;
            title = "Link is not downloadable";
            message = "The link you selected does not return a downloadable file. This could be due to an incorrect link, restricted access, or unsupported content.";
        }
        else if (viewModel.Category?.CategorySaveDirectory == null || viewModel.Category.CategorySaveDirectory.SaveDirectory.IsStringNullOrEmpty())
        {
            isValid = false;
            title = "Category not found";
            message = "The download category could not be found. Please try again.\nIf you are still having problems, please contact support.";
        }

        return new ValidateUrlDetailsViewModel
        {
            IsValid = isValid,
            Title = title,
            Message = message,
        };
    }

    public async Task<bool> ValidateDownloadFileAsync(DownloadFileViewModel viewModel)
    {
        // Check url validation
        if (viewModel.Url.IsStringNullOrEmpty() || !viewModel.Url.CheckUrlValidation())
        {
            await DialogBoxManager.ShowDangerDialogAsync("Url", "Please provide a valid URL to continue.", DialogButtons.Ok);
            return false;
        }

        // Get category from database
        var category = _categoryService
            .Categories
            .FirstOrDefault(c => c.Id == viewModel.CategoryId);

        // Check category
        if (category == null)
        {
            await DialogBoxManager.ShowDangerDialogAsync("Category",
                "The specified file category could not be located. Please verify the file URL and try again. If the issue persists, please contact our support team for further assistance.",
                DialogButtons.Ok);

            return false;
        }

        // Check category save directory
        if (category.CategorySaveDirectory == null)
        {
            await DialogBoxManager.ShowDangerDialogAsync("Save location",
                $"The save location for '{category.Title}' category could not be found. Please verify your settings and try again.",
                DialogButtons.Ok);

            return false;
        }

        // Check file name
        if (viewModel.FileName.IsStringNullOrEmpty())
        {
            await DialogBoxManager.ShowDangerDialogAsync("File name",
                "Please provide a name for the file, ensuring it includes the appropriate extension.",
                DialogButtons.Ok);

            return false;
        }

        // Check file extension
        if (!viewModel.FileName.HasFileExtension())
        {
            await DialogBoxManager.ShowDangerDialogAsync("File name",
                "File type is null or invalid. Please choose correct file type like .exe or .zip.",
                DialogButtons.Ok);

            return false;
        }

        // Check Url point to a file and file size is greater than 0
        if (viewModel.Size is not (null or <= 0))
            return true;

        // Check is file size unknown
        if (viewModel.IsSizeUnknown)
            return true;

        await DialogBoxManager.ShowDangerDialogAsync("No file detected",
            "It seems the URL does not point to a file. Make sure the URL points to a file and try again.",
            DialogButtons.Ok);

        return false;
    }

    public async Task<DuplicateDownloadLinkAction> GetUserDuplicateActionAsync(string url, string fileName,
        string saveLocation)
    {
        var duplicateAction = _settingsService.Settings.DuplicateDownloadLinkAction;
        if (duplicateAction != DuplicateDownloadLinkAction.LetUserChoose)
            throw new InvalidOperationException("Only works when settings related to managing duplicate links have been delegated to the user.");

        var ownerWindow = App.Desktop?.Windows.FirstOrDefault(w => w.IsFocused) ?? App.Desktop?.MainWindow;
        if (ownerWindow == null)
            throw new InvalidOperationException("Owner window not found.");

        var serviceProvider = Application.Current?.GetServiceProvider();
        var appService = serviceProvider?.GetService<IAppService>();
        if (appService == null)
            throw new InvalidOperationException("Can't find app service.");

        var viewModel = new DuplicateDownloadLinkWindowViewModel(appService, url, saveLocation, fileName);
        var window = new DuplicateDownloadLinkWindow { DataContext = viewModel };

        var action = await window.ShowDialog<DuplicateDownloadLinkAction?>(ownerWindow);
        if (action == null)
            throw new InvalidOperationException("Duplicate download link action is null.");

        return action.Value;
    }

    public string GetNewFileName(string url, string fileName, string saveLocation)
    {
        var filePath = Path.Combine(saveLocation, fileName);
        var downloadFiles = DownloadFiles
            .Where(df => df.Url?.Equals(url) == true)
            .ToList();

        if (downloadFiles.Count == 0)
            return fileName;

        var filePathList = downloadFiles
            .Where(df => !df.FileName.IsStringNullOrEmpty() && !df.SaveLocation.IsStringNullOrEmpty())
            .Select(df => Path.Combine(df.SaveLocation!, df.FileName!))
            .Distinct()
            .ToList();

        var isFilePathDuplicate = filePathList.Exists(fp => fp.Equals(filePath));
        var index = 2;
        var originalFileName = Path.GetFileNameWithoutExtension(filePath);
        var newFileName = Path.GetFileName(filePath);
        var fileExtension = Path.GetExtension(filePath);

        // Choose new name until it doesn't exist
        while (isFilePathDuplicate || File.Exists(filePath))
        {
            var oldFileName = Path.GetFileName(filePath);
            newFileName = $"{originalFileName}_{index}{fileExtension}";

            filePath = filePath.Replace(oldFileName, newFileName);
            index++;
            isFilePathDuplicate = filePathList.Exists(fp => fp.Equals(filePath));
        }

        return newFileName;
    }

    public string GetNewFileName(string fileName, string saveLocation)
    {
        var downloadFiles = DownloadFiles
            .Where(df => !df.FileName.IsStringNullOrEmpty()
                         && !df.SaveLocation.IsStringNullOrEmpty()
                         && df.FileName!.Equals(fileName)
                         && df.SaveLocation!.Equals(saveLocation))
            .ToList();

        if (downloadFiles.Count == 0 && !File.Exists(Path.Combine(saveLocation, fileName)))
            return fileName;

        var fileNames = downloadFiles.ConvertAll(df => df.FileName);
        var index = 2;
        var originalFileName = Path.GetFileNameWithoutExtension(fileName);
        var newFileName = Path.GetFileName(fileName);
        var fileExtension = Path.GetExtension(fileName);

        // Choose new name until it does not exist
        while (fileNames.Contains(newFileName) || File.Exists(Path.Combine(saveLocation, newFileName)))
        {
            newFileName = $"{originalFileName}_{index}{fileExtension}";
            index++;
        }

        return newFileName;
    }

    public void ShowOrFocusDownloadWindow(DownloadFileViewModel? viewModel)
    {
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == viewModel?.Id);
        if (downloadFile == null)
            return;

        var downloadFileTask = _downloadFileTasks.Find(task => task.Key == downloadFile.Id);
        if (downloadFileTask == null)
            return;

        Dispatcher.UIThread.Post(() => downloadFileTask.ShowOrFocusWindow());
    }

    #region Helpers

    private async void DownloadFileOnDownloadFinished(object? sender, DownloadFileEventArgs e)
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

            // Hide download window
            var downloadFileTask = _downloadFileTasks.Find(task => task.Key == e.Id);
            Dispatcher.UIThread.Post(() => downloadFileTask?.DownloadWindow?.Hide());

            if (e.Error != null)
                throw e.Error;
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowDangerDialogAsync("Error downloading file",
                $"An error occurred while downloading the file.\nError message: {ex.Message}",
                DialogButtons.Ok);

            Log.Error(ex, "An error occurred while downloading the file. Error message: {ErrorMessage}", ex.Message);
        }
    }

    private async void DownloadFileOnDownloadStopped(object? sender, DownloadFileEventArgs e)
    {
        try
        {
            var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == e.Id);
            if (downloadFile == null)
                return;

            // Remove DownloadStopped event from download file
            downloadFile.DownloadStopped -= DownloadFileOnDownloadStopped;

            // Get download file task
            var downloadFileTask = _downloadFileTasks.Find(task => task.Key == downloadFile.Id);
            if (downloadFileTask == null)
                return;

            // Hide download window
            Dispatcher.UIThread.Post(() => downloadFileTask.DownloadWindow?.Hide());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while stopping the download. Error message: {ErrorMessage}", ex.Message);

            await DialogBoxManager.ShowDangerDialogAsync("Error stopping download",
                $"An error occurred while stopping the download.\nError message: {ex.Message}",
                DialogButtons.Ok);
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
            await Dispatcher.UIThread.InvokeAsync(StopOperationsAsync);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while stopping operations. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
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

            // Remove DownloadFinished event from download file
            downloadFile.DownloadFinished -= DownloadFileOnDownloadFinished;
            // Remove DownloadStopped event from download file
            downloadFile.DownloadStopped -= DownloadFileOnDownloadStopped;

            downloadFile.TimeLeft = null;
            downloadFile.TransferRate = null;
            downloadFile.TempDownloadQueueId = downloadFile.DownloadQueueId;

            if (downloadFile.IsCompleted)
            {
                downloadFile.DownloadPackage = null;
                downloadFile.DownloadQueueId = null;
                downloadFile.DownloadQueueName = null;
                downloadFile.DownloadQueuePriority = null;

                // Play download completed sound
                if (_settingsService.Settings.UseDownloadCompleteSound)
                    _ = AudioManager.PlayAsync(AppNotificationType.DownloadCompleted);

                // Show complete download dialog when user want's this
                if (_settingsService.Settings.ShowCompleteDownloadDialog)
                {
                    var serviceProvider = Application.Current?.GetServiceProvider();
                    var appService = serviceProvider?.GetService<IAppService>();
                    if (appService == null)
                        throw new InvalidOperationException("Can't find app service.");

                    var viewModel = new CompleteDownloadWindowViewModel(appService, downloadFile);
                    var window = new CompleteDownloadWindow { DataContext = viewModel };
                    window.Show();
                }
            }
            else if (downloadFile.IsError)
            {
                // Play download failed sound
                if (_settingsService.Settings.UseDownloadFailedSound)
                    _ = AudioManager.PlayAsync(AppNotificationType.DownloadFailed);
            }
            else if (downloadFile.IsStopped)
            {
                // Play download stopped sound
                if (_settingsService.Settings.UseDownloadStoppedSound && downloadFile.PlayStopSound)
                    _ = AudioManager.PlayAsync(AppNotificationType.DownloadStopped);

                downloadFile.PlayStopSound = true;
            }

            List<DownloadFinishedTaskValue?> taskValues = [];
            if (DownloadFinishedAsyncTasks.TryGetValue(downloadFile.Id, out var asyncTasks))
            {
                foreach (var task in asyncTasks)
                {
                    var value = await task(downloadFile);
                    taskValues.Add(value);
                }
            }

            DownloadFinishedAsyncTasks.Remove(downloadFile.Id);

            if (DownloadFinishedSyncTasks.TryGetValue(downloadFile.Id, out var syncTasks))
                taskValues.AddRange(syncTasks.Select(task => task(downloadFile)));

            DownloadFinishedSyncTasks.Remove(downloadFile.Id);

            // Set temp download queue id to null
            downloadFile.TempDownloadQueueId = null;

            var exception = taskValues.Find(v => v?.Exception != null)?.Exception;
            if (exception != null)
            {
                await DialogBoxManager.ShowDangerDialogAsync("Error downloading file",
                    $"An error occurred while downloading the file.\nError message: {exception.Message}",
                    DialogButtons.Ok);

                Log.Error(exception, "An error occurred while downloading the file. Error message: {ErrorMessage}", exception.Message);
            }

            var allTasksAreNull = taskValues.TrueForAll(v => v == null);
            var updateNeededTaskIsExist = taskValues.Exists(v => v?.UpdateDownloadFile == true);
            if (allTasksAreNull || updateNeededTaskIsExist)
                await UpdateDownloadFileAsync(downloadFile);

            // Find task from the list
            var downloadFileTask = _downloadFileTasks.Find(task => task.Key == downloadFile.Id);
            if (downloadFileTask != null)
            {
                // Close download window
                downloadFileTask.DownloadWindow?.Close();

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

            _stopOperations.Remove(id);
        }

        _stopOperationIsRunning = false;
    }

    private async Task EnsureDownloadFileStoppedAsync(int downloadFileId)
    {
        // Find tasks from the list
        DownloadFileTaskViewModel? downloadFileTask;

        // Check if the stop operation is finished
        while (true)
        {
            downloadFileTask = _downloadFileTasks.Find(task => task.Key == downloadFileId);
            if (downloadFileTask?.StopOperationFinished == true)
                break;

            await Task.Delay(100);
        }

        // Remove the task
        _downloadFileTasks.Remove(downloadFileTask);
    }

    private async Task DeleteDuplicateDownloadFilesAsync(string url)
    {
        var downloadFiles = DownloadFiles
            .Where(df => df.Url?.Equals(url) == true)
            .ToList();

        await DeleteDownloadFilesAsync(downloadFiles, alsoDeleteFile: true);
    }

    private async Task<DownloadFileViewModel> ShowCompleteDownloadDialogOrResumeAsync(string url)
    {
        var downloadFile = DownloadFiles.LastOrDefault(df => df.Url?.Equals(url) == true);
        if (downloadFile == null)
            throw new InvalidOperationException("No duplicate download files found.");

        var ownerWindow = App.Desktop?.Windows.FirstOrDefault(w => w.IsFocused) ?? App.Desktop?.MainWindow;
        if (ownerWindow == null)
            throw new InvalidOperationException("Owner window not found.");

        var serviceProvider = Application.Current?.GetServiceProvider();
        var appService = serviceProvider?.GetService<IAppService>();
        if (appService == null)
            throw new InvalidOperationException("Can't find app service.");

        if (downloadFile.IsCompleted)
        {
            var viewModel = new CompleteDownloadWindowViewModel(appService, downloadFile);
            var window = new CompleteDownloadWindow { DataContext = viewModel };
            await window.ShowDialog(ownerWindow);
        }
        else
        {
            if (downloadFile.IsPaused)
            {
                var downloadFileTask = _downloadFileTasks.Find(task => task.Key == downloadFile.Id);
                if (downloadFileTask?.DownloadWindow == null)
                    throw new InvalidOperationException("Download window not found.");

                ResumeDownloadFile(downloadFile);

                // Show download window if it is not visible
                if (!downloadFileTask.DownloadWindow.IsVisible)
                    Dispatcher.UIThread.Post(() => downloadFileTask.DownloadWindow.Show());
            }
            else
            {
                _ = StartDownloadFileAsync(downloadFile);
            }
        }

        return downloadFile;
    }

    private long GetMaximumBytesPerSecond()
    {
        if (!_settingsService.Settings.IsSpeedLimiterEnabled)
            return 0;

        var limitSpeed = (long)(_settingsService.Settings.LimitSpeed ?? 0);
        var limitUnit = _settingsService.Settings.LimitUnit?.Equals("KB", StringComparison.OrdinalIgnoreCase) == true ? Constants.KiloByte : Constants.MegaByte;
        return limitSpeed * limitUnit;
    }

    private static async Task<bool> CheckDiskSpaceAsync(DownloadFileViewModel downloadFile)
    {
        // Make sure save location and file name are not empty
        if (downloadFile.SaveLocation.IsStringNullOrEmpty() || downloadFile.FileName.IsStringNullOrEmpty())
        {
            await DialogBoxManager.ShowDangerDialogAsync("Storage not found",
                "An error occurred while trying to determine the storage location for this file. This may be due to a system error or missing file information.",
                DialogButtons.Ok);

            return false;
        }

        // Create save directory before calculating available free space
        if (!Directory.Exists(downloadFile.SaveLocation))
            Directory.CreateDirectory(downloadFile.SaveLocation!);

        // Find file if exists and get it's size
        var filePath = Path.Combine(downloadFile.SaveLocation!, downloadFile.FileName!);
        long downloadedSize = 0;
        if (File.Exists(filePath))
            downloadedSize = new FileInfo(filePath).Length;

        // When the server does not include Content-Length in the request header, the validation of the amount of free disk space will be correct.
        if (downloadFile.IsSizeUnknown)
            return true;

        // Make sure size is greater than 0
        if (downloadFile.Size is null or <= 0)
            return false;

        // Find the drive where the file is stored and check the free space
        var driveInfo = new DriveInfo(downloadFile.SaveLocation!);
        var hasEnoughSpace = driveInfo.AvailableFreeSpace >= downloadFile.Size!.Value - downloadedSize;
        if (hasEnoughSpace)
            return true;

        // If there is no available space, notify user
        await DialogBoxManager.ShowDangerDialogAsync("Insufficient disk space",
            $"There is not enough free space available on the path '{downloadFile.SaveLocation}'.",
            DialogButtons.Ok);

        return false;
    }

    #endregion
}