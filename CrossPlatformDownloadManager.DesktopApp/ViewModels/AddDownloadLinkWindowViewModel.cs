using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.AppService;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class AddDownloadLinkWindowViewModel : ViewModelBase
{
    #region Private Fields

    private int? _addedDownloadFileId;
    private bool _isFile;

    // Properties
    private string? _url;
    private ObservableCollection<CategoryViewModel> _categories = [];
    private CategoryViewModel? _selectedCategory;
    private string? _fileName;
    private string? _description;
    private double _fileSize;
    private bool _isLoadingUrl;
    private ObservableCollection<DownloadQueueViewModel> _downloadQueues = [];
    private DownloadQueueViewModel? _selectedDownloadQueue;
    private bool _rememberMyChoice;
    private bool _startQueue;
    private bool _defaultQueueIsExist;

    #endregion

    #region Properties

    public string? Url
    {
        get => _url;
        set => this.RaiseAndSetIfChanged(ref _url, value?.Trim());
    }

    public ObservableCollection<CategoryViewModel> Categories
    {
        get => _categories;
        set => this.RaiseAndSetIfChanged(ref _categories, value);
    }

    public CategoryViewModel? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCategory, value);
            this.RaisePropertyChanged(nameof(FileTypeIcon));
        }
    }

    public string? FileName
    {
        get => _fileName;
        set => this.RaiseAndSetIfChanged(ref _fileName, value);
    }

    public string? Description
    {
        get => _description;
        set => this.RaiseAndSetIfChanged(ref _description, value);
    }

    public string? FileTypeIcon => SelectedCategory?.Icon;

    public double FileSize
    {
        get => _fileSize;
        set => this.RaiseAndSetIfChanged(ref _fileSize, value);
    }

    public bool IsLoadingUrl
    {
        get => _isLoadingUrl;
        set => this.RaiseAndSetIfChanged(ref _isLoadingUrl, value);
    }

    public ObservableCollection<DownloadQueueViewModel> DownloadQueues
    {
        get => _downloadQueues;
        set => this.RaiseAndSetIfChanged(ref _downloadQueues, value);
    }

    public DownloadQueueViewModel? SelectedDownloadQueue
    {
        get => _selectedDownloadQueue;
        set => this.RaiseAndSetIfChanged(ref _selectedDownloadQueue, value);
    }

    public bool RememberMyChoice
    {
        get => _rememberMyChoice;
        set => this.RaiseAndSetIfChanged(ref _rememberMyChoice, value);
    }

    public bool StartQueue
    {
        get => _startQueue;
        set => this.RaiseAndSetIfChanged(ref _startQueue, value);
    }

    public bool DefaultQueueIsExist
    {
        get => _defaultQueueIsExist;
        set => this.RaiseAndSetIfChanged(ref _defaultQueueIsExist, value);
    }

    #endregion

    #region Commands

    public ICommand AddNewCategoryCommand { get; }

    public ICommand AddNewQueueCommand { get; }

    public ICommand AddDownloadFileToDownloadQueueCommand { get; }

    public ICommand AddToDefaultQueueCommand { get; }

    public ICommand StartDownloadCommand { get; }

    public ICommand CancelCommand { get; }

    #endregion

    public AddDownloadLinkWindowViewModel(IAppService appService) : base(appService)
    {
        LoadCategoriesAsync().GetAwaiter();
        LoadDownloadQueues();

        AddNewCategoryCommand = ReactiveCommand.CreateFromTask<Window?>(AddNewCategoryAsync);
        AddNewQueueCommand = ReactiveCommand.CreateFromTask<Window?>(AddNewQueueAsync);
        AddDownloadFileToDownloadQueueCommand = ReactiveCommand.CreateFromTask<Window?>(AddDownloadFileToDownloadQueueAsync);
        AddToDefaultQueueCommand = ReactiveCommand.CreateFromTask<Window?>(AddToDefaultQueueAsync);
        StartDownloadCommand = ReactiveCommand.CreateFromTask<Window?>(StartDownloadAsync);
        CancelCommand = ReactiveCommand.CreateFromTask<Window?>(CancelAsync);
    }

    private async Task CancelAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                throw new InvalidOperationException("An error occured while trying to cancel.");

            owner.Close();
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(ex);
        }
    }

    private async Task StartDownloadAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                throw new InvalidOperationException("An error occured while trying to start download.");

            var isValid = await ValidateDownloadFileAsync();
            if (!isValid)
                return;

            var result = await AddDownloadFileAsync(null);
            if (!result)
                return;

            var downloadFile = AppService
                .DownloadFileService
                .DownloadFiles
                .FirstOrDefault(df => df.Id == _addedDownloadFileId);

            if (downloadFile == null)
            {
                await ShowInfoDialogAsync("File not found",
                    "Inserted file not found. Maybe it was deleted or not inserted correctly. Please try again or contact support team.",
                    DialogButtons.Ok);

                return;
            }

            // Start download before DownloadWindow opened
            _ = AppService
                .DownloadFileService
                .StartDownloadFileAsync(downloadFile);

            var vm = new DownloadWindowViewModel(AppService, downloadFile);
            var window = new DownloadWindow { DataContext = vm };
            window.Show();

            owner.Close(true);
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(ex);
        }
    }

    private async Task AddNewCategoryAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                throw new InvalidOperationException("An error occured while trying to add new category.");

            var vm = new AddEditCategoryWindowViewModel(AppService);
            var window = new AddEditCategoryWindow { DataContext = vm };
            var result = await window.ShowDialog<bool?>(owner);
            if (result != true)
                return;

            await LoadCategoriesAsync();
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(ex);
        }
    }

    private async Task AddNewQueueAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                throw new InvalidOperationException("An error occured while trying to add new queue.");

            var vm = new AddEditQueueWindowViewModel(AppService);
            var window = new AddEditQueueWindow { DataContext = vm };
            var result = await window.ShowDialog<bool?>(owner);
            if (result != true)
                return;

            LoadDownloadQueues();
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(ex);
        }
    }

    private async Task AddDownloadFileToDownloadQueueAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                throw new InvalidOperationException("An error occured while trying to add file to queue.");

            var isValid = await ValidateDownloadFileAsync();
            if (!isValid)
                return;

            if (SelectedDownloadQueue == null)
            {
                await ShowInfoDialogAsync("Queue not selected", "Please select a queue for your file.", DialogButtons.Ok);
                return;
            }

            var downloadQueue = AppService
                .DownloadQueueService
                .DownloadQueues
                .FirstOrDefault(dq => dq.Id == SelectedDownloadQueue.Id);

            if (downloadQueue == null)
            {
                await ShowInfoDialogAsync("Queue not found",
                    "Queue not found. Maybe it was deleted or changed. Please try again or contact support team.",
                    DialogButtons.Ok);

                return;
            }

            var result = await AddDownloadFileAsync(downloadQueue);
            if (!result)
                return;

            if (RememberMyChoice)
            {
                await AppService
                    .DownloadQueueService
                    .ChangeDefaultDownloadQueueAsync(downloadQueue);
            }

            await AppService
                .DownloadQueueService
                .ChangeLastSelectedDownloadQueueAsync(downloadQueue);

            if (StartQueue)
            {
                _ = AppService
                    .DownloadQueueService
                    .StartDownloadQueueAsync(downloadQueue);
            }

            owner.Close(true);
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(ex);
        }
    }

    private async Task AddToDefaultQueueAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            var isValid = await ValidateDownloadFileAsync();
            if (!isValid)
                return;

            var defaultDownloadQueue = AppService
                .DownloadQueueService
                .DownloadQueues
                .FirstOrDefault(dq => dq.IsDefault);

            DefaultQueueIsExist = defaultDownloadQueue != null;
            if (!DefaultQueueIsExist)
                return;

            var result = await AddDownloadFileAsync(defaultDownloadQueue);
            if (!result)
                return;

            owner.Close(true);
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(ex);
        }
    }

    private async Task<bool> AddDownloadFileAsync(DownloadQueueViewModel? downloadQueue)
    {
        var ext = Path.GetExtension(FileName!);
        var fileExtensions = await AppService
            .UnitOfWork
            .CategoryFileExtensionRepository
            .GetAllAsync(where: fe => fe.Extension.ToLower() == ext.ToLower(),
                includeProperties: "Category.CategorySaveDirectory");

        var category = fileExtensions
            .Find(fe => fe.Category?.IsDefault != true)?
            .Category ?? fileExtensions.FirstOrDefault()?.Category;

        if (category?.CategorySaveDirectory == null)
        {
            await ShowInfoDialogAsync("Category", "Can't find save location for this category.", DialogButtons.Ok);
            return false;
        }

        int? maxDownloadQueuePriority = null;
        if (downloadQueue != null)
        {
            maxDownloadQueuePriority = await AppService
                .UnitOfWork
                .DownloadFileRepository
                .GetMaxAsync(selector: df => df.DownloadQueuePriority,
                    where: df => df.DownloadQueueId == downloadQueue.Id) ?? 0;

            maxDownloadQueuePriority++;
        }

        var downloadFile = new DownloadFile
        {
            Url = Url!,
            FileName = FileName!,
            DownloadQueueId = downloadQueue?.Id,
            Size = FileSize,
            Description = Description,
            Status = DownloadFileStatus.None,
            LastTryDate = null,
            DateAdded = DateTime.Now,
            DownloadQueuePriority = maxDownloadQueuePriority,
            CategoryId = category.Id,
            SaveLocation = category.CategorySaveDirectory.SaveDirectory,
        };

        await AppService
            .DownloadFileService
            .AddDownloadFileAsync(downloadFile);

        _addedDownloadFileId = downloadFile.Id;
        return true;
    }

    private async Task<bool> ValidateDownloadFileAsync()
    {
        // Check url validation
        if (Url.IsNullOrEmpty() || !Url.CheckUrlValidation())
        {
            await ShowInfoDialogAsync("Url", "Please enter a valid url.", DialogButtons.Ok);
            return false;
        }

        // Check category
        if (SelectedCategory == null)
        {
            await ShowInfoDialogAsync("Category", "Please choose a category for your file.", DialogButtons.Ok);
            return false;
        }

        // Check file name
        if (FileName.IsNullOrEmpty())
        {
            await ShowInfoDialogAsync("File name", "Please enter a file name.", DialogButtons.Ok);
            return false;
        }

        // Check file extension
        if (!FileName.HasFileExtension())
        {
            await ShowInfoDialogAsync("File name", "File type is null or invalid. Please choose correct file type like .exe or .zip.", DialogButtons.Ok);
            return false;
        }

        // Check Url point to a file and file size is greater than 0
        if (_isFile && FileSize > 0)
            return true;

        await ShowInfoDialogAsync("No file detected",
            "It seems the URL does not point to a file. Make sure the URL points to a file and try again.",
            DialogButtons.Ok);

        return false;
    }

    private void LoadDownloadQueues()
    {
        DownloadQueues = AppService
            .DownloadQueueService
            .DownloadQueues;

        SelectedDownloadQueue = GetSelectedDownloadQueue();
    }

    private DownloadQueueViewModel? GetSelectedDownloadQueue()
    {
        var defaultQueue = DownloadQueues.FirstOrDefault(dq => dq.IsDefault);
        var lastChoice = DownloadQueues.FirstOrDefault(dq => dq.IsLastChoice);
        return defaultQueue ?? lastChoice ?? DownloadQueues.FirstOrDefault();
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var categories = await AppService
                .UnitOfWork
                .CategoryRepository
                .GetAllAsync();

            var viewModels = AppService.Mapper.Map<List<CategoryViewModel>>(categories);
            Categories = viewModels.ToObservableCollection();
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(ex);
        }
    }

    public async Task GetUrlInfoAsync()
    {
        try
        {
            // Set loading flag to true
            IsLoadingUrl = true;

            // Make sure url is valid
            if (!Url.CheckUrlValidation())
                return;

            Url = Url!.Replace("\\", "/").Trim();

            // Find a download file with the same url
            var downloadFileWithSameUrl = AppService
                .DownloadFileService
                .DownloadFiles
                .FirstOrDefault(df => !df.Url.IsNullOrEmpty() && df.Url!.Equals(Url));

            // If download file exist, don't add this url
            if (downloadFileWithSameUrl != null)
            {
                // TODO: Show message box
                return;
            }

            // Create an instance of HttpClient
            using var httpClient = new HttpClient();
            // Send a HEAD request to get the headers only
            using var request = new HttpRequestMessage(HttpMethod.Head, Url);
            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            // Set file flag to true
            _isFile = true;
            // Check if the Content-Type indicates a file
            var contentType = response.Content.Headers.ContentType?.MediaType?.ToLower() ?? string.Empty;
            if (!contentType.StartsWith("application/") &&
                !contentType.StartsWith("image/") &&
                !contentType.StartsWith("video/") &&
                !contentType.StartsWith("audio/") &&
                !contentType.StartsWith("text/"))
            {
                _isFile = false;

                // Check Content-Disposition header
                if (response.Content.Headers.ContentDisposition != null)
                {
                    var dispositionType = response.Content.Headers.ContentDisposition.DispositionType;
                    if (dispositionType.Equals("attachment", StringComparison.OrdinalIgnoreCase))
                        _isFile = true;
                }
            }

            string? fileName = null;
            // Get file name from header if possible
            if (response.Content.Headers.ContentDisposition != null)
                fileName = response.Content.Headers.ContentDisposition.FileName?.Trim('\"') ?? string.Empty;

            // Fallback to using the URL to guess the file name if Content-Disposition is not present
            if (fileName.IsNullOrEmpty())
                fileName = Url!.GetFileName();

            // Set file name
            FileName = fileName ?? string.Empty;
            // Set file size
            FileSize = response.Content.Headers.ContentLength ?? 0;

            // find category item by file extension
            var extension = Path.GetExtension(FileName!);
            // Get all custom categories
            var customCategories = await AppService
                .UnitOfWork
                .CategoryRepository
                .GetAllAsync(where: c => !c.IsDefault, includeProperties: "FileExtensions");

            // Find file extension by extension
            CategoryFileExtension? fileExtension;
            var customCategory = customCategories
                .Find(c => c.FileExtensions.Any(fe => fe.Extension.Equals(extension, StringComparison.CurrentCultureIgnoreCase)));

            if (customCategory != null)
            {
                fileExtension = customCategory.FileExtensions
                    .FirstOrDefault(fe => fe.Extension.Equals(extension, StringComparison.CurrentCultureIgnoreCase));
            }
            else
            {
                fileExtension = await AppService
                    .UnitOfWork
                    .CategoryFileExtensionRepository
                    .GetAsync(where: fe => fe.Extension.ToLower() == extension.ToLower(), includeProperties: "Category");
            }

            // Find category by category file extension or choose general category
            if (fileExtension != null)
            {
                var category = customCategory ?? fileExtension.Category;
                SelectedCategory = category != null ? Categories.FirstOrDefault(c => c.Id == category.Id) : Categories.FirstOrDefault();
            }
            else
            {
                SelectedCategory = Categories.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(ex);
        }
        finally
        {
            IsLoadingUrl = false;
        }
    }

    protected override void OnDownloadQueueServiceDataChanged()
    {
        base.OnDownloadQueueServiceDataChanged();
        LoadDownloadQueues();
    }
}