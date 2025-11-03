using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using RolandK.AvaloniaExtensions.DependencyInjection;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService;

/// <summary>
/// Service for managing download files.
/// </summary>
public class DownloadFileService : PropertyChangedBase, IDownloadFileService
{
    #region Private Fields

    /// <summary>
    /// The unit of work service for accessing database operations.
    /// </summary>
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// The mapper for converting between different object types.
    /// </summary>
    private readonly IMapper _mapper;

    /// <summary>
    /// The settings service for accessing application settings.
    /// </summary>
    private readonly ISettingsService _settingsService;

    /// <summary>
    /// The category service for accessing categories.
    /// </summary>
    private readonly ICategoryService _categoryService;

    /// <summary>
    /// The semaphore slim for managing concurrent access to the completed downloads files.
    /// </summary>
    private readonly SemaphoreSlim _addCompletedBlocker;

    /// <summary>
    /// The completed download files queue.
    /// </summary>
    private readonly ConcurrentQueue<FileDownloader> _completedDownloads = [];

    /// <summary>
    /// The downloading files collection.
    /// </summary>
    private readonly ObservableCollection<FileDownloader> _downloadingFiles = [];

    /// <summary>
    /// The cancellation token source for canceling the watcher task.
    /// </summary>
    private CancellationTokenSource? _watcherCancellationSource;

    // Backing fields for properties
    private ObservableCollection<DownloadFileViewModel> _downloadFiles = [];

    #endregion

    #region Properties

    public ObservableCollection<DownloadFileViewModel> DownloadFiles
    {
        get => _downloadFiles;
        private set => SetField(ref _downloadFiles, value);
    }

    #endregion

    #region Events

    public event EventHandler? DataChanged;

    #endregion

    /// <summary>
    /// Initializes a new instance of DownloadFileService
    /// </summary>
    /// <param name="unitOfWork">The unit of work for database operations</param>
    /// <param name="mapper">The mapper for object mapping</param>
    /// <param name="settingsService">The settings service for application settings</param>
    /// <param name="categoryService">The category service for category operations</param>
    public DownloadFileService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ISettingsService settingsService,
        ICategoryService categoryService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _settingsService = settingsService;
        _categoryService = categoryService;
        _addCompletedBlocker = new SemaphoreSlim(1);

        StartWatcher();

        Log.Debug("DownloadFileService initialized with required dependencies");
    }

    public async Task LoadDownloadFilesAsync()
    {
        Log.Debug("Loading download files and updating data...");

        // Get all download files from database
        var downloadFiles = await _unitOfWork
            .DownloadFileRepository
            .GetAllAsync(includeProperties: ["Category.FileExtensions", "DownloadQueue"]);

        Log.Debug("Retrieved {DownloadFileCount} download files from database", downloadFiles.Count);

        // Convert download files to view models
        var viewModels = _mapper.Map<List<DownloadFileViewModel>>(downloadFiles);
        var primaryKeys = viewModels.ConvertAll(df => df.Id);

        // Find download files that removed
        var deletedDownloadFiles = DownloadFiles
            .Where(df => !primaryKeys.Contains(df.Id))
            .ToList();

        Log.Debug("Found {DeletedCount} download files to remove", deletedDownloadFiles.Count);

        // Remove download files from list
        foreach (var downloadFile in deletedDownloadFiles)
        {
            Log.Debug("Removing download file with ID {DownloadFileId} from local collection", downloadFile.Id);
            await DeleteDownloadFileAsync(downloadFile, alsoDeleteFile: false, reloadData: false);
        }

        // Find primary keys
        primaryKeys = DownloadFiles.Select(df => df.Id).ToList();
        // Find new download files
        var addedDownloadFiles = viewModels.Where(df => !primaryKeys.Contains(df.Id)).ToList();

        Log.Debug("Found {AddedCount} new download files to add", addedDownloadFiles.Count);

        // Add new download files
        foreach (var downloadFile in addedDownloadFiles)
        {
            Log.Debug("Adding new download file with ID {DownloadFileId} to local collection", downloadFile.Id);
            DownloadFiles.Add(downloadFile);
        }

        // Find old download files
        var previousDownloadFiles = viewModels.Except(addedDownloadFiles).ToList();

        Log.Debug("Updating {ExistingCount} existing download files", previousDownloadFiles.Count);

        // Update required data in old download files
        foreach (var downloadFile in previousDownloadFiles)
        {
            // Make sure old download file exists
            var previousDownloadFile = DownloadFiles.FirstOrDefault(df => df.Id == downloadFile.Id);
            if (previousDownloadFile == null)
                continue;

            // Update data
            Log.Debug("Updating existing download file with ID {DownloadFileId}", downloadFile.Id);

            previousDownloadFile.DownloadQueueId = downloadFile.DownloadQueueId;
            previousDownloadFile.DownloadQueueName = downloadFile.DownloadQueueName;
            previousDownloadFile.DownloadQueuePriority = downloadFile.DownloadQueuePriority;
        }

        Log.Debug("Updating the type of the download files...");

        // Set file type for each download file
        foreach (var downloadFile in DownloadFiles)
            downloadFile.FileType = GetFileType(downloadFile);

        // Notify download files changed
        OnPropertyChanged(nameof(DownloadFiles));
        // Raise changed event
        DataChanged?.Invoke(this, EventArgs.Empty);
        // Log information
        Log.Information("Download files loaded successfully. Total files: {TotalCount}", DownloadFiles.Count);
    }

    public async Task<DownloadFileViewModel?> AddDownloadFileAsync(DownloadFileViewModel viewModel, DownloadFileOptions? options = null)
    {
        Log.Information("Trying to add new download file with URL '{URL}'", viewModel.Url);

        // Validate download file
        var isValid = await ValidateDownloadFileAsync(viewModel);
        if (!isValid)
        {
            Log.Information("The validation of the download file failed. CDM can't process this download file.");
            return null;
        }

        // Find category
        var category = _categoryService.Categories.FirstOrDefault(c => c.Id == viewModel.CategoryId);

        // Get save location from view model.
        // If view model save location is empty and categories are disabled, get save location from settings.
        // Otherwise, get save location from category.
        var saveLocation = viewModel.SaveLocation.IsStringNullOrEmpty()
            ? _settingsService.Settings.DisableCategories ? _settingsService.Settings.GlobalSaveLocation! : category!.CategorySaveDirectory!.SaveDirectory
            : viewModel.SaveLocation!;

        Log.Debug("Save location for the download file is '{SaveLocation}'.", saveLocation);

        // Create an instance of DownloadFile
        var downloadFile = new DownloadFile
        {
            Url = viewModel.Url!,
            FileName = viewModel.FileName!,
            DownloadQueueId = viewModel.DownloadQueueId,
            Size = viewModel.Size!.Value,
            IsSizeUnknown = viewModel.IsSizeUnknown,
            Description = viewModel.Description ?? options?.Description,
            Status = viewModel.Status ?? DownloadFileStatus.None,
            LastTryDate = null,
            DateAdded = DateTime.Now,
            DownloadQueuePriority = viewModel.DownloadQueuePriority,
            CategoryId = category!.Id,
            SaveLocation = saveLocation,
            DownloadProgress = viewModel.DownloadProgress is > 0 ? viewModel.DownloadProgress.Value : 0,
            DownloadPackage = viewModel.DownloadPackage,
            Referer = viewModel.Referer ?? options?.Referer,
            PageAddress = viewModel.PageAddress ?? options?.PageAddress,
            Username = viewModel.Username ?? options?.Username,
            Password = viewModel.Password ?? options?.Password
        };

        // Handle duplicate download links
        DownloadFileViewModel? result = null;
        // Check is url duplicate another time
        var isUrlDuplicate = CheckIsUrlDuplicate(downloadFile.Url);
        // Check if there is an existing download file with the same URL and handle it if its exists
        if (viewModel.IsUrlDuplicate || isUrlDuplicate)
        {
            try
            {
                if (isUrlDuplicate)
                {
                    Log.Debug("URL is duplicate. Handling duplicate URL...");
                    result = await HandleDuplicateUrlAsync(downloadFile);
                }
            }
            catch (Exception ex)
            {
                // If an error occurs while trying to handle duplicate URL, log it and return null
                Log.Error(ex, "An error occurred while trying to handle duplicate URL. Error message: {ErrorMessage}", ex.Message);
                return null;
            }
        }

        // If duplicate download action sets to ShowCompleteDialogOrResume, we have to pass the existing download file
        if (result != null)
        {
            Log.Information("Duplicate download action is set to {Action}. Returning existing download file...",
                nameof(DuplicateDownloadLinkAction.ShowCompleteDialogOrResume));

            return result;
        }

        // Check if the file name is duplicate and handle it if its exists
        var isFileNameDuplicate = CheckIsFileNameDuplicate(downloadFile.FileName, downloadFile.SaveLocation);
        // Make sure each file has a unique name
        if (viewModel.IsFileNameDuplicate || isFileNameDuplicate)
        {
            if (isFileNameDuplicate)
            {
                Log.Debug("File name is duplicate. Handling duplicate file name...");
                HandleDuplicateFileName(downloadFile);
            }
        }

        // Add new download file and save it
        await _unitOfWork.DownloadFileRepository.AddAsync(downloadFile);
        await _unitOfWork.SaveAsync();

        Log.Information("New download file added. Download file ID: {DownloadFileId}", downloadFile.Id);

        // Reload data
        await LoadDownloadFilesAsync();

        // Find download file in data
        result = DownloadFiles.FirstOrDefault(df => df.Id == downloadFile.Id);
        // Start download when necessary
        if (options?.StartDownloading == true)
        {
            Log.Information("Starting download file...");
            _ = StartDownloadFileAsync(result);
        }

        // Return new download file
        return result;
    }

    public async Task<DownloadFileViewModel?> AddDownloadFileAsync(string? url, DownloadFileOptions? options = null)
    {
        if (url.IsStringNullOrEmpty() || !url.CheckUrlValidation())
        {
            Log.Warning("Attempted to add download file with invalid URL: {URL}", url);
            return null;
        }

        Log.Debug("Creating download file from URL: {URL}", url);
        var downloadFile = await GetDownloadFileFromUrlAsync(url, options);
        return await AddDownloadFileAsync(downloadFile, options);
    }

    public async Task UpdateDownloadFileAsync(DownloadFileViewModel viewModel)
    {
        var downloadFileViewModel = DownloadFiles.FirstOrDefault(df => df.Id == viewModel.Id);
        if (downloadFileViewModel == null)
        {
            Log.Warning("Download file with ID {DownloadFileId} not found for update", viewModel.Id);
            return;
        }

        Log.Debug("Updating download file with ID: {DownloadFileId}", viewModel.Id);

        var downloadFile = _mapper.Map<DownloadFile>(downloadFileViewModel);
        await _unitOfWork.DownloadFileRepository.UpdateAsync(downloadFile);
        await _unitOfWork.SaveAsync();

        Log.Debug("Download file with ID {DownloadFileId} updated in database", viewModel.Id);
        await LoadDownloadFilesAsync();
    }

    public async Task UpdateDownloadFilesAsync(List<DownloadFileViewModel> viewModels)
    {
        if (viewModels.Count == 0)
        {
            Log.Debug("No download files provided for update");
            return;
        }

        Log.Debug("Updating {Count} download files in batch", viewModels.Count);

        var downloadFileViewModels = viewModels
            .Select(vm => DownloadFiles.FirstOrDefault(df => df.Id == vm.Id))
            .Where(df => df != null)
            .ToList();

        if (downloadFileViewModels.Count == 0)
        {
            Log.Warning("No matching download files found for update");
            return;
        }

        var downloadFiles = _mapper.Map<List<DownloadFile>>(downloadFileViewModels);
        await _unitOfWork.DownloadFileRepository.UpdateAllAsync(downloadFiles);
        await _unitOfWork.SaveAsync();

        Log.Debug("Successfully updated {Count} download files in database", downloadFiles.Count);
        await LoadDownloadFilesAsync();
    }

    public async Task DeleteDownloadFileAsync(DownloadFileViewModel? viewModel, bool alsoDeleteFile, bool reloadData = true)
    {
        // Find download file
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == viewModel?.Id);
        if (downloadFile == null)
        {
            Log.Warning("Download file with ID {DownloadFileId} not found for deletion", viewModel?.Id);
            return;
        }

        Log.Information("Deleting download file with ID: {DownloadFileId}, File: {FileName}", downloadFile.Id, downloadFile.FileName);

        // Find original download file in database
        var downloadFileInDb = await _unitOfWork
            .DownloadFileRepository
            .GetAsync(where: df => df.Id == downloadFile.Id);

        // Stop download file if downloading or paused
        if (downloadFile.IsDownloading || downloadFile.IsPaused)
        {
            Log.Debug("Stopping download file before deletion");
            await StopDownloadFileAsync(downloadFile, ensureStopped: true);
        }

        // Remove temp files of download file
        await RemoveTempFilesAsync(downloadFile);
        // If the download file does not exist in the database but does exist in the application,
        // only the file in the application needs to be deleted and there is no need to delete the file in the database as well
        var shouldReturn = false;
        if (downloadFileInDb == null)
        {
            Log.Debug("Download file exists only in local collection, removing from local collection");
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
                {
                    Log.Debug("Deleting physical file: {FilePath}", filePath);
                    await Rubbish.MoveAsync(filePath);
                }
            }
        }

        // Check if the download file needs to be deleted from the database
        if (shouldReturn)
            return;

        // Remove download file from database
        await _unitOfWork.DownloadFileRepository.DeleteAsync(downloadFileInDb);
        await _unitOfWork.SaveAsync();

        Log.Debug("Download file with ID {DownloadFileId} deleted from database", downloadFile.Id);

        // Update downloads list when necessary
        if (reloadData)
        {
            Log.Debug("Reloading download files after deletion");
            await LoadDownloadFilesAsync();
        }

        Log.Information("Download file with ID {DownloadFileId} deleted successfully", downloadFile.Id);
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
                {
                    Log.Debug("Download file is paused, resuming it");
                    ResumeDownloadFile(downloadFile);
                }
                else
                {
                    Log.Debug("Download file is not in a valid state for starting. State: {State}", downloadFile?.Status);
                }

                return;
            }

            Log.Information("Starting download file with ID: {DownloadFileId}", downloadFile.Id);

            // Check disk size before starting download
            var hasEnoughSpace = await CheckDiskSpaceAsync(downloadFile);
            if (!hasEnoughSpace)
            {
                Log.Warning("Insufficient disk space for download file with ID: {DownloadFileId}", downloadFile.Id);
                return;
            }

            // Create an instance of the FileDownloader class for managing download file
            var fileDownloader = GetOrCreateFileDownloader(downloadFile)!;
            // Create a window for showing the download progress
            fileDownloader.CreateDownloadWindow(showWindow);

            // Subscribe to the events
            downloadFile.DownloadFinished += DownloadFileOnDownloadFinished;
            downloadFile.DownloadStopped += DownloadFileOnDownloadStopped;
            // Start download
            await fileDownloader.StartDownloadFileAsync();

            Log.Debug("Download started successfully for file with ID: {DownloadFileId}", downloadFile.Id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while downloading a file. Error message: {ErrorMessage}", ex.Message);
        }
    }

    public async Task StopDownloadFileAsync(DownloadFileViewModel? viewModel, bool ensureStopped = false, bool playSound = true)
    {
        // Find download file
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == viewModel?.Id);
        // Check download file state
        if (downloadFile == null || downloadFile.IsStopped || downloadFile.IsStopping)
        {
            Log.Debug("Download file is not in a valid state for stopping. State: {State}", downloadFile?.Status);
            return;
        }

        Log.Information("Stopping download file with ID: {DownloadFileId}", downloadFile.Id);

        // Reset the completely state of the download file
        downloadFile.IsCompletelyStopped = false;
        // Find file downloader for the current download file
        var fileDownloader = GetOrCreateFileDownloader(downloadFile, canCreate: false);
        if (fileDownloader == null)
        {
            Log.Warning("File downloader not found for download file with ID: {DownloadFileId}", downloadFile.Id);
            return;
        }

        // Change the play sound flag
        downloadFile.PlayStopSound = playSound;
        // Stop download file
        await fileDownloader.StopDownloadFileAsync();

        Log.Debug("Stop command sent for download file with ID: {DownloadFileId}", downloadFile.Id);

        // Wait for the download to stop
        if (ensureStopped)
        {
            Log.Debug("Waiting for download file to completely stop");
            await EnsureDownloadFileStoppedAsync(downloadFile.Id);
        }
    }

    public void ResumeDownloadFile(DownloadFileViewModel? viewModel)
    {
        // Find download file
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == viewModel?.Id);
        // Check download file state
        if (downloadFile is not { IsPaused: true })
        {
            Log.Debug("Download file is not paused or not found. State: {State}", downloadFile?.Status);
            return;
        }

        Log.Information("Resuming download file with ID: {DownloadFileId}", downloadFile.Id);

        // Find file downloader for the current download file
        var fileDownloader = GetOrCreateFileDownloader(downloadFile, canCreate: false);
        if (fileDownloader == null)
        {
            Log.Warning("File downloader not found for download file with ID: {DownloadFileId}", downloadFile.Id);
            return;
        }

        // Resume download file
        fileDownloader.ResumeDownloadFile();
        // Show or focus download window
        ShowOrFocusDownloadWindow(downloadFile);

        Log.Debug("Download file with ID {DownloadFileId} resumed successfully", downloadFile.Id);
    }

    public void PauseDownloadFile(DownloadFileViewModel? viewModel)
    {
        // Find download file
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == viewModel?.Id);
        // Check download file state
        if (downloadFile == null || downloadFile.IsPaused)
        {
            Log.Debug("Download file is already paused or not found. State: {State}", downloadFile?.Status);
            return;
        }

        Log.Information("Pausing download file with ID: {DownloadFileId}", downloadFile.Id);

        // Find file downloader for the current download file
        var fileDownloader = GetOrCreateFileDownloader(downloadFile, canCreate: false);
        // Pause download file
        fileDownloader?.PauseDownloadFile();

        Log.Debug("Download file with ID {DownloadFileId} paused successfully", downloadFile.Id);
    }

    public void LimitDownloadFileSpeed(DownloadFileViewModel? viewModel, long speed)
    {
        // Find download file
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == viewModel?.Id);
        if (downloadFile == null)
        {
            Log.Warning("Download file with ID {DownloadFileId} not found for speed limiting", viewModel?.Id);
            return;
        }

        Log.Debug("Limiting download speed for file with ID {DownloadFileId} to {Speed} bytes/s", downloadFile.Id, speed);

        // Find file downloader for the current download file
        var fileDownloader = GetOrCreateFileDownloader(downloadFile, canCreate: false);
        if (fileDownloader == null)
        {
            Log.Warning("File downloader not found for download file with ID: {DownloadFileId}", downloadFile.Id);
            return;
        }

        // Change the maximum bytes per second speed
        fileDownloader.DownloadConfiguration.MaximumBytesPerSecond = speed;

        Log.Debug("Download speed limited successfully for file with ID {DownloadFileId}", downloadFile.Id);
    }

    public async Task RedownloadDownloadFileAsync(DownloadFileViewModel? viewModel, bool showWindow = true)
    {
        // Try to find the download file
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == viewModel?.Id);
        if (downloadFile == null)
        {
            Log.Warning("Download file with ID {DownloadFileId} not found for re-download", viewModel?.Id);
            return;
        }

        Log.Information("Re-downloading file with ID: {DownloadFileId}", downloadFile.Id);

        // Reset the properties of the download file
        downloadFile.Reset();
        // Remove temp files of download file
        await RemoveTempFilesAsync(downloadFile);
        // Remove download file from storage as well
        if (!downloadFile.SaveLocation.IsStringNullOrEmpty() && !downloadFile.FileName.IsStringNullOrEmpty())
        {
            var filePath = Path.Combine(downloadFile.SaveLocation!, downloadFile.FileName!);
            if (File.Exists(filePath))
            {
                Log.Debug("Removing existing file for re-download: {FilePath}", filePath);
                await Rubbish.MoveAsync(filePath);
            }
        }

        // Update download file with the new data
        await UpdateDownloadFileAsync(downloadFile);
        // Start downloading the file
        _ = StartDownloadFileAsync(downloadFile, showWindow);

        Log.Information("Re-download initiated for file with ID: {DownloadFileId}", downloadFile.Id);
    }

    public string GetDownloadSpeed()
    {
        var downloadSpeed = DownloadFiles.Where(df => df.IsDownloading).Sum(df => df.TransferRate ?? 0);
        var speedString = downloadSpeed.ToFileSize();
        Log.Debug("Current total download speed: {DownloadSpeed}", speedString);
        return speedString;
    }

    public async Task<DownloadFileViewModel> GetDownloadFileFromUrlAsync(string? url, DownloadFileOptions? options = null, CancellationToken cancellationToken = default)
    {
        var downloadFile = new DownloadFileViewModel
        {
            // Change the URL to correct format
            Url = url?.Replace("\\", "/").Trim(),
            Referer = options?.Referer,
            PageAddress = options?.PageAddress,
            Description = options?.Description,
            Username = options?.Username,
            Password = options?.Password
        };

        Log.Debug("Trying to get download file details from URL: {Url}", downloadFile.Url);

        // Check if the URL is valid
        if (!downloadFile.Url.CheckUrlValidation())
        {
            Log.Debug("It seems that the URL is not valid. URL: {Url}", downloadFile.Url);
            return downloadFile;
        }

        // Create an instance of the DownloadRequest class.
        var request = new DownloadRequest(downloadFile.Url!);
        // Fetch response headers
        await request.FetchResponseHeadersAsync(cancellationToken);

        // Update download file url if it's different
        var requestUrl = request.Url?.AbsoluteUri.Trim() ?? string.Empty;
        if (!downloadFile.Url!.Equals(requestUrl) && !requestUrl.IsStringNullOrEmpty())
        {
            downloadFile.Url = requestUrl;
            Log.Debug("Download file URL updated to '{FinalUrl}'. Original URL was '{OriginalUrl}'.", downloadFile.Url, url);
        }

        // Check if response headers fetched successfully
        if (request.ResponseHeaders.Count == 0)
        {
            Log.Debug("Failed to fetch response headers for URL: {Url}", downloadFile.Url);
            return downloadFile;
        }

        var isFile = true;
        string? contentDisposition;

        // Check if the Content-Type indicates a file
        if (request.ResponseHeaders.TryGetValue("Content-Type", out var contentType)
            && !contentType.Contains("application/")
            && !contentType.Contains("image/")
            && !contentType.Contains("video/")
            && !contentType.Contains("audio/")
            && !contentType.Contains("text/"))
        {
            // Check Content-Disposition header
            isFile = request.ResponseHeaders.TryGetValue("Content-Disposition", out contentDisposition)
                     && contentDisposition.Contains("attachment", StringComparison.OrdinalIgnoreCase);
        }

        // Make sure the url point to a file
        if (!isFile)
        {
            Log.Debug("URL does not point to a file. URL: {Url}", downloadFile.Url);
            return downloadFile;
        }

        string? fileName = null;
        // Get file name from header if possible
        if (request.ResponseHeaders.TryGetValue("Content-Disposition", out contentDisposition))
            fileName = contentDisposition.GetFileNameFromContentDisposition() ?? string.Empty;

        // Get file name from x-suggested-filename header if possible
        if (fileName.IsStringNullOrEmpty() && request.ResponseHeaders.TryGetValue("x-suggested-filename", out var suggestedFileName))
            fileName = suggestedFileName;

        // Fallback to using the URL to guess the file name if Content-Disposition is not present
        if (fileName.IsStringNullOrEmpty())
            fileName = downloadFile.Url.GetFileName();

        // Set file name
        downloadFile.FileName = fileName?.Trim() ?? string.Empty;
        Log.Debug("The download file name is '{FileName}'.", downloadFile.FileName);

        // Check if the URL has Content-Length header
        downloadFile.IsSizeUnknown = !request.ResponseHeaders.TryGetValue("Content-Length", out var contentLength);
        Log.Debug("The download file size is {FileSize}.", downloadFile.IsSizeUnknown ? "Unknown" : "Known");

        // Set file size
        downloadFile.Size = downloadFile.IsSizeUnknown ? 0 : long.TryParse(contentLength, out var size) ? size : 0;
        Log.Debug("The size of the download file is {FileSize} byte(s).", downloadFile.Size);

        // find category item by file extension
        var extension = Path.GetExtension(downloadFile.FileName);
        Log.Debug("The file extension is '{Extension}'.", extension);

        // Get all custom categories
        var customCategories = _categoryService
            .Categories
            .Where(c => !c.IsDefault)
            .ToList();

        // Find file extension by extension
        CategoryFileExtensionViewModel? fileExtension;
        var customCategory = customCategories
            .Find(c => c.FileExtensions.Any(fe => fe.Extension.Equals(extension, StringComparison.CurrentCultureIgnoreCase)));

        // Check if the custom category exists
        // Custom categories have higher priority than default categories.
        if (customCategory != null)
        {
            Log.Debug("Custom category found for file extension '{Extension}'.", extension);

            fileExtension = customCategory
                .FileExtensions
                .FirstOrDefault(fe => fe.Extension.Equals(extension, StringComparison.CurrentCultureIgnoreCase));
        }
        // Otherwise, there is no custom category, so we need to find the default category.
        else
        {
            Log.Debug("Using default category for file extension '{Extension}'...", extension);

            fileExtension = _categoryService
                .Categories
                .SelectMany(c => c.FileExtensions)
                .FirstOrDefault(fe => fe.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase));
        }

        CategoryViewModel? category = null;
        // Find category by domain
        var siteDomain = downloadFile.Url.GetDomainFromUrl();
        if (!siteDomain.IsStringNullOrEmpty())
        {
            Log.Debug("Checking if there is a category for the domain '{SiteDomain}'...", siteDomain);

            category = _categoryService
                .Categories
                .FirstOrDefault(c => c.AutoAddedLinksFromSitesList.Contains(siteDomain!));
        }

        // Find category by file extension
        if (category == null)
        {
            Log.Debug("Category is null. Finding category...");

            // Find category by category file extension or choose general category
            if (fileExtension != null)
            {
                category = customCategory ?? fileExtension.Category;
            }
            else
            {
                category = _categoryService
                    .Categories
                    .FirstOrDefault(c => c.Title.Equals(Constants.GeneralCategoryTitle, StringComparison.OrdinalIgnoreCase));
            }
        }

        // Set category id
        downloadFile.CategoryId = category?.Id;
        Log.Debug("The category id is '{CategoryId}'.", downloadFile.CategoryId);

        // Find a download file with the same url
        downloadFile.IsUrlDuplicate = CheckIsUrlDuplicate(downloadFile.Url);
        Log.Debug("The download file url is {DuplicateState}.", downloadFile.IsUrlDuplicate ? "duplicate" : "not duplicate");

        // Find a download file with the same name
        downloadFile.IsFileNameDuplicate = CheckIsFileNameDuplicate(downloadFile.FileName, category?.CategorySaveDirectory?.SaveDirectory);
        Log.Debug("The download file name is {DuplicateState}.", downloadFile.IsFileNameDuplicate ? "duplicate" : "not duplicate");

        // Return the download file
        Log.Debug("Successfully created download file from URL: {Url}", url);
        return downloadFile;
    }

    public async Task<bool> ValidateDownloadFileAsync(DownloadFileViewModel downloadFile, bool showMessage = true)
    {
        Log.Debug("Validating download file...");

        // Check url validation
        if (downloadFile.Url.IsStringNullOrEmpty() || !downloadFile.Url.CheckUrlValidation())
        {
            Log.Debug("The URL of the download file is invalid. CDM can't process this download file.");

            if (showMessage)
                await DialogBoxManager.ShowDangerDialogAsync("Url", "Please provide a valid URL to continue.", DialogButtons.Ok);

            return false;
        }

        // Check if the categories already disabled or not
        if (_settingsService.Settings.DisableCategories && _settingsService.Settings.GlobalSaveLocation.IsStringNullOrEmpty())
        {
            Log.Debug("The categories are disabled and the global save location is not set. The global save location is required to download files.");

            if (showMessage)
            {
                await DialogBoxManager.ShowDangerDialogAsync("Save location",
                    "The global save location is not set. Please set it in the settings.",
                    DialogButtons.Ok);
            }

            return false;
        }

        // Get category from database
        var category = _categoryService.Categories.FirstOrDefault(c => c.Id == downloadFile.CategoryId);
        // Check category
        if (category == null)
        {
            Log.Debug("Category is null. The specified file category could not be located.");

            if (showMessage)
            {
                await DialogBoxManager.ShowDangerDialogAsync("Category",
                    "The specified file category could not be located. Please verify the file URL and try again. If the issue persists, please contact our support team for further assistance.",
                    DialogButtons.Ok);
            }

            return false;
        }

        // Check category save directory
        if (category.CategorySaveDirectory == null || category.CategorySaveDirectory.SaveDirectory.IsStringNullOrEmpty())
        {
            Log.Debug("The save location for '{CategoryTitle}' category could not be found.", category.Title);

            if (showMessage)
            {
                await DialogBoxManager.ShowDangerDialogAsync("Save location",
                    $"The save location for '{category.Title}' category could not be found. Please verify your settings and try again.",
                    DialogButtons.Ok);
            }

            return false;
        }

        // Check file name
        if (downloadFile.FileName.IsStringNullOrEmpty())
        {
            Log.Debug("The file name is null or empty. CDM can't process this download file.");

            if (showMessage)
            {
                await DialogBoxManager.ShowDangerDialogAsync("File name",
                    "Please provide a name for the file, ensuring it includes the appropriate extension.",
                    DialogButtons.Ok);
            }

            return false;
        }

        // Check file extension
        if (!downloadFile.FileName.HasFileExtension())
        {
            Log.Debug("The file name does not have a valid file extension.");

            if (showMessage)
            {
                await DialogBoxManager.ShowDangerDialogAsync("File name",
                    "File type is null or invalid. Please choose correct file type like .exe or .zip.",
                    DialogButtons.Ok);
            }

            return false;
        }

        // Check Url point to a file and file size is greater than 0 or is unknown
        if (downloadFile.Size is not (null or <= 0) || downloadFile.IsSizeUnknown)
        {
            Log.Information("It's look like the download file is valid and can be added to database.");
            return true;
        }

        Log.Debug("The size of the download file is not provided or invalid.");

        if (showMessage)
        {
            await DialogBoxManager.ShowDangerDialogAsync("No file detected",
                "It seems the URL does not point to a file. Make sure the URL points to a file and try again.",
                DialogButtons.Ok);
        }

        return false;
    }

    public async Task<DuplicateDownloadLinkAction> GetUserDuplicateActionAsync(string url, string fileName, string saveLocation)
    {
        // Get the duplicate download link action from the settings
        var duplicateAction = _settingsService.Settings.DuplicateDownloadLinkAction;
        // If the duplicate download link action is not set to LetUserChoose, throw an exception
        if (duplicateAction != DuplicateDownloadLinkAction.LetUserChoose)
            throw new InvalidOperationException("Only works when settings related to managing duplicate links have been delegated to the user.");

        Log.Debug("Showing duplicate download link dialog for URL: {Url}", url);

        // Get the owner window
        var ownerWindow = App.Desktop?.Windows.FirstOrDefault(w => w.IsFocused) ?? App.Desktop?.MainWindow;
        // If the owner window is not found, throw an exception
        if (ownerWindow == null)
            throw new InvalidOperationException("Owner window not found.");

        // Get the service provider
        var serviceProvider = Application.Current?.GetServiceProvider();
        // Get the app service
        var appService = serviceProvider?.GetService<IAppService>();
        // If the app service is not found, throw an exception
        if (appService == null)
            throw new InvalidOperationException("Can't find app service.");

        // Create a new DuplicateDownloadLinkWindowViewModel
        var viewModel = new DuplicateDownloadLinkWindowViewModel(appService, url, saveLocation, fileName);
        // Create a new DuplicateDownloadLinkWindow
        var window = new DuplicateDownloadLinkWindow { DataContext = viewModel };
        DuplicateDownloadLinkAction? action;
        // If the owner window is not visible
        if (!ownerWindow.IsVisible)
        {
            // Create a new TaskCompletionSource
            var duplicateCompletionSource = new TaskCompletionSource<bool>();
            // Add a Closed event handler to the window
            window.Closed += (_, _) => duplicateCompletionSource.TrySetResult(true);
            // Show the window
            window.Show();
            // Wait for the window to close
            await duplicateCompletionSource.Task;
            // Get the result from the view model
            action = viewModel.GetResult();
        }
        // If the owner window is visible
        else
        {
            // Show the dialog and get the result
            action = await window.ShowDialog<DuplicateDownloadLinkAction?>(ownerWindow);
        }

        Log.Debug("User selected duplicate action: {Action}", action);

        // If the action is null, throw an exception
        return action ?? throw new InvalidOperationException("Duplicate download link action is null.");
    }

    public string GetNewFileName(string url, string fileName, string saveLocation)
    {
        Log.Debug("Generating new file name for duplicate URL: {Url}", url);

        var filePath = Path.Combine(saveLocation, fileName);
        var downloadFiles = DownloadFiles
            .Where(df => df.Url?.Equals(url) == true)
            .ToList();

        if (downloadFiles.Count == 0)
        {
            Log.Debug("No duplicate files found for URL, using original file name");
            return fileName;
        }

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

        Log.Debug("Generated new file name: {NewFileName}", newFileName);
        return newFileName;
    }

    public string GetNewFileName(string fileName, string saveLocation)
    {
        Log.Debug("Generating new file name for duplicate file: {FileName}", fileName);

        var downloadFiles = DownloadFiles
            .Where(df => !df.FileName.IsStringNullOrEmpty()
                         && !df.SaveLocation.IsStringNullOrEmpty()
                         && df.FileName!.Equals(fileName)
                         && df.SaveLocation!.Equals(saveLocation))
            .ToList();

        if (downloadFiles.Count == 0 && !File.Exists(Path.Combine(saveLocation, fileName)))
        {
            Log.Debug("No duplicate files found, using original file name");
            return fileName;
        }

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

        Log.Debug("Generated new file name: {NewFileName}", newFileName);
        return newFileName;
    }

    public void ShowOrFocusDownloadWindow(DownloadFileViewModel? viewModel)
    {
        // Find download file
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == viewModel?.Id);
        if (downloadFile == null)
        {
            Log.Debug("Download file not found for showing/focusing window");
            return;
        }

        Log.Debug("Showing or focusing download window for file with ID: {DownloadFileId}", downloadFile.Id);

        // Find file downloader for the current download file
        var fileDownloader = GetOrCreateFileDownloader(downloadFile, canCreate: false);
        if (fileDownloader == null)
        {
            Log.Warning("File downloader not found for download file with ID: {DownloadFileId}", downloadFile.Id);
            return;
        }

        Dispatcher.UIThread.Post(() => fileDownloader.ShowOrFocusWindow());
    }

    public void AddCompletedTask(DownloadFileViewModel? viewModel, Func<DownloadFileViewModel?, Task> completedTask)
    {
        // Find download file
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == viewModel?.Id);
        if (downloadFile == null)
        {
            Log.Warning("Download file not found for adding completed task");
            return;
        }

        Log.Debug("Adding async completed task for download file with ID: {DownloadFileId}", downloadFile.Id);

        // Find file downloader for the current download file
        var fileDownloader = GetOrCreateFileDownloader(downloadFile)!;
        // Add completed task to file downloader
        fileDownloader.AddAsyncCompletedTask(completedTask);
    }

    public void AddCompletedTask(DownloadFileViewModel? viewModel, Action<DownloadFileViewModel?> completedTask)
    {
        // Find download file
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == viewModel?.Id);
        if (downloadFile == null)
        {
            Log.Warning("Download file not found for adding completed task");
            return;
        }

        Log.Debug("Adding sync completed task for download file with ID: {DownloadFileId}", downloadFile.Id);

        // Find file downloader for the current download file
        var fileDownloader = GetOrCreateFileDownloader(downloadFile)!;
        // Add completed task to file downloader
        fileDownloader.AddSyncCompletedTask(completedTask);
    }

    #region Event handlers

    /// <summary>
    /// Handles the <see cref="DownloadFileViewModel.DownloadFinished"/> event when a download file finished.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The <see cref="DownloadFileEventArgs"/> instance that contains the event data.</param>
    private async void DownloadFileOnDownloadFinished(object? sender, DownloadFileEventArgs e)
    {
        try
        {
            Log.Debug("Download finished event received for file with ID: {DownloadFileId}", e.Id);

            // Wait for add completed blocker
            await _addCompletedBlocker.WaitAsync();
            // Find download file
            var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == e.Id);
            if (downloadFile == null)
            {
                Log.Warning("Download file with ID {DownloadFileId} not found in finished event", e.Id);
                return;
            }

            // Unsubscribe from Finished event
            downloadFile.DownloadFinished -= DownloadFileOnDownloadFinished;
            // Find file downloader for the current download file
            var fileDownloader = GetOrCreateFileDownloader(downloadFile, canCreate: false);
            if (fileDownloader == null)
            {
                Log.Warning("File downloader not found for download file with ID: {DownloadFileId}", downloadFile.Id);
                return;
            }

            // Add download file to completed download files
            _completedDownloads.Enqueue(fileDownloader);

            // Hide download window
            Dispatcher.UIThread.Post(() => fileDownloader.DownloadWindow?.Hide());

            // Check for errors
            if (e.Error != null)
            {
                Log.Error(e.Error, "Download finished with error for file with ID: {DownloadFileId}", downloadFile.Id);
                throw e.Error;
            }

            Log.Debug("Download finished successfully for file with ID: {DownloadFileId}", downloadFile.Id);
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowDangerDialogAsync("Error downloading file",
                $"An error occurred while downloading the file.\nError message: {ex.Message}",
                DialogButtons.Ok);

            Log.Error(ex, "An error occurred while downloading the file. Error message: {ErrorMessage}", ex.Message);
        }
    }

    /// <summary>
    /// Handles the <see cref="DownloadFileViewModel.DownloadStopped"/> event when a download file stopped.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The <see cref="DownloadFileEventArgs"/> instance that contains the event data.</param>
    private async void DownloadFileOnDownloadStopped(object? sender, DownloadFileEventArgs e)
    {
        try
        {
            Log.Debug("Download stopped event received for file with ID: {DownloadFileId}", e.Id);

            var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == e.Id);
            if (downloadFile == null)
            {
                Log.Warning("Download file with ID {DownloadFileId} not found in stopped event", e.Id);
                return;
            }

            // Remove DownloadStopped event from download file
            downloadFile.DownloadStopped -= DownloadFileOnDownloadStopped;
            // Find file downloader for the current download file
            var fileDownloader = GetOrCreateFileDownloader(downloadFile, canCreate: false);
            if (fileDownloader == null)
            {
                Log.Warning("File downloader not found for download file with ID: {DownloadFileId}", downloadFile.Id);
                return;
            }

            // Hide download window
            Dispatcher.UIThread.Post(() => fileDownloader.DownloadWindow?.Hide());

            Log.Debug("Download stopped event processed for file with ID: {DownloadFileId}", downloadFile.Id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while stopping the download. Error message: {ErrorMessage}", ex.Message);

            await DialogBoxManager.ShowDangerDialogAsync("Error stopping download",
                $"An error occurred while stopping the download.\nError message: {ex.Message}",
                DialogButtons.Ok);
        }
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Starts a long-running task that watches for and manages completed downloads.
    /// </summary>
    private void StartWatcher()
    {
        Log.Debug("Starting download file watcher...");

        _watcherCancellationSource = new CancellationTokenSource();

        Task.Factory.StartNew(function: WatchCompletedFilesAsync,
            creationOptions: TaskCreationOptions.LongRunning,
            cancellationToken: _watcherCancellationSource.Token,
            scheduler: TaskScheduler.Default);

        Log.Debug("Download file watcher started successfully");
    }

    /// <summary>
    /// Gets or creates an instance of the <see cref="FileDownloader"/> class for a <see cref="DownloadFileViewModel"/>.
    /// </summary>
    /// <param name="downloadFile">The download file that file downloader belongs to.</param>
    /// <param name="canCreate">Indicates whether the access to create a new <see cref="FileDownloader"/> class.</param>
    /// <returns>Returns an instance of the <see cref="FileDownloader"/> class.</returns>
    private FileDownloader? GetOrCreateFileDownloader(DownloadFileViewModel downloadFile, bool canCreate = true)
    {
        // Try to find file downloader
        var fileDownloader = _downloadingFiles.FirstOrDefault(downloader => downloader.DownloadFile.Id == downloadFile.Id);
        // If file downloader found, return it
        // If the permission for create is false, return null
        if (fileDownloader != null || !canCreate)
        {
            Log.Debug("File downloader {Status} for download file with ID: {DownloadFileId}", fileDownloader != null ? "found" : "not found and cannot create", downloadFile.Id);
            return fileDownloader;
        }

        Log.Debug("Creating new file downloader for download file with ID: {DownloadFileId}", downloadFile.Id);

        // Create an instance of the FileDownloader class for managing download file
        fileDownloader = new FileDownloader(downloadFile);
        // Add file downloader to the list
        _downloadingFiles.Add(fileDownloader);

        Log.Debug("File downloader created successfully for download file with ID: {DownloadFileId}", downloadFile.Id);
        return fileDownloader;
    }

    /// <summary>
    /// Watches the completed download files and manage them.
    /// </summary>
    private async Task WatchCompletedFilesAsync()
    {
        Log.Debug("Download file watcher started monitoring completed downloads");

        while (true)
        {
            try
            {
                // Check if watch operation is cancelled
                if (_watcherCancellationSource?.IsCancellationRequested == true)
                {
                    Log.Debug("Download file watcher cancellation requested");
                    break;
                }

                FileDownloader? fileDownloader;
                try
                {
                    // Check if there is any completed download files
                    if (_completedDownloads.IsEmpty || !_completedDownloads.TryDequeue(out fileDownloader))
                    {
                        await Task.Delay(50);
                        continue;
                    }
                }
                finally
                {
                    // Release the blocker, so the new completed download files can add to the queue
                    _addCompletedBlocker.Release();
                }

                Log.Debug("Processing completed download file with ID: {DownloadFileId}", fileDownloader.DownloadFile.Id);

                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    try
                    {
                        // Get the download file from the file downloader
                        var downloadFile = fileDownloader.DownloadFile;
                        // Remove DownloadFinished event from download file
                        downloadFile.DownloadFinished -= DownloadFileOnDownloadFinished;
                        // Remove DownloadStopped event from download file
                        downloadFile.DownloadStopped -= DownloadFileOnDownloadStopped;

                        // Reset time left and transfer rate
                        downloadFile.TimeLeft = null;
                        downloadFile.TransferRate = null;
                        // Store the download queue id for next usage
                        // DownloadQueueId will be set to null but in the completed tasks (that is used below) we need it
                        // So we store it in TempDownloadQueueId and after using it, we set it to null
                        downloadFile.TempDownloadQueueId = downloadFile.DownloadQueueId;

                        // Check if download file is completed
                        if (downloadFile.IsCompleted)
                        {
                            Log.Debug("Download file with ID {DownloadFileId} completed successfully", downloadFile.Id);

                            // When download completed, set these properties to null
                            // We don't need them anymore
                            downloadFile.DownloadPackage = null;
                            downloadFile.DownloadQueueId = null;
                            downloadFile.DownloadQueueName = null;
                            downloadFile.DownloadQueuePriority = null;

                            // Check if download file size is unknown
                            // Set download file size is unknown flag to false, because download file is completed and we know the size
                            if (downloadFile.IsSizeUnknown)
                                downloadFile.IsSizeUnknown = false;

                            // Play download completed sound
                            if (_settingsService.Settings.UseDownloadCompleteSound)
                            {
                                Log.Debug("Playing download completed sound");
                                _ = AudioManager.PlayAsync(AppNotificationType.DownloadCompleted);
                            }

                            // Show complete download dialog when user want's this
                            if (_settingsService.Settings.ShowCompleteDownloadDialog && !downloadFile.IsRunningInQueue)
                            {
                                Log.Debug("Showing complete download dialog for file with ID: {DownloadFileId}", downloadFile.Id);
                                // Run this code on UI thread
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
                            Log.Debug("Download file with ID {DownloadFileId} completed with error", downloadFile.Id);
                            // Play download failed sound
                            if (_settingsService.Settings.UseDownloadFailedSound)
                            {
                                Log.Debug("Playing download failed sound");
                                _ = AudioManager.PlayAsync(AppNotificationType.DownloadFailed);
                            }
                        }
                        else if (downloadFile.IsStopped)
                        {
                            Log.Debug("Download file with ID {DownloadFileId} was stopped", downloadFile.Id);
                            // Play download stopped sound
                            if (_settingsService.Settings.UseDownloadStoppedSound && downloadFile.PlayStopSound)
                            {
                                Log.Debug("Playing download stopped sound");
                                _ = AudioManager.PlayAsync(AppNotificationType.DownloadStopped);
                            }

                            downloadFile.PlayStopSound = true;
                        }

                        // Reset is running in queue flag
                        downloadFile.IsRunningInQueue = false;
                        // Get all completed tasks that should run when download file is completed.
                        var completedAsyncTasks = fileDownloader.GetCompletedAsyncTasks();
                        // Check if there is any completed tasks
                        if (completedAsyncTasks.Count > 0)
                        {
                            Log.Debug("Executing {Count} async completed tasks for file with ID: {DownloadFileId}", completedAsyncTasks.Count, downloadFile.Id);
                            // Iterate through all completed tasks and invoke them
                            foreach (var completedTask in completedAsyncTasks)
                            {
                                try
                                {
                                    await completedTask.Invoke(downloadFile);
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, "An error occurred while invoking the completed task. Error message: {ErrorMessage}", ex.Message);
                                    await DialogBoxManager.ShowErrorDialogAsync(ex);
                                }
                            }

                            // Clear all completed tasks
                            fileDownloader.ClearCompletedAsyncTasks();
                        }

                        // Get all completed sync tasks that should run when download file is completed.
                        var completedSyncTasks = fileDownloader.GetCompletedSyncTasks();
                        // Check if there is any completed tasks
                        if (completedSyncTasks.Count > 0)
                        {
                            Log.Debug("Executing {Count} sync completed tasks for file with ID: {DownloadFileId}", completedSyncTasks.Count, downloadFile.Id);
                            // Iterate through all completed tasks and invoke them
                            foreach (var completedTask in completedSyncTasks)
                            {
                                try
                                {
                                    completedTask.Invoke(downloadFile);
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, "An error occurred while invoking the completed task. Error message: {ErrorMessage}", ex.Message);
                                    await DialogBoxManager.ShowErrorDialogAsync(ex);
                                }
                            }

                            // Clear all completed tasks
                            fileDownloader.ClearCompletedSyncTasks();
                        }

                        // Set temp download queue id to null
                        downloadFile.TempDownloadQueueId = null;
                        // Update download file
                        await UpdateDownloadFileAsync(downloadFile);
                        // Close the download window
                        fileDownloader.DownloadWindow?.Close();

                        // Set the completely stopped flag to true
                        // With this flag we can use EnsureDownloadFileStoppedAsync without any problem
                        downloadFile.IsCompletelyStopped = true;

                        // Remove file downloader from the downloading files list
                        var originalFileDownloader = _downloadingFiles.FirstOrDefault(downloader => downloader.DownloadFile.Id == downloadFile.Id);
                        if (originalFileDownloader != null)
                        {
                            _downloadingFiles.Remove(originalFileDownloader);
                            Log.Debug("File downloader removed from downloading files list for file with ID: {DownloadFileId}", downloadFile.Id);
                        }

                        Log.Debug("Completed processing download file with ID: {DownloadFileId}", downloadFile.Id);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "An error occurred while processing the download file. Error message: {ErrorMessage}", ex.Message);
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while processing the download file. Error message: {ErrorMessage}", ex.Message);
            }
        }

        Log.Debug("Download file watcher stopped");
    }

    /// <summary>
    /// Ensures that the download file is completely stopped.
    /// </summary>
    /// <param name="downloadFileId">The ID of the download file to check.</param>
    private async Task EnsureDownloadFileStoppedAsync(int downloadFileId)
    {
        Log.Debug("Ensuring download file with ID {DownloadFileId} is completely stopped", downloadFileId);

        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == downloadFileId);
        if (downloadFile == null)
        {
            Log.Warning("Download file with ID {DownloadFileId} not found for ensuring stop", downloadFileId);
            return;
        }

        // Check if the stop operation is finished
        while (!downloadFile.IsCompletelyStopped)
        {
            Log.Debug("Waiting for download file with ID {DownloadFileId} to completely stop...", downloadFileId);
            await Task.Delay(50);
        }

        Log.Debug("Download file with ID {DownloadFileId} is completely stopped", downloadFileId);
    }

    /// <summary>
    /// ِDeletes duplicate download files with the same url from the database.
    /// </summary>
    /// <param name="url">The url of the download files to delete.</param>
    private async Task DeleteDuplicateDownloadFilesAsync(string url)
    {
        Log.Debug("Deleting duplicate download files for URL: {Url}", url);

        // Get download files with the same URL
        var downloadFiles = DownloadFiles.Where(df => df.Url?.Equals(url) == true).ToList();
        // Check if there is any download files with the same URL
        if (downloadFiles.Count == 0)
        {
            Log.Debug("No duplicate download files found for URL: {Url}", url);
            return;
        }

        Log.Debug("Found {Count} duplicate download files for URL: {Url}", downloadFiles.Count, url);

        // Delete all download files with the same URL
        foreach (var downloadFile in downloadFiles)
        {
            Log.Debug("Deleting duplicate download file with ID: {DownloadFileId}", downloadFile.Id);
            await DeleteDownloadFileAsync(downloadFile, alsoDeleteFile: true, reloadData: false);
        }

        // Reload download files
        await LoadDownloadFilesAsync();

        Log.Debug("Successfully deleted {Count} duplicate download files for URL: {Url}", downloadFiles.Count, url);
    }

    /// <summary>
    /// Shows a dialog to the user and notify him/her about download status when the download file is completed.
    /// Otherwise, resumes the download file.
    /// </summary>
    /// <param name="url">The url of the download file.</param>
    /// <returns>Returns the download file.</returns>
    /// <exception cref="InvalidOperationException">If the download file is not found or the owner window is not found or the app service is not found.</exception>
    private async Task<DownloadFileViewModel> ShowCompleteDownloadDialogOrResumeAsync(string url)
    {
        Log.Debug("Showing complete download dialog or resuming for URL: {Url}", url);

        // Try to find the download file
        var downloadFile = DownloadFiles.LastOrDefault(df => df.Url?.Equals(url) == true);
        if (downloadFile == null)
            throw new InvalidOperationException("No duplicate download files found.");

        if (downloadFile.IsCompleted)
        {
            Log.Debug("Download file is completed, showing complete dialog");
            // Try to find the owner of the complete download window
            var ownerWindow = App.Desktop?.Windows.FirstOrDefault(w => w.IsFocused) ?? App.Desktop?.MainWindow;
            // Try to find app service
            var appService = Application.Current?.GetServiceProvider().GetService<IAppService>();
            if (appService == null)
                throw new InvalidOperationException("Can't find app service.");

            // Create view model for the complete download window
            var viewModel = new CompleteDownloadWindowViewModel(appService, downloadFile);
            // Create an instance of the complete download window
            var window = new CompleteDownloadWindow { DataContext = viewModel };

            // Check if ownerWindow is null or the IsVisible property is false
            if (ownerWindow is not { IsVisible: true })
            {
                // If the ownerWindow is null or the IsVisible property is false, show the window as a dialog
                // Use a TaskCompletionSource to handle the operation asynchronously
                var tsc = new TaskCompletionSource<bool>();
                window.Closed += (_, _) => tsc.TrySetResult(true);
                window.Show();

                await tsc.Task;
            }
            else
            {
                await window.ShowDialog(ownerWindow);
            }
        }
        else
        {
            Log.Debug("Download file is not completed, resuming download");
            // Check if the download file is paused, resume it and show the download window.
            if (downloadFile.IsPaused)
            {
                // Find file downloader for the current download file
                var fileDownloader = GetOrCreateFileDownloader(downloadFile, canCreate: false);
                if (fileDownloader?.DownloadWindow == null)
                    throw new InvalidOperationException("Download window not found.");

                // Resume download file
                ResumeDownloadFile(downloadFile);

                // Show download window if it is not visible
                if (!fileDownloader.DownloadWindow.IsVisible)
                    Dispatcher.UIThread.Post(() => fileDownloader.DownloadWindow.Show());
            }
            // Otherwise, start the download file
            else
            {
                _ = StartDownloadFileAsync(downloadFile);
            }
        }

        Log.Debug("Completed show complete download dialog or resume for URL: {Url}", url);
        return downloadFile;
    }

    /// <summary>
    /// Checks the available disk space and shows a dialog to the user if there is not enough space.
    /// </summary>
    /// <param name="downloadFile">The download file to check.</param>
    /// <returns>Returns true if there is enough space, otherwise false.</returns>
    private async Task<bool> CheckDiskSpaceAsync(DownloadFileViewModel downloadFile)
    {
        Log.Debug("Checking disk space for download file with ID: {DownloadFileId}", downloadFile.Id);

        // Make sure save location and file name are not empty
        if (downloadFile.SaveLocation.IsStringNullOrEmpty() || downloadFile.FileName.IsStringNullOrEmpty())
        {
            Log.Warning("Save location or file name is empty for download file with ID: {DownloadFileId}", downloadFile.Id);
            await DialogBoxManager.ShowDangerDialogAsync("Storage not found",
                "An error occurred while trying to determine the storage location for this file. This may be due to a system error or missing file information.",
                DialogButtons.Ok);

            return false;
        }

        // Create save directory before calculating available free space
        if (!Directory.Exists(downloadFile.SaveLocation))
        {
            Log.Debug("Creating save directory: {SaveLocation}", downloadFile.SaveLocation);
            Directory.CreateDirectory(downloadFile.SaveLocation!);
        }

        // Find file if exists and get it's size
        var filePath = Path.Combine(downloadFile.SaveLocation!, downloadFile.FileName!);
        long downloadedSize = 0;
        if (File.Exists(filePath))
        {
            downloadedSize = new FileInfo(filePath).Length;
            Log.Debug("Existing file found with size: {FileSize} bytes", downloadedSize);
        }

        // When the server does not include Content-Length in the request header, the validation of the amount of free disk space will be correct.
        if (downloadFile.IsSizeUnknown)
        {
            Log.Debug("File size is unknown, skipping disk space check");
            return true;
        }

        // Make sure size is greater than 0
        if (downloadFile.Size is null or <= 0)
        {
            Log.Warning("File size is invalid for download file with ID: {DownloadFileId}", downloadFile.Id);
            await DialogBoxManager.ShowDangerDialogAsync("Unknown file size",
                "The size of the file you're trying to download is unknown, so it's not possible to determine how much storage space is required on your hard drive.",
                DialogButtons.Ok);

            return false;
        }

        // Find the drive where the file is stored and check the free space
        var driveInfo = new DriveInfo(downloadFile.SaveLocation!);
        var hasEnoughSpace = driveInfo.AvailableFreeSpace >= downloadFile.Size!.Value - downloadedSize;
        if (!hasEnoughSpace)
        {
            Log.Warning("Insufficient disk space on drive {Drive} for download file with ID: {DownloadFileId}", driveInfo.Name, downloadFile.Id);
            await DialogBoxManager.ShowDangerDialogAsync("Insufficient disk space",
                $"There is not enough free space available on the path '{downloadFile.SaveLocation}'.",
                DialogButtons.Ok);

            return false;
        }

        downloadedSize = 0;
        var tempFileLocation = _settingsService.GetTemporaryFileLocation();
        var tempDirectory = Path.Combine(tempFileLocation, Path.GetFileNameWithoutExtension(downloadFile.FileName!));
        if (Directory.Exists(tempDirectory))
        {
            var tempFiles = Directory.GetFiles(tempDirectory, "*.tmp");
            downloadedSize += tempFiles.Select(tempFile => new FileInfo(tempFile)).Select(fileInfo => fileInfo.Length).Sum();
            Log.Debug("Temporary files size: {TempSize} bytes", downloadedSize);
        }

        driveInfo = new DriveInfo(tempFileLocation);
        var requiredSpace = downloadFile.Size!.Value - downloadedSize;
        hasEnoughSpace = driveInfo.AvailableFreeSpace >= requiredSpace;
        if (hasEnoughSpace)
        {
            Log.Debug("Sufficient disk space available for download file with ID: {DownloadFileId}", downloadFile.Id);
            return true;
        }

        // If there is no available space, notify user
        Log.Warning("Insufficient disk space in temp location {TempLocation} for download file with ID: {DownloadFileId}", tempFileLocation, downloadFile.Id);
        await DialogBoxManager.ShowDangerDialogAsync("Insufficient disk space",
            $"There is not enough free space available on the path '{tempFileLocation}'." +
            $" Please ensure that at least {requiredSpace.ToFileSize()} of free space is available, or change the temporary file storage location from the settings.",
            DialogButtons.Ok);

        return false;
    }

    /// <summary>
    /// Checks whether the url is duplicate.
    /// Iterates through the download files and checks if the url is the same.
    /// </summary>
    /// <param name="url">The url to check.</param>
    /// <returns>True if the url is duplicate, otherwise false.</returns>
    private bool CheckIsUrlDuplicate(string? url)
    {
        var isDuplicate = !url.IsStringNullOrEmpty() && DownloadFiles.FirstOrDefault(df => !df.Url.IsStringNullOrEmpty() && df.Url!.Equals(url)) != null;
        Log.Debug("URL {Url} is {DuplicateState}", url, isDuplicate ? "duplicate" : "not duplicate");
        return isDuplicate;
    }

    /// <summary>
    /// Checks whether the path that the file will be saved to is the same as another file.
    /// Iterates through the download files and checks if the file name and save directory are the same.
    /// </summary>
    /// <param name="fileName">The file name to check.</param>
    /// <param name="saveDirectory">The path that the file will be saved to.</param>
    /// <returns>True if the path is duplicate, otherwise false.</returns>
    private bool CheckIsFileNameDuplicate(string? fileName, string? saveDirectory)
    {
        var isDuplicate = !fileName.IsStringNullOrEmpty()
                          && !saveDirectory.IsStringNullOrEmpty()
                          && DownloadFiles.FirstOrDefault(df => df.FileName?.Equals(fileName) == true && df.SaveLocation?.Equals(saveDirectory) == true) != null;

        Log.Debug("File name {FileName} in {SaveDirectory} is {DuplicateState}", fileName, saveDirectory, isDuplicate ? "duplicate" : "not duplicate");
        return isDuplicate;
    }

    /// <summary>
    /// Handles the duplicate URLs.
    /// If there is a duplicate URL, based on the application settings, this method handle the duplicate download file and change its name of something else.
    /// </summary>
    /// <param name="downloadFile">The download file to handle.</param>
    /// <returns>In some cases, we have to use an existing download file and then return it.</returns>
    /// <exception cref="InvalidOperationException">If there is a duplicate URL and the user chose to let user choose how to handle the duplicate URL, but the user didn't choose anything, this exception will be thrown.</exception>
    private async Task<DownloadFileViewModel?> HandleDuplicateUrlAsync(DownloadFile downloadFile)
    {
        Log.Debug("Handling duplicate URL: {Url}", downloadFile.Url);

        // Define the result
        DownloadFileViewModel? result = null;
        // Get the option that user chose
        var savedDuplicateAction = _settingsService.Settings.DuplicateDownloadLinkAction;
        // If the user chose to let user choose, we have to show a dialog to user and get the result
        var duplicateAction = savedDuplicateAction == DuplicateDownloadLinkAction.LetUserChoose
            ? await GetUserDuplicateActionAsync(downloadFile.Url, downloadFile.FileName, downloadFile.SaveLocation)
            : savedDuplicateAction;

        Log.Debug("Duplicate action selected: {DuplicateAction}", duplicateAction);

        // Make sure duplicate action has value
        if (duplicateAction is DuplicateDownloadLinkAction.LetUserChoose)
            throw new InvalidOperationException("When you want to add a duplicate URL, you must specify how to handle this URL.");

        // Handle duplicate
        switch (duplicateAction)
        {
            // Handle duplicate with a number at the end of the file name
            case DuplicateDownloadLinkAction.DuplicateWithNumber:
            {
                Log.Debug("Handling duplicate with number suffix");
                // Get new file name with number at the end
                var newFileName = GetNewFileName(downloadFile.Url, downloadFile.FileName, downloadFile.SaveLocation);
                // Make sure file name is not empty
                if (newFileName.IsStringNullOrEmpty())
                    throw new InvalidOperationException("New file name for duplicate link is null or empty.");

                // Change download file name
                downloadFile.FileName = newFileName;
                Log.Debug("File name changed to: {NewFileName}", newFileName);
                break;
            }

            // Handle duplicate and overwrite existing file
            case DuplicateDownloadLinkAction.OverwriteExisting:
            {
                Log.Debug("Handling duplicate by overwriting existing files");
                // Delete previous file and replace new one
                await DeleteDuplicateDownloadFilesAsync(downloadFile.Url);
                break;
            }

            // Handle duplicate with showing complete download dialog or resume file
            case DuplicateDownloadLinkAction.ShowCompleteDialogOrResume:
            {
                Log.Debug("Handling duplicate by showing complete dialog or resuming");
                // Show complete dialog or resume file
                result = await ShowCompleteDownloadDialogOrResumeAsync(downloadFile.Url);
                break;
            }

            // At this point, duplicate download action is invalid
            case DuplicateDownloadLinkAction.LetUserChoose:
            default:
                throw new InvalidOperationException("Duplicate download link action is invalid.");
        }

        Log.Debug("Duplicate URL handling completed for: {Url}", downloadFile.Url);
        return result;
    }

    /// <summary>
    /// Handles download files with the same file name and save location.
    /// </summary>
    /// <param name="downloadFile">The download file to handle.</param>
    /// <exception cref="InvalidOperationException">If the new file name for the download file is null or empty, this exception will be thrown.</exception>
    private void HandleDuplicateFileName(DownloadFile downloadFile)
    {
        Log.Debug("Handling duplicate file name: {FileName}", downloadFile.FileName);

        // Find duplicate download file
        var duplicateDownloadFile = DownloadFiles
            .FirstOrDefault(df => df.FileName?.Equals(downloadFile.FileName) == true && df.SaveLocation?.Equals(downloadFile.SaveLocation) == true);

        // Find download file on system and if it exists, we have to choose another name for download file
        var duplicateFilePath = Path.Combine(downloadFile.SaveLocation, downloadFile.FileName);
        if (duplicateDownloadFile == null && !File.Exists(duplicateFilePath))
        {
            Log.Debug("No duplicate file name found");
            return;
        }

        Log.Debug("Duplicate file name found, generating new file name");

        // Get new file name and make sure it's not empty
        var newFileName = GetNewFileName(downloadFile.FileName, downloadFile.SaveLocation);
        if (newFileName.IsStringNullOrEmpty())
            throw new InvalidOperationException("New file name for duplicate file is null or empty.");

        // Change file name
        downloadFile.FileName = newFileName;
        Log.Debug("File name changed to: {NewFileName}", newFileName);
    }

    /// <summary>
    /// Removes the temporary files of the download file from the storage.
    /// </summary>
    /// <param name="downloadFile">The download file to remove the temporary files from.</param>
    private async Task RemoveTempFilesAsync(DownloadFileViewModel downloadFile)
    {
        Log.Debug("Removing temporary files for download file with ID: {DownloadFileId}", downloadFile.Id);

        // Make sure the file name is not null or empty
        if (downloadFile.FileName.IsStringNullOrEmpty())
            throw new InvalidOperationException("The file name of the download file is undefined.");

        // Get the directory path from the settings
        var fileName = Path.GetFileNameWithoutExtension(downloadFile.FileName!);
        var directoryPath = Path.Combine(_settingsService.Settings.TemporaryFileLocation, fileName);
        // Remove temporary directory with all files in it
        if (Directory.Exists(directoryPath))
        {
            Log.Debug("Removing temporary directory: {DirectoryPath}", directoryPath);
            var deleteResult = await Rubbish.MoveAsync(directoryPath);
            if (!deleteResult)
                throw new InvalidOperationException("Could not remove the temporary directory.");
        }

        // Get download package
        var downloadPackage = downloadFile.GetDownloadPackage();
        // Check if the download package is not null, get the temporary save path and remove the temporary directory with all files in it
        if (downloadPackage != null && Directory.Exists(downloadPackage.TemporarySavePath))
        {
            Log.Debug("Removing download package temporary directory: {DirectoryPath}", downloadPackage.TemporarySavePath);
            var deleteResult = await Rubbish.MoveAsync(downloadPackage.TemporarySavePath);
            if (!deleteResult)
                throw new InvalidOperationException("Could not remove the temporary directory.");
        }

        Log.Debug("Temporary files removed successfully for download file with ID: {DownloadFileId}", downloadFile.Id);
    }

    /// <summary>
    /// Gets the file type of download file.
    /// </summary>
    /// <param name="downloadFile">The download file to get the file type of.</param>
    /// <returns>The file type of the download file.</returns>
    private string GetFileType(DownloadFileViewModel downloadFile)
    {
        Log.Debug("Getting file type for download file with ID: {DownloadFileId}", downloadFile.Id);

        // Declare a string variable to store the file type
        string fileType;
        // Get the file extension from the downloadFile
        var ext = Path.GetExtension(downloadFile.FileName);
        // Get the file extension from the category service
        var fileExtension = _categoryService
            .Categories
            .FirstOrDefault(c => c.Id == downloadFile.CategoryId)?
            .FileExtensions
            .FirstOrDefault(fe => fe.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase));

        // If the file extension is null
        if (fileExtension == null)
        {
            // Get the file extension from the category service
            var fileExtensions = _categoryService
                .Categories
                .SelectMany(c => c.FileExtensions)
                .FirstOrDefault(fe => fe.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase));

            // Set the file type to the alias of the file extension or the unknown file type
            fileType = fileExtensions?.Alias ?? Constants.UnknownFileType;
        }
        else
        {
            // Set the file type to the alias of the file extension
            fileType = fileExtension.Alias;
        }

        Log.Debug("File type determined as: {FileType} for download file with ID: {DownloadFileId}", fileType, downloadFile.Id);
        return fileType;
    }

    #endregion
}