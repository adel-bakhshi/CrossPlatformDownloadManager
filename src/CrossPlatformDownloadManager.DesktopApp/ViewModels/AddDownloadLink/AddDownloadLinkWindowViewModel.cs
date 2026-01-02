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
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService.Models;
using CrossPlatformDownloadManager.DesktopApp.ViewModels.AddEditQueue;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.DesktopApp.Views.AddEditQueue;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.AddDownloadLink;

/// <summary>
/// ViewModel for the Add Download Link Window, handling the logic for adding new download links
/// </summary>
public class AddDownloadLinkWindowViewModel : ViewModelBase
{
    #region Private Fields

    ///<summary>
    /// The cancellation token source for the download link validation task.
    /// </summary>
    private readonly CancellationTokenSource _cancellationTokenSource;

    /// <summary>
    /// The debouncer for handling the loading state of the URL validation.
    /// </summary>
    private readonly Debouncer _changeLoadingStateDebouncer;

    // Private backing fields for properties
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

    /// <summary>
    /// Gets or sets a value that indicates the download file being added.
    /// </summary>
    public DownloadFileViewModel DownloadFile
    {
        get => _downloadFile;
        set
        {
            this.RaiseAndSetIfChanged(ref _downloadFile, value);
            this.RaisePropertyChanged(nameof(IsFileSizeVisible));
        }
    }

    /// <summary>
    /// Gets or sets a value that indicates the collection of available categories.
    /// </summary>
    public ObservableCollection<CategoryViewModel> Categories
    {
        get => _categories;
        set => this.RaiseAndSetIfChanged(ref _categories, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the currently selected category.
    /// </summary>
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

    /// <summary>
    /// Gets or sets a value that indicates the collection of available download queues.
    /// </summary>
    public ObservableCollection<DownloadQueueViewModel> DownloadQueues
    {
        get => _downloadQueues;
        set => this.RaiseAndSetIfChanged(ref _downloadQueues, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the currently selected download queue.
    /// </summary>
    public DownloadQueueViewModel? SelectedDownloadQueue
    {
        get => _selectedDownloadQueue;
        set => this.RaiseAndSetIfChanged(ref _selectedDownloadQueue, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the flag indicating if URL details are being loaded.
    /// </summary>
    public bool IsLoadingUrl
    {
        get => _isLoadingUrl;
        set => this.RaiseAndSetIfChanged(ref _isLoadingUrl, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the flag indicating if user choice should be remembered.
    /// </summary>
    public bool RememberMyChoice
    {
        get => _rememberMyChoice;
        set => this.RaiseAndSetIfChanged(ref _rememberMyChoice, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the flag indicating if download queue should be started.
    /// </summary>
    public bool StartDownloadQueue
    {
        get => _startDownloadQueue;
        set => this.RaiseAndSetIfChanged(ref _startDownloadQueue, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the flag indicating if default download queue exists.
    /// </summary>
    public bool DefaultDownloadQueueIsExist
    {
        get => _defaultDownloadQueueIsExist;
        set => this.RaiseAndSetIfChanged(ref _defaultDownloadQueueIsExist, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the flag indicating if categories are disabled.
    /// </summary>
    public bool CategoriesAreDisabled
    {
        get => _categoriesAreDisabled;
        set => this.RaiseAndSetIfChanged(ref _categoriesAreDisabled, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the flag indicating if file size should be visible.
    /// </summary>
    public bool IsFileSizeVisible => DownloadFile.IsSizeUnknown || DownloadFile.Size > 0;

    #endregion

    #region Commands

    /// <summary>
    /// Gets or sets the command to add a new category.
    /// </summary>
    public ICommand AddNewCategoryCommand { get; }

    /// <summary>
    /// Gets or sets the command to add a new download queue.
    /// </summary>
    public ICommand AddNewQueueCommand { get; }

    /// <summary>
    /// Gets or sets the command to add a download file to a download queue.
    /// </summary>
    public ICommand AddDownloadFileToDownloadQueueCommand { get; }

    /// <summary>
    /// Gets or sets the command to add a download file to the default download queue.
    /// </summary>
    public ICommand AddDownloadFileToDefaultDownloadQueueCommand { get; }

    /// <summary>
    /// Gets or sets the command to start the download.
    /// </summary>
    public ICommand StartDownloadCommand { get; }

    /// <summary>
    /// Gets or sets the command to cancel the operation.
    /// </summary>
    public ICommand CancelCommand { get; }

    #endregion

    /// <summary>
    /// Constructor for the ViewModel.
    /// </summary>
    /// <param name="appService">Application service instance.</param>
    public AddDownloadLinkWindowViewModel(IAppService appService) : base(appService)
    {
        // Initialize private fields
        _cancellationTokenSource = new CancellationTokenSource();
        _changeLoadingStateDebouncer = new Debouncer(TimeSpan.FromSeconds(1.2));

        LoadCategories();
        LoadDownloadQueues();

        // Initialize commands
        AddNewCategoryCommand = ReactiveCommand.CreateFromTask<Window?>(AddNewCategoryAsync);
        AddNewQueueCommand = ReactiveCommand.CreateFromTask<Window?>(AddNewQueueAsync);
        AddDownloadFileToDownloadQueueCommand = ReactiveCommand.CreateFromTask<Window?>(AddDownloadFileToDownloadQueueAsync);
        AddDownloadFileToDefaultDownloadQueueCommand = ReactiveCommand.CreateFromTask<Window?>(AddDownloadFileToDefaultDownloadQueueAsync);
        StartDownloadCommand = ReactiveCommand.CreateFromTask<Window?>(StartDownloadAsync);
        CancelCommand = ReactiveCommand.CreateFromTask<Window?>(CancelAsync);
    }

    /// <summary>
    /// Asynchronously retrieves details from the provided URL.
    /// </summary>
    public async Task GetUrlDetailsAsync()
    {
        try
        {
            Log.Debug("Getting url details from {Class}...", nameof(AddDownloadLinkWindowViewModel));

            // Set the loading flag to true
            IsLoadingUrl = true;
            // Create download file options
            var options = new DownloadFileOptions
            {
                Referer = DownloadFile.Referer,
                PageAddress = DownloadFile.PageAddress,
                Description = DownloadFile.Description,
                Username = DownloadFile.Username,
                Password = DownloadFile.Password
            };

            // Get the download file from URL
            DownloadFile = await AppService.DownloadFileService.GetDownloadFileFromUrlAsync(DownloadFile.Url, options, _cancellationTokenSource.Token);
            // Change the selected category based on the category of the download file
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == DownloadFile.CategoryId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to get url details. Error message: {ErrorMessage}", ex.Message);
            SelectedCategory = null;
        }
        finally
        {
            _changeLoadingStateDebouncer.Run(() => IsLoadingUrl = false);
        }
    }

    /// <summary>
    /// Handles cancellation of the operation.
    /// </summary>
    /// <param name="owner">The owner window.</param>
    private async Task CancelAsync(Window? owner)
    {
        try
        {
            Log.Debug("Trying to cancel the operation...");

            // Cancel the operation
            await _cancellationTokenSource.CancelAsync();
            owner?.Close();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to close the window. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    /// <summary>
    /// Starts the download process.
    /// </summary>
    /// <param name="owner">The owner window.</param>
    private async Task StartDownloadAsync(Window? owner)
    {
        try
        {
            // Check if the owner window is null and if it is, throw an error.
            if (owner == null)
                throw new InvalidOperationException("An error occurred while trying to start download.");

            Log.Information("Starting download for {Url}...", DownloadFile.Url);

            // Add download file to database
            var downloadFile = await AppService.DownloadFileService.AddDownloadFileAsync(DownloadFile);
            if (downloadFile == null)
                return;

            // Start download and open download window
            _ = AppService.DownloadFileService.StartDownloadFileAsync(downloadFile);
            // Close the window
            owner.Close(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to start download. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    /// <summary>
    /// Opens dialog to add a new category.
    /// </summary>
    /// <param name="owner">The owner window.</param>
    private async Task AddNewCategoryAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                throw new InvalidOperationException("An error occurred while trying to add new category.");

            Log.Debug("Trying to open add new category window...");

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

    /// <summary>
    /// Opens dialog to add a new queue.
    /// </summary>
    /// <param name="owner">The owner window.</param>
    private async Task AddNewQueueAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                throw new InvalidOperationException("An error occurred while trying to add new queue.");

            Log.Debug("Trying to open add new queue window...");

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

    /// <summary>
    /// Adds download file to the selected download queue.
    /// </summary>
    /// <param name="owner">The owner window.</param>
    private async Task AddDownloadFileToDownloadQueueAsync(Window? owner)
    {
        try
        {
            // Check if the owner window is null and if it is, throw an error.
            if (owner == null)
                throw new InvalidOperationException("An error occurred while trying to add file to queue.");

            // Check if the selected download queue is null and if it is, show an error message.
            if (SelectedDownloadQueue == null)
            {
                await DialogBoxManager.ShowInfoDialogAsync("Queue not selected", "Please select a queue for your file.", DialogButtons.Ok);
                return;
            }

            // Find the download queue
            var downloadQueue = AppService
                .DownloadQueueService
                .DownloadQueues
                .FirstOrDefault(dq => dq.Id == SelectedDownloadQueue.Id);

            // Make sure download queue exists
            if (downloadQueue == null)
            {
                Log.Debug("Download queue not found. Download queue ID: {DownloadQueueId}, Download queue title: {DownloadQueueTitle}",
                    SelectedDownloadQueue.Id, SelectedDownloadQueue.Title);

                await DialogBoxManager.ShowInfoDialogAsync("Queue not found",
                    "Queue not found. Maybe it was deleted or changed. Please try again or contact support team.",
                    DialogButtons.Ok);

                return;
            }

            Log.Debug("Trying to add download file to database...");

            // Add download file to database
            var downloadFile = await AppService.DownloadFileService.AddDownloadFileAsync(DownloadFile);
            if (downloadFile == null)
                return;

            Log.Debug("Adding download file with ID {DownloadFileId} to queue with ID {DownloadQueueId}...", downloadFile.Id, downloadQueue.Id);

            // Add download file to selected download queue
            await AppService
                .DownloadQueueService
                .AddDownloadFileToDownloadQueueAsync(downloadQueue, downloadFile);

            // Check if the user wants to remember the selected download queue
            // Change the default download queue
            if (RememberMyChoice)
            {
                Log.Debug("Saving the selected download queue as the default download queue...");

                await AppService
                    .DownloadQueueService
                    .ChangeDefaultDownloadQueueAsync(downloadQueue, reloadData: false);
            }

            Log.Debug("Saving the selected download queue as the last selected download queue...");

            // Change the last selected download queue
            await AppService
                .DownloadQueueService
                .ChangeLastSelectedDownloadQueueAsync(downloadQueue);

            // Check if the user wants to start the download queue
            if (StartDownloadQueue)
            {
                Log.Debug("Starting the download queue...");

                // Start downloading the queue
                _ = AppService
                    .DownloadQueueService
                    .StartDownloadQueueAsync(downloadQueue);
            }

            // Close the window
            owner.Close(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to add file to queue. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    /// <summary>
    /// Adds download file to the default download queue.
    /// </summary>
    /// <param name="owner">The owner window.</param>
    private async Task AddDownloadFileToDefaultDownloadQueueAsync(Window? owner)
    {
        try
        {
            // Check if the owner window is null and if it is, throw an error.
            if (owner == null)
                throw new InvalidOperationException("An error occurred while trying to add file to default queue.");

            Log.Debug("Getting the default download queue...");

            // Find the default download queue
            var defaultDownloadQueue = AppService
                .DownloadQueueService
                .DownloadQueues
                .FirstOrDefault(dq => dq.IsDefault);

            // Check if default download queue is exists
            DefaultDownloadQueueIsExist = defaultDownloadQueue != null;
            if (!DefaultDownloadQueueIsExist)
            {
                Log.Debug("The default download queue does not exist. Returning...");
                return;
            }

            Log.Debug("Trying to add download file to database...");

            // Add download file to database
            var downloadFile = await AppService.DownloadFileService.AddDownloadFileAsync(DownloadFile);
            if (downloadFile == null)
                return;

            Log.Debug("Adding download file with ID {DownloadFileId} to queue with ID {DownloadQueueId}...", downloadFile.Id, defaultDownloadQueue!.Id);

            // Add download file to default download queue
            await AppService
                .DownloadQueueService
                .AddDownloadFileToDownloadQueueAsync(defaultDownloadQueue, downloadFile);

            // Close the window
            owner.Close(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to add file to default queue. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    /// <summary>
    /// Loads the download queues.
    /// </summary>
    private void LoadDownloadQueues()
    {
        Log.Debug("Loading download queues...");

        DownloadQueues = AppService.DownloadQueueService.DownloadQueues;
        SelectedDownloadQueue = GetSelectedDownloadQueue();
    }

    /// <summary>
    /// Gets the selected download queue.
    /// </summary>
    private DownloadQueueViewModel? GetSelectedDownloadQueue()
    {
        Log.Debug("Getting the selected download queue...");

        var defaultQueue = DownloadQueues.FirstOrDefault(dq => dq.IsDefault);
        var lastChoice = DownloadQueues.FirstOrDefault(dq => dq.IsLastChoice);
        return defaultQueue ?? lastChoice ?? DownloadQueues.FirstOrDefault();
    }

    /// <summary>
    /// Loads the categories.
    /// </summary>
    private void LoadCategories()
    {
        Log.Debug("Loading the categories...");

        // Check if categories are disabled or not
        CategoriesAreDisabled = AppService.SettingsService.Settings.DisableCategories;

        // Store selected category id
        var selectedCategoryId = SelectedCategory?.Id;
        // Get all categories
        Categories = AppService.CategoryService.Categories;
        // Re select previous category
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == selectedCategoryId) ?? Categories.FirstOrDefault();
    }

    /// <summary>
    /// Handles changes in download queue service data.
    /// </summary>
    protected override void OnDownloadQueueServiceDataChanged()
    {
        base.OnDownloadQueueServiceDataChanged();
        LoadDownloadQueues();
    }

    /// <summary>
    /// Handles changes in category service data.
    /// </summary>
    protected override void OnCategoryServiceCategoriesChanged()
    {
        base.OnCategoryServiceCategoriesChanged();
        LoadCategories();
    }

    /// <summary>
    /// Handles changes in settings service data.
    /// </summary>
    protected override void OnSettingsServiceDataChanged()
    {
        base.OnSettingsServiceDataChanged();

        // Check if categories are disabled or not
        CategoriesAreDisabled = AppService.SettingsService.Settings.DisableCategories;
        Log.Debug("Categories are disabled: {CategoriesAreDisabled}", CategoriesAreDisabled ? "Yes" : "No");
    }
}