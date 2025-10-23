using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
    private readonly SemaphoreSlim _addCompletedBlocker;
    private readonly ConcurrentQueue<FileDownloader> _completedDownloads = [];
    private readonly ObservableCollection<FileDownloader> _downloadingFiles = [];
    private CancellationTokenSource? _watcherCancellationSource;
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
            await DeleteDownloadFileAsync(downloadFile, alsoDeleteFile: false, reloadData: false);

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

        // Set file type for each download file
        foreach (var downloadFile in DownloadFiles)
            downloadFile.FileType = GetFileType(downloadFile);

        // Notify download files changed
        OnPropertyChanged(nameof(DownloadFiles));
        // Raise changed event
        DataChanged?.Invoke(this, EventArgs.Empty);
        // Log information
        Log.Information("Download files loaded successfully.");
    }

    public async Task<DownloadFileViewModel?> AddDownloadFileAsync(DownloadFileViewModel viewModel, bool startDownloading = false)
    {
        // Validate download file
        var isValid = await ValidateDownloadFileAsync(viewModel);
        if (!isValid)
            return null;

        // Find category
        var category = _categoryService.Categories.FirstOrDefault(c => c.Id == viewModel.CategoryId);
        // Set save location
        // Check if the save location has value, use it
        // Otherwise, get the save location from the categories
        var saveLocation = viewModel.SaveLocation.IsStringNullOrEmpty()
            ? _settingsService.Settings.DisableCategories
                ? _settingsService.Settings.GlobalSaveLocation!
                : category!.CategorySaveDirectory!.SaveDirectory
            : viewModel.SaveLocation!;

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
            CategoryId = category!.Id,
            SaveLocation = saveLocation,
            DownloadProgress = viewModel.DownloadProgress is > 0 ? viewModel.DownloadProgress.Value : 0,
            DownloadPackage = viewModel.DownloadPackage
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
                    result = await HandleDuplicateUrlAsync(downloadFile);
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
            return result;

        // Check if the file name is duplicate and handle it if its exists
        var isFileNameDuplicate = CheckIsFileNameDuplicate(downloadFile.FileName, downloadFile.SaveLocation);
        // Make sure each file has a unique name
        if (viewModel.IsFileNameDuplicate || isFileNameDuplicate)
        {
            if (isFileNameDuplicate)
                HandleDuplicateFileName(downloadFile);
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

    public async Task<DownloadFileViewModel?> AddDownloadFileAsync(string? url, bool startDownloading = false)
    {
        if (url.IsStringNullOrEmpty() || !url.CheckUrlValidation())
            return null;

        var downloadFile = await GetDownloadFileFromUrlAsync(url);
        return await AddDownloadFileAsync(downloadFile, startDownloading);
    }

    public async Task UpdateDownloadFileAsync(DownloadFileViewModel viewModel)
    {
        var downloadFileViewModel = DownloadFiles.FirstOrDefault(df => df.Id == viewModel.Id);
        if (downloadFileViewModel == null)
            return;

        var downloadFile = _mapper.Map<DownloadFile>(downloadFileViewModel);
        await _unitOfWork.DownloadFileRepository.UpdateAsync(downloadFile);
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
        await _unitOfWork.DownloadFileRepository.UpdateAllAsync(downloadFiles);
        await _unitOfWork.SaveAsync();

        await LoadDownloadFilesAsync();
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

        // Remove temp files of download file
        await RemoveTempFilesAsync(downloadFile);
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

            // Create an instance of the FileDownloader class for managing download file
            var fileDownloader = GetOrCreateFileDownloader(downloadFile)!;
            // Create a window for showing the download progress
            fileDownloader.CreateDownloadWindow(showWindow);

            // Subscribe to the events
            downloadFile.DownloadFinished += DownloadFileOnDownloadFinished;
            downloadFile.DownloadStopped += DownloadFileOnDownloadStopped;
            // Start download
            await fileDownloader.StartDownloadFileAsync();
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
            return;

        // Reset the completely state of the download file
        downloadFile.IsCompletelyStopped = false;
        // Find file downloader for the current download file
        var fileDownloader = GetOrCreateFileDownloader(downloadFile, canCreate: false);
        if (fileDownloader == null)
            return;

        // Change the play sound flag
        downloadFile.PlayStopSound = playSound;
        // Stop download file
        await fileDownloader.StopDownloadFileAsync();

        // Wait for the download to stop
        if (ensureStopped)
            await EnsureDownloadFileStoppedAsync(downloadFile.Id);
    }

    public void ResumeDownloadFile(DownloadFileViewModel? viewModel)
    {
        // Find download file
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == viewModel?.Id);
        // Check download file state
        if (downloadFile is not { IsPaused: true })
            return;

        // Find file downloader for the current download file
        var fileDownloader = GetOrCreateFileDownloader(downloadFile, canCreate: false);
        if (fileDownloader == null)
            return;

        // Resume download file
        fileDownloader.ResumeDownloadFile();
        // Show or focus download window
        ShowOrFocusDownloadWindow(downloadFile);
    }

    public void PauseDownloadFile(DownloadFileViewModel? viewModel)
    {
        // Find download file
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == viewModel?.Id);
        // Check download file state
        if (downloadFile == null || downloadFile.IsPaused)
            return;

        // Find file downloader for the current download file
        var fileDownloader = GetOrCreateFileDownloader(downloadFile, canCreate: false);
        // Pause download file
        fileDownloader?.PauseDownloadFile();
    }

    public void LimitDownloadFileSpeed(DownloadFileViewModel? viewModel, long speed)
    {
        // Find download file
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == viewModel?.Id);
        if (downloadFile == null)
            return;

        // Find file downloader for the current download file
        var fileDownloader = GetOrCreateFileDownloader(downloadFile, canCreate: false);
        if (fileDownloader == null)
            return;

        // Change the maximum bytes per second speed
        fileDownloader.DownloadConfiguration.MaximumBytesPerSecond = speed;
    }

    public async Task RedownloadDownloadFileAsync(DownloadFileViewModel? viewModel, bool showWindow = true)
    {
        // Try to find the download file
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == viewModel?.Id);
        if (downloadFile == null)
            return;

        // Reset the properties of the download file
        downloadFile.Reset();
        // Remove temp files of download file
        await RemoveTempFilesAsync(downloadFile);
        // Remove download file from storage as well
        if (!downloadFile.SaveLocation.IsStringNullOrEmpty() && !downloadFile.FileName.IsStringNullOrEmpty())
        {
            var filePath = Path.Combine(downloadFile.SaveLocation!, downloadFile.FileName!);
            if (File.Exists(filePath))
                await Rubbish.MoveAsync(filePath);
        }

        // Update download file with the new data
        await UpdateDownloadFileAsync(downloadFile);
        // Start downloading the file
        _ = StartDownloadFileAsync(downloadFile, showWindow);
    }

    public string GetDownloadSpeed()
    {
        var downloadSpeed = DownloadFiles.Where(df => df.IsDownloading).Sum(df => df.TransferRate ?? 0);
        return downloadSpeed.ToFileSize();
    }

    public async Task<DownloadFileViewModel> GetDownloadFileFromUrlAsync(string? url, CancellationToken cancellationToken = default)
    {
        var downloadFile = new DownloadFileViewModel
        {
            // Change the URL to correct format
            Url = url?.Replace("\\", "/").Trim()
        };

        // Check if the URL is valid
        if (!downloadFile.Url.CheckUrlValidation())
            return downloadFile;

        // Create an instance of the DownloadRequest class.
        var request = new DownloadRequest(downloadFile.Url!);
        // Subscribe to the PropertyChanged event and watch the Url property.
        // When the URL is changed, update the URL of the download file.
        request.PropertyChanged += (_, e) => UpdateDownloadFileUrl(downloadFile, request, e.PropertyName);
        await request.FetchResponseHeadersAsync(cancellationToken);
        // Check if response headers fetched successfully
        if (request.ResponseHeaders.Count == 0)
            return downloadFile;

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
            return downloadFile;

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
        // Check if the URL has Content-Length header
        downloadFile.IsSizeUnknown = !request.ResponseHeaders.TryGetValue("Content-Length", out var contentLength);
        // Set file size
        downloadFile.Size = downloadFile.IsSizeUnknown ? 0 : long.TryParse(contentLength, out var size) ? size : 0;

        // find category item by file extension
        var extension = Path.GetExtension(downloadFile.FileName);
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
            fileExtension = customCategory
                .FileExtensions
                .FirstOrDefault(fe => fe.Extension.Equals(extension, StringComparison.CurrentCultureIgnoreCase));
        }
        // Otherwise, there is no custom category, so we need to find the default category.
        else
        {
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
            category = _categoryService
                .Categories
                .FirstOrDefault(c => c.AutoAddedLinksFromSitesList.Contains(siteDomain!));
        }

        // Find category by file extension
        if (category == null)
        {
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
        // Find a download file with the same url
        downloadFile.IsUrlDuplicate = CheckIsUrlDuplicate(downloadFile.Url);
        // Find a download file with the same name
        downloadFile.IsFileNameDuplicate = CheckIsFileNameDuplicate(downloadFile.FileName, category?.CategorySaveDirectory?.SaveDirectory);
        // Return the download file
        return downloadFile;
    }

    public async Task<bool> ValidateDownloadFileAsync(DownloadFileViewModel downloadFile, bool showMessage = true)
    {
        // Check url validation
        if (downloadFile.Url.IsStringNullOrEmpty() || !downloadFile.Url.CheckUrlValidation())
        {
            if (showMessage)
                await DialogBoxManager.ShowDangerDialogAsync("Url", "Please provide a valid URL to continue.", DialogButtons.Ok);

            return false;
        }

        // Check if the categories already disabled or not
        if (_settingsService.Settings.DisableCategories && _settingsService.Settings.GlobalSaveLocation.IsStringNullOrEmpty())
        {
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
            if (showMessage)
            {
                await DialogBoxManager.ShowDangerDialogAsync("File name",
                    "File type is null or invalid. Please choose correct file type like .exe or .zip.",
                    DialogButtons.Ok);
            }

            return false;
        }

        // Check Url point to a file and file size is greater than 0
        if (downloadFile.Size is not (null or <= 0))
            return true;

        // Check is file size unknown
        if (downloadFile.IsSizeUnknown)
            return true;

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

        // If the action is null, throw an exception
        return action ?? throw new InvalidOperationException("Duplicate download link action is null.");
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
        // Find download file
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == viewModel?.Id);
        if (downloadFile == null)
            return;

        // Find file downloader for the current download file
        var fileDownloader = GetOrCreateFileDownloader(downloadFile, canCreate: false);
        if (fileDownloader == null)
            return;

        Dispatcher.UIThread.Post(() => fileDownloader.ShowOrFocusWindow());
    }

    public void AddCompletedTask(DownloadFileViewModel? viewModel, Func<DownloadFileViewModel?, Task> completedTask)
    {
        // Find download file
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == viewModel?.Id);
        if (downloadFile == null)
            return;

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
            return;

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
            // Wait for add completed blocker
            await _addCompletedBlocker.WaitAsync();
            // Find download file
            var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == e.Id);
            if (downloadFile == null)
                return;

            // Unsubscribe from Finished event
            downloadFile.DownloadFinished -= DownloadFileOnDownloadFinished;
            // Find file downloader for the current download file
            var fileDownloader = GetOrCreateFileDownloader(downloadFile, canCreate: false);
            if (fileDownloader == null)
                return;

            // Add download file to completed download files
            _completedDownloads.Enqueue(fileDownloader);

            // Hide download window
            Dispatcher.UIThread.Post(() => fileDownloader.DownloadWindow?.Hide());

            // Check for errors
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

    /// <summary>
    /// Handles the <see cref="DownloadFileViewModel.DownloadStopped"/> event when a download file stopped.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The <see cref="DownloadFileEventArgs"/> instance that contains the event data.</param>
    private async void DownloadFileOnDownloadStopped(object? sender, DownloadFileEventArgs e)
    {
        try
        {
            var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == e.Id);
            if (downloadFile == null)
                return;

            // Remove DownloadStopped event from download file
            downloadFile.DownloadStopped -= DownloadFileOnDownloadStopped;
            // Find file downloader for the current download file
            var fileDownloader = GetOrCreateFileDownloader(downloadFile, canCreate: false);
            if (fileDownloader == null)
                return;

            // Hide download window
            Dispatcher.UIThread.Post(() => fileDownloader.DownloadWindow?.Hide());
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
        _watcherCancellationSource = new CancellationTokenSource();

        Task.Factory.StartNew(function: WatchCompletedFilesAsync,
            creationOptions: TaskCreationOptions.LongRunning,
            cancellationToken: _watcherCancellationSource.Token,
            scheduler: TaskScheduler.Default);
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
            return fileDownloader;

        // Create an instance of the FileDownloader class for managing download file
        fileDownloader = new FileDownloader(downloadFile);
        // Add file downloader to the list
        _downloadingFiles.Add(fileDownloader);

        return fileDownloader;
    }

    /// <summary>
    /// Watches the completed download files and manage them.
    /// </summary>
    private async Task WatchCompletedFilesAsync()
    {
        while (true)
        {
            try
            {
                // Check if watch operation is cancelled
                if (_watcherCancellationSource?.IsCancellationRequested == true)
                    break;

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
                                _ = AudioManager.PlayAsync(AppNotificationType.DownloadCompleted);

                            // Show complete download dialog when user want's this
                            if (_settingsService.Settings.ShowCompleteDownloadDialog && !downloadFile.IsRunningInQueue)
                            {
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

                        // Reset is running in queue flag
                        downloadFile.IsRunningInQueue = false;
                        // Get all completed tasks that should run when download file is completed.
                        var completedAsyncTasks = fileDownloader.GetCompletedAsyncTasks();
                        // Check if there is any completed tasks
                        if (completedAsyncTasks.Count > 0)
                        {
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
                            _downloadingFiles.Remove(originalFileDownloader);
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
    }

    /// <summary>
    /// Ensures that the download file is completely stopped.
    /// </summary>
    /// <param name="downloadFileId">The ID of the download file to check.</param>
    private async Task EnsureDownloadFileStoppedAsync(int downloadFileId)
    {
        var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == downloadFileId);
        if (downloadFile == null)
            return;

        // Check if the stop operation is finished
        while (!downloadFile.IsCompletelyStopped)
            await Task.Delay(50);
    }

    /// <summary>
    /// ِDeletes duplicate download files with the same url from the database.
    /// </summary>
    /// <param name="url">The url of the download files to delete.</param>
    private async Task DeleteDuplicateDownloadFilesAsync(string url)
    {
        // Get download files with the same URL
        var downloadFiles = DownloadFiles.Where(df => df.Url?.Equals(url) == true).ToList();
        // Check if there is any download files with the same URL
        if (downloadFiles.Count == 0)
            return;

        // Delete all download files with the same URL
        foreach (var downloadFile in downloadFiles)
            await DeleteDownloadFileAsync(downloadFile, alsoDeleteFile: true, reloadData: false);

        // Reload download files
        await LoadDownloadFilesAsync();
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
        // Try to find the download file
        var downloadFile = DownloadFiles.LastOrDefault(df => df.Url?.Equals(url) == true);
        if (downloadFile == null)
            throw new InvalidOperationException("No duplicate download files found.");

        if (downloadFile.IsCompleted)
        {
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

        return downloadFile;
    }

    /// <summary>
    /// Checks the available disk space and shows a dialog to the user if there is not enough space.
    /// </summary>
    /// <param name="downloadFile">The download file to check.</param>
    /// <returns>Returns true if there is enough space, otherwise false.</returns>
    private async Task<bool> CheckDiskSpaceAsync(DownloadFileViewModel downloadFile)
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
        {
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
        }

        driveInfo = new DriveInfo(tempFileLocation);
        var requiredSpace = downloadFile.Size!.Value - downloadedSize;
        hasEnoughSpace = driveInfo.AvailableFreeSpace >= requiredSpace;
        if (hasEnoughSpace)
            return true;

        // If there is no available space, notify user
        await DialogBoxManager.ShowDangerDialogAsync("Insufficient disk space",
            $"There is not enough free space available on the path '{tempFileLocation}'." +
            $" Please ensure that at least {requiredSpace.ToFileSize()} of free space is available, or change the temporary file storage location from the settings.",
            DialogButtons.Ok);

        return false;
    }

    /// <summary>
    /// Updates the URL of a download file with the new one.
    /// </summary>
    /// <param name="downloadFile">The download file to update.</param>
    /// <param name="request">The download request sent to the server to retrieve URL information.</param>
    /// <param name="propertyName">The property name to check if it is the URL.</param>
    private static void UpdateDownloadFileUrl(DownloadFileViewModel downloadFile, DownloadRequest request, string? propertyName)
    {
        if (propertyName?.Equals(nameof(DownloadRequest.Url)) != true)
            return;

        downloadFile.Url = request.Url?.AbsoluteUri.Trim();
    }

    /// <summary>
    /// Checks whether the url is duplicate.
    /// Iterates through the download files and checks if the url is the same.
    /// </summary>
    /// <param name="url">The url to check.</param>
    /// <returns>True if the url is duplicate, otherwise false.</returns>
    private bool CheckIsUrlDuplicate(string? url)
    {
        return !url.IsStringNullOrEmpty() && DownloadFiles.FirstOrDefault(df => !df.Url.IsStringNullOrEmpty() && df.Url!.Equals(url)) != null;
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
        return !fileName.IsStringNullOrEmpty()
               && !saveDirectory.IsStringNullOrEmpty()
               && DownloadFiles.FirstOrDefault(df => df.FileName?.Equals(fileName) == true && df.SaveLocation?.Equals(saveDirectory) == true) != null;
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
        // Define the result
        DownloadFileViewModel? result = null;
        // Get the option that user chose
        var savedDuplicateAction = _settingsService.Settings.DuplicateDownloadLinkAction;
        // If the user chose to let user choose, we have to show a dialog to user and get the result
        var duplicateAction = savedDuplicateAction == DuplicateDownloadLinkAction.LetUserChoose
            ? await GetUserDuplicateActionAsync(downloadFile.Url, downloadFile.FileName, downloadFile.SaveLocation)
            : savedDuplicateAction;

        // Make sure duplicate action has value
        if (duplicateAction is DuplicateDownloadLinkAction.LetUserChoose)
            throw new InvalidOperationException("When you want to add a duplicate URL, you must specify how to handle this URL.");

        // Handle duplicate
        switch (duplicateAction)
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

        return result;
    }

    /// <summary>
    /// Handles download files with the same file name and save location.
    /// </summary>
    /// <param name="downloadFile">The download file to handle.</param>
    /// <exception cref="InvalidOperationException">If the new file name for the download file is null or empty, this exception will be thrown.</exception>
    private void HandleDuplicateFileName(DownloadFile downloadFile)
    {
        // Find duplicate download file
        var duplicateDownloadFile = DownloadFiles
            .FirstOrDefault(df => df.FileName?.Equals(downloadFile.FileName) == true && df.SaveLocation?.Equals(downloadFile.SaveLocation) == true);

        // Find download file on system and if it exists, we have to choose another name for download file
        var duplicateFilePath = Path.Combine(downloadFile.SaveLocation, downloadFile.FileName);
        if (duplicateDownloadFile == null && !File.Exists(duplicateFilePath))
            return;

        // Get new file name and make sure it's not empty
        var newFileName = GetNewFileName(downloadFile.FileName, downloadFile.SaveLocation);
        if (newFileName.IsStringNullOrEmpty())
            throw new InvalidOperationException("New file name for duplicate file is null or empty.");

        // Change file name
        downloadFile.FileName = newFileName;
    }

    /// <summary>
    /// Removes the temporary files of the download file from the storage.
    /// </summary>
    /// <param name="downloadFile">The download file to remove the temporary files from.</param>
    private async Task RemoveTempFilesAsync(DownloadFileViewModel downloadFile)
    {
        // Make sure the file name is not null or empty
        if (downloadFile.FileName.IsStringNullOrEmpty())
            throw new InvalidOperationException("The file name of the download file is undefined.");

        // Get the directory path from the settings
        var fileName = Path.GetFileNameWithoutExtension(downloadFile.FileName!);
        var directoryPath = Path.Combine(_settingsService.Settings.TemporaryFileLocation, fileName);
        // Remove temporary directory with all files in it
        if (Directory.Exists(directoryPath))
        {
            var deleteResult = await Rubbish.MoveAsync(directoryPath);
            if (!deleteResult)
                throw new InvalidOperationException("Could not remove the temporary directory.");
        }

        // Get download package
        var downloadPackage = downloadFile.GetDownloadPackage();
        // Check if the download package is not null, get the temporary save path and remove the temporary directory with all files in it
        if (downloadPackage != null && Directory.Exists(downloadPackage.TemporarySavePath))
        {
            var deleteResult = await Rubbish.MoveAsync(downloadPackage.TemporarySavePath);
            if (!deleteResult)
                throw new InvalidOperationException("Could not remove the temporary directory.");
        }
    }

    /// <summary>
    /// Gets the file type of download file.
    /// </summary>
    /// <param name="downloadFile">The download file to get the file type of.</param>
    /// <returns>The file type of the download file.</returns>
    private string GetFileType(DownloadFileViewModel downloadFile)
    {
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

        return fileType;
    }

    #endregion
}