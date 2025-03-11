using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class AddDownloadLinkWindowViewModel : ViewModelBase
{
    #region Private Fields

    private UrlDetailsResultViewModel? _urlDetails;
    private readonly CancellationTokenSource _cancellationTokenSource;

    private DownloadFileViewModel _downloadFile = new();
    private ObservableCollection<CategoryViewModel> _categories = [];
    private CategoryViewModel? _selectedCategory;
    private ObservableCollection<DownloadQueueViewModel> _downloadQueues = [];
    private DownloadQueueViewModel? _selectedDownloadQueue;
    private bool _isLoadingUrl;
    private bool _rememberMyChoice;
    private bool _startDownloadQueue;
    private bool _defaultDownloadQueueIsExist;
    private bool _categoriesAreDisabled;

    #endregion

    #region Properties

    public DownloadFileViewModel DownloadFile
    {
        get => _downloadFile;
        set
        {
            this.RaiseAndSetIfChanged(ref _downloadFile, value);
            this.RaisePropertyChanged(nameof(IsFileSizeVisible));
        }
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

            DownloadFile.CategoryId = value?.Id;
            this.RaisePropertyChanged(nameof(DownloadFile));
        }
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

    public bool IsLoadingUrl
    {
        get => _isLoadingUrl;
        set => this.RaiseAndSetIfChanged(ref _isLoadingUrl, value);
    }

    public bool RememberMyChoice
    {
        get => _rememberMyChoice;
        set => this.RaiseAndSetIfChanged(ref _rememberMyChoice, value);
    }

    public bool StartDownloadQueue
    {
        get => _startDownloadQueue;
        set => this.RaiseAndSetIfChanged(ref _startDownloadQueue, value);
    }

    public bool DefaultDownloadQueueIsExist
    {
        get => _defaultDownloadQueueIsExist;
        set => this.RaiseAndSetIfChanged(ref _defaultDownloadQueueIsExist, value);
    }

    public bool CategoriesAreDisabled
    {
        get => _categoriesAreDisabled;
        set => this.RaiseAndSetIfChanged(ref _categoriesAreDisabled, value);
    }

    public bool IsFileSizeVisible => DownloadFile.IsSizeUnknown || DownloadFile.Size > 0;

    #endregion

    #region Commands

    public ICommand AddNewCategoryCommand { get; }

    public ICommand AddNewQueueCommand { get; }

    public ICommand AddDownloadFileToDownloadQueueCommand { get; }

    public ICommand AddDownloadFileToDefaultDownloadQueueCommand { get; }

    public ICommand StartDownloadCommand { get; }

    public ICommand CancelCommand { get; }

    #endregion

    public AddDownloadLinkWindowViewModel(IAppService appService) : base(appService)
    {
        _cancellationTokenSource = new CancellationTokenSource();

        LoadCategories();
        LoadDownloadQueues();

        AddNewCategoryCommand = ReactiveCommand.CreateFromTask<Window?>(AddNewCategoryAsync);
        AddNewQueueCommand = ReactiveCommand.CreateFromTask<Window?>(AddNewQueueAsync);
        AddDownloadFileToDownloadQueueCommand = ReactiveCommand.CreateFromTask<Window?>(AddDownloadFileToDownloadQueueAsync);
        AddDownloadFileToDefaultDownloadQueueCommand = ReactiveCommand.CreateFromTask<Window?>(AddDownloadFileToDefaultDownloadQueueAsync);
        StartDownloadCommand = ReactiveCommand.CreateFromTask<Window?>(StartDownloadAsync);
        CancelCommand = ReactiveCommand.CreateFromTask<Window?>(CancelAsync);
    }

    private async Task CancelAsync(Window? owner)
    {
        try
        {
            await _cancellationTokenSource.CancelAsync();
            owner?.Close();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to close the window. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task StartDownloadAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                throw new InvalidOperationException("An error occurred while trying to start download.");

            var downloadFile = await AddDownloadFileAsync();
            if (downloadFile == null)
                return;

            // Start download and open download window
            _ = AppService.DownloadFileService.StartDownloadFileAsync(downloadFile);

            owner.Close(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to start download. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task AddNewCategoryAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                throw new InvalidOperationException("An error occurred while trying to add new category.");

            var vm = new AddEditCategoryWindowViewModel(AppService);
            var window = new AddEditCategoryWindow { DataContext = vm };
            await window.ShowDialog(owner);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to add new category. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task AddNewQueueAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                throw new InvalidOperationException("An error occurred while trying to add new queue.");

            var vm = new AddEditQueueWindowViewModel(AppService, null);
            var window = new AddEditQueueWindow { DataContext = vm };
            await window.ShowDialog<bool?>(owner);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to add new queue. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task AddDownloadFileToDownloadQueueAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                throw new InvalidOperationException("An error occurred while trying to add file to queue.");

            if (SelectedDownloadQueue == null)
            {
                await DialogBoxManager.ShowInfoDialogAsync("Queue not selected", "Please select a queue for your file.", DialogButtons.Ok);
                return;
            }

            var downloadQueue = AppService
                .DownloadQueueService
                .DownloadQueues
                .FirstOrDefault(dq => dq.Id == SelectedDownloadQueue.Id);

            if (downloadQueue == null)
            {
                await DialogBoxManager.ShowInfoDialogAsync("Queue not found",
                    "Queue not found. Maybe it was deleted or changed. Please try again or contact support team.",
                    DialogButtons.Ok);

                return;
            }

            var downloadFile = await AddDownloadFileAsync();
            if (downloadFile == null)
                return;

            // Add download file to selected download queue
            await AppService
                .DownloadQueueService
                .AddDownloadFileToDownloadQueueAsync(downloadQueue, downloadFile);

            if (RememberMyChoice)
            {
                await AppService
                    .DownloadQueueService
                    .ChangeDefaultDownloadQueueAsync(downloadQueue, reloadData: false);
            }

            await AppService
                .DownloadQueueService
                .ChangeLastSelectedDownloadQueueAsync(downloadQueue);

            if (StartDownloadQueue)
            {
                _ = AppService
                    .DownloadQueueService
                    .StartDownloadQueueAsync(downloadQueue);
            }

            owner.Close(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to add file to queue. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task AddDownloadFileToDefaultDownloadQueueAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                throw new InvalidOperationException("An error occurred while trying to add file to default queue.");

            var defaultDownloadQueue = AppService
                .DownloadQueueService
                .DownloadQueues
                .FirstOrDefault(dq => dq.IsDefault);

            DefaultDownloadQueueIsExist = defaultDownloadQueue != null;
            if (!DefaultDownloadQueueIsExist)
                return;

            var downloadFile = await AddDownloadFileAsync();
            if (downloadFile == null)
                return;

            await AppService
                .DownloadQueueService
                .AddDownloadFileToDownloadQueueAsync(defaultDownloadQueue, downloadFile);

            owner.Close(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to add file to default queue. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task<DownloadFileViewModel?> AddDownloadFileAsync()
    {
        // Make sure url details is not null and url has value
        if (_urlDetails == null || !_urlDetails.Url.Equals(DownloadFile.Url))
            return null;

        // Validate url details
        var validateUrlDetailsResult = AppService.DownloadFileService.ValidateUrlDetails(_urlDetails);
        if (!validateUrlDetailsResult.IsValid)
            return null;

        // Validate download file
        var validateDownloadFile = await AppService.DownloadFileService.ValidateDownloadFileAsync(DownloadFile);
        if (!validateDownloadFile)
            return null;

        // Check for duplicate download file
        DuplicateDownloadLinkAction? duplicateAction = null;
        if (_urlDetails.IsUrlDuplicate)
        {
            var savedDuplicateAction = AppService.SettingsService.Settings.DuplicateDownloadLinkAction;
            if (savedDuplicateAction == DuplicateDownloadLinkAction.LetUserChoose)
            {
                duplicateAction = await AppService
                    .DownloadFileService
                    .GetUserDuplicateActionAsync(_urlDetails.Url, _urlDetails.FileName, _urlDetails.Category!.CategorySaveDirectory!.SaveDirectory);
            }
            else
            {
                duplicateAction = savedDuplicateAction;
            }
        }

        // Add new download file
        var downloadFile = await AppService
            .DownloadFileService
            .AddDownloadFileAsync(DownloadFile,
                isUrlDuplicate: _urlDetails.IsUrlDuplicate,
                duplicateAction: duplicateAction,
                isFileNameDuplicate: _urlDetails.IsFileNameDuplicate);

        // Make sure download file is created
        if (downloadFile != null)
            return downloadFile;

        // Otherwise, show error message
        await DialogBoxManager.ShowInfoDialogAsync("File not found",
            "An error occurred while trying to add file. Please try again or contact support.",
            DialogButtons.Ok);

        return null;
    }

    private void LoadDownloadQueues()
    {
        var downloadQueues = AppService
            .DownloadQueueService
            .DownloadQueues;

        DownloadQueues.UpdateCollection(downloadQueues, dq => dq.Id);
        SelectedDownloadQueue = GetSelectedDownloadQueue();
    }

    private DownloadQueueViewModel? GetSelectedDownloadQueue()
    {
        var defaultQueue = DownloadQueues.FirstOrDefault(dq => dq.IsDefault);
        var lastChoice = DownloadQueues.FirstOrDefault(dq => dq.IsLastChoice);
        return defaultQueue ?? lastChoice ?? DownloadQueues.FirstOrDefault();
    }

    private void LoadCategories()
    {
        // Check if categories are disabled or not
        CategoriesAreDisabled = AppService.SettingsService.Settings.DisableCategories;

        // Store selected category id
        var selectedCategoryId = SelectedCategory?.Id;
        // Get all categories
        var categories = AppService
            .CategoryService
            .Categories;

        // Update collection
        Categories.UpdateCollection(categories, c => c.Id);
        // Re select previous category
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == selectedCategoryId) ?? Categories.FirstOrDefault();
    }

    public async Task GetUrlDetailsAsync()
    {
        try
        {
            IsLoadingUrl = true;

            // Get url details
            _urlDetails = await AppService.DownloadFileService.GetUrlDetailsAsync(DownloadFile.Url, _cancellationTokenSource.Token);

            DownloadFile.Url = _urlDetails.Url;
            DownloadFile.FileName = _urlDetails.FileName;
            DownloadFile.Size = _urlDetails.FileSize;
            DownloadFile.IsSizeUnknown = _urlDetails.IsFileSizeUnknown;
            DownloadFile.CategoryId = _urlDetails.Category?.Id;

            SelectedCategory = Categories.FirstOrDefault(c => c.Id == _urlDetails.Category?.Id);

            // Notify IsFileSizeVisible changed
            this.RaisePropertyChanged(nameof(IsFileSizeVisible));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to get url details. Error message: {ErrorMessage}", ex.Message);
            SelectedCategory = null;
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

    protected override void OnCategoryServiceCategoriesChanged()
    {
        base.OnCategoryServiceCategoriesChanged();
        LoadCategories();
    }

    protected override void OnSettingsServiceDataChanged()
    {
        base.OnSettingsServiceDataChanged();

        // Check if categories are disabled or not
        CategoriesAreDisabled = AppService.SettingsService.Settings.DisableCategories;
    }
}