using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
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

    private DownloadFileViewModel _downloadFile = new();
    private ObservableCollection<CategoryViewModel> _categories = [];
    private CategoryViewModel? _selectedCategory;
    private ObservableCollection<DownloadQueueViewModel> _downloadQueues = [];
    private DownloadQueueViewModel? _selectedDownloadQueue;
    private bool _isLoadingUrl;
    private bool _rememberMyChoice;
    private bool _startDownloadQueue;
    private bool _defaultDownloadQueueIsExist;

    #endregion

    #region Properties

    public DownloadFileViewModel DownloadFile
    {
        get => _downloadFile;
        set => this.RaiseAndSetIfChanged(ref _downloadFile, value);
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
        LoadCategoriesAsync().GetAwaiter();
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

            var isValidResult = await AppService.DownloadFileService.ValidateDownloadFileAsync(DownloadFile);
            if (!isValidResult.IsSuccess)
            {
                await ShowInfoDialogAsync(isValidResult.Header!, isValidResult.Message!, DialogButtons.Ok);
                return;
            }

            var result = await AppService.DownloadFileService.AddDownloadFileAsync(DownloadFile);
            if (!result.IsSuccess)
            {
                await ShowDangerDialogAsync("Add file", "An error occured while trying to add file.", DialogButtons.Ok);
                return;
            }

            var downloadFile = result.Result;
            if (downloadFile == null)
            {
                await ShowInfoDialogAsync("File not found",
                    "Inserted file not found. Maybe it was deleted or not inserted correctly. Please try again or contact support team.",
                    DialogButtons.Ok);

                return;
            }

            // Start download before DownloadWindow opened
            _ = AppService.DownloadFileService.StartDownloadFileAsync(downloadFile);

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

            var isValidResult = await AppService
                .DownloadFileService
                .ValidateDownloadFileAsync(DownloadFile);

            if (!isValidResult.IsSuccess)
            {
                await ShowInfoDialogAsync(isValidResult.Header!, isValidResult.Message!, DialogButtons.Ok);
                return;
            }

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

            var result = await AppService
                .DownloadFileService
                .AddDownloadFileAsync(DownloadFile);

            if (!result.IsSuccess)
            {
                await ShowDangerDialogAsync("Add file", "An error occured while trying to add file.", DialogButtons.Ok);
                return;
            }

            // Add download file to selected download queue
            await AppService
                .DownloadQueueService
                .AddDownloadFileToDownloadQueueAsync(downloadQueue, result.Result);

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
            await ShowErrorDialogAsync(ex);
        }
    }

    private async Task AddDownloadFileToDefaultDownloadQueueAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            var isValidResult = await AppService
                .DownloadFileService
                .ValidateDownloadFileAsync(DownloadFile);

            if (!isValidResult.IsSuccess)
            {
                await ShowInfoDialogAsync(isValidResult.Header!, isValidResult.Message!, DialogButtons.Ok);
                return;
            }

            var defaultDownloadQueue = AppService
                .DownloadQueueService
                .DownloadQueues
                .FirstOrDefault(dq => dq.IsDefault);

            DefaultDownloadQueueIsExist = defaultDownloadQueue != null;
            if (!DefaultDownloadQueueIsExist)
                return;

            var result = await AppService
                .DownloadFileService
                .AddDownloadFileAsync(DownloadFile);

            if (!result.IsSuccess)
            {
                await ShowDangerDialogAsync("Add file", "An error occured while trying to add file.", DialogButtons.Ok);
                return;
            }

            await AppService
                .DownloadQueueService
                .AddDownloadFileToDownloadQueueAsync(defaultDownloadQueue, DownloadFile);

            owner.Close(true);
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(ex);
        }
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

    public async Task GetUrlDetailsAsync()
    {
        try
        {
            IsLoadingUrl = true;
            var result = await AppService.DownloadFileService.GetUrlDetailsAsync(DownloadFile.Url);
            if (!result.IsSuccess)
            {
                switch (result.Result)
                {
                    case { IsFile: false }:
                    {
                        await ShowInfoDialogAsync("File not found", 
                            "The link you entered does not return a downloadable file. This could be due to an incorrect link, restricted access, or unsupported content.",
                            DialogButtons.Ok);
                        
                        break;
                    }
                }

                return;
            }

            if (result.Result!.IsUrlDuplicate)
            {
                // TODO: Based on user settings handle duplicate urls
            }

            DownloadFile.Url = result.Result.Url;
            DownloadFile.FileName = result.Result.FileName;
            DownloadFile.Size = result.Result.FileSize;
            DownloadFile.CategoryId = result.Result.Category?.Id;

            SelectedCategory = Categories.FirstOrDefault(c => c.Id == result.Result.Category?.Id);
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