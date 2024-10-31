using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.AppService;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    #region Private Fields

    private readonly DispatcherTimer _updateSpeedTimer;

    #endregion

    #region Properties

    // Categories list data
    private ObservableCollection<CategoryHeader> _categories = [];

    public ObservableCollection<CategoryHeader> Categories
    {
        get => _categories;
        set => this.RaiseAndSetIfChanged(ref _categories, value);
    }

    // Selected category
    private CategoryHeader? _selectedCategoryHeader = null;

    public CategoryHeader? SelectedCategoryHeader
    {
        get => _selectedCategoryHeader;
        set
        {
            if (_selectedCategoryHeader != null && value != _selectedCategoryHeader)
                SelectedCategory = null;

            this.RaiseAndSetIfChanged(ref _selectedCategoryHeader, value);
            FilterDownloadList();
        }
    }

    // Selected category item
    private Category? _selectedCategory = null;

    public Category? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCategory, value);
            FilterDownloadList();
        }
    }

    // Download list items
    private ObservableCollection<DownloadFileViewModel> _downloadFiles = [];

    public ObservableCollection<DownloadFileViewModel> DownloadFiles
    {
        get => _downloadFiles;
        set => this.RaiseAndSetIfChanged(ref _downloadFiles, value);
    }

    private bool _selectAllDownloadFiles = false;

    public bool SelectAllDownloadFiles
    {
        get => _selectAllDownloadFiles;
        set => this.RaiseAndSetIfChanged(ref _selectAllDownloadFiles, value);
    }

    private string? _totalSpeed;

    public string? TotalSpeed
    {
        get => _totalSpeed;
        set => this.RaiseAndSetIfChanged(ref _totalSpeed, value);
    }

    private string? _selectedFilesTotalSize;

    public string? SelectedFilesTotalSize
    {
        get => _selectedFilesTotalSize;
        set => this.RaiseAndSetIfChanged(ref _selectedFilesTotalSize, value);
    }

    private string? _searchText;

    public string? SearchText
    {
        get => _searchText;
        set
        {
            this.RaiseAndSetIfChanged(ref _searchText, value);
            FilterDownloadList();
        }
    }

    private ObservableCollection<DownloadQueueViewModel> _downloadQueues = [];

    public ObservableCollection<DownloadQueueViewModel> DownloadQueues
    {
        get => _downloadQueues;
        set => this.RaiseAndSetIfChanged(ref _downloadQueues, value);
    }

    #endregion

    #region Commands

    public ICommand? SelectAllRowsCommand { get; }

    public ICommand? AddNewLinkCommand { get; }

    public ICommand? ResumeDownloadFileCommand { get; }

    public ICommand? StopDownloadFileCommand { get; }

    public ICommand? StopAllDownloadFilesCommand { get; }

    public ICommand? DeleteDownloadFilesCommand { get; }

    public ICommand? DeleteCompletedDownloadFilesCommand { get; }

    public ICommand? OpenSettingsWindowCommand { get; }

    public ICommand? StartStopDownloadQueueCommand { get; }

    public ICommand? ShowDownloadQueueDetailsCommand { get; }

    public ICommand? AddNewDownloadQueueCommand { get; }

    #endregion

    public MainWindowViewModel(IAppService appService) : base(appService)
    {
        _updateSpeedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _updateSpeedTimer.Tick += UpdateSpeedTimerOnTick;
        _updateSpeedTimer.Start();

        LoadCategoriesAsync().GetAwaiter();
        SelectedCategoryHeader = Categories.FirstOrDefault();
        FilterDownloadList();
        LoadDownloadQueues();

        TotalSpeed = "0 KB";
        SelectedFilesTotalSize = "0 KB";

        SelectAllRowsCommand = ReactiveCommand.Create<DataGrid?>(SelectAllRows);
        AddNewLinkCommand = ReactiveCommand.Create<Window?>(AddNewLink);
        ResumeDownloadFileCommand = ReactiveCommand.Create<DataGrid?>(ResumeDownloadFile);
        StopDownloadFileCommand = ReactiveCommand.Create<DataGrid?>(StopDownloadFile);
        StopAllDownloadFilesCommand = ReactiveCommand.Create(StopAllDownloadFiles);
        DeleteDownloadFilesCommand = ReactiveCommand.Create<DataGrid?>(DeleteDownloadFiles);
        DeleteCompletedDownloadFilesCommand = ReactiveCommand.Create(DeleteCompletedDownloadFiles);
        OpenSettingsWindowCommand = ReactiveCommand.Create<Window?>(OpenSettingsWindow);
        StartStopDownloadQueueCommand =
            ReactiveCommand.CreateFromTask<DownloadQueueViewModel?>(StartStopDownloadQueueAsync);
        ShowDownloadQueueDetailsCommand = ReactiveCommand.Create<Button?>(ShowDownloadQueueDetails);
        AddNewDownloadQueueCommand = ReactiveCommand.Create<Window?>(AddNewDownloadQueue);
    }

    private void LoadDownloadQueues()
    {
        DownloadQueues = AppService
            .DownloadQueueService
            .DownloadQueues;
    }

    private void SelectAllRows(DataGrid? dataGrid)
    {
        if (dataGrid == null)
            return;

        if (DownloadFiles.Count == 0)
        {
            SelectAllDownloadFiles = false;
            dataGrid.SelectedIndex = -1;
            return;
        }

        if (!SelectAllDownloadFiles)
            dataGrid.SelectedIndex = -1;
        else
            dataGrid.SelectAll();
    }

    private async void AddNewLink(Window? owner)
    {
        // TODO: Show message box
        try
        {
            if (owner == null)
                return;

            var url = string.Empty;
            if (owner.Clipboard != null)
                url = await owner.Clipboard.GetTextAsync();

            var urlIsValid = url.CheckUrlValidation();
            var vm = new AddDownloadLinkWindowViewModel(AppService)
            {
                Url = urlIsValid ? url : null,
                IsLoadingUrl = urlIsValid
            };

            var window = new AddDownloadLinkWindow { DataContext = vm };
            var result = await window.ShowDialog<bool>(owner);
            if (!result)
                return;

            // TODO: Why I'm comment this code???
            // await LoadCategoriesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void ResumeDownloadFile(DataGrid? dataGrid)
    {
        // TODO: Show message box
        try
        {
            if (dataGrid == null || dataGrid.SelectedItems.Count == 0)
                return;

            var downloadFiles = dataGrid
                .SelectedItems
                .OfType<DownloadFileViewModel>()
                .Where(df => df is { IsDownloading: false, IsCompleted: false })
                .ToList();

            foreach (var downloadFile in downloadFiles)
            {
                if (downloadFile.IsPaused)
                {
                    AppService
                        .DownloadFileService
                        .ResumeDownloadFile(downloadFile);
                }
                else
                {
                    _ = AppService
                        .DownloadFileService
                        .StartDownloadFileAsync(downloadFile);
                }

                var vm = new DownloadWindowViewModel(AppService, downloadFile);
                var window = new DownloadWindow { DataContext = vm };
                window.Show();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void StopDownloadFile(DataGrid? dataGrid)
    {
        // TODO: Show message box
        try
        {
            if (dataGrid == null || dataGrid.SelectedItems.Count == 0)
                return;

            var downloadFiles = dataGrid
                .SelectedItems
                .OfType<DownloadFileViewModel>()
                .Where(df => df.IsDownloading)
                .ToList();

            foreach (var downloadFile in downloadFiles)
            {
                _ = AppService
                    .DownloadFileService
                    .StopDownloadFileAsync(downloadFile);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async void StopAllDownloadFiles()
    {
        // TODO: Show message box
        try
        {
            var runningDownloadQueues = AppService
                .DownloadQueueService
                .DownloadQueues
                .Where(dq => dq.IsRunning)
                .ToList();

            foreach (var downloadQueue in runningDownloadQueues)
            {
                await AppService
                    .DownloadQueueService
                    .StopDownloadQueueAsync(downloadQueue);
            }

            var downloadFiles = AppService
                .DownloadFileService
                .DownloadFiles
                .Where(df => df.IsDownloading)
                .ToList();

            foreach (var downloadFile in downloadFiles)
            {
                await AppService
                    .DownloadFileService
                    .StopDownloadFileAsync(downloadFile);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async void DeleteDownloadFiles(DataGrid? dataGrid)
    {
        // TODO: Show message box
        try
        {
            if (dataGrid == null || dataGrid.SelectedItems.Count == 0)
                return;

            var downloadFiles = dataGrid
                .SelectedItems
                .OfType<DownloadFileViewModel>()
                .ToList();

            for (var i = downloadFiles.Count - 1; i >= 0; i--)
            {
                if (downloadFiles[i].IsDownloading)
                {
                    await AppService
                        .DownloadFileService
                        .StopDownloadFileAsync(downloadFiles[i]);
                }

                await AppService
                    .DownloadFileService
                    .DeleteDownloadFileAsync(downloadFiles[i], true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async void DeleteCompletedDownloadFiles()
    {
        // TODO: Show message box and ask user for deleting file
        try
        {
            var downloadFiles = AppService
                .DownloadFileService
                .DownloadFiles
                .Where(df => df.IsCompleted)
                .ToList();

            foreach (var downloadFile in downloadFiles)
            {
                await AppService
                    .DownloadFileService
                    .DeleteDownloadFileAsync(downloadFile, false);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async void OpenSettingsWindow(Window? owner)
    {
        // TODO: Show message box
        try
        {
            if (owner == null)
                return;

            var vm = new SettingsWindowViewModel(AppService);
            var window = new SettingsWindow { DataContext = vm };
            await window.ShowDialog(owner);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async Task StartStopDownloadQueueAsync(DownloadQueueViewModel? downloadQueue)
    {
        // TODO: Show message box
        try
        {
            if (downloadQueue == null)
                return;

            if (!downloadQueue.IsRunning)
            {
                await AppService
                    .DownloadQueueService
                    .StartDownloadQueueAsync(downloadQueue);
            }
            else
            {
                await AppService
                    .DownloadQueueService
                    .StopDownloadQueueAsync(downloadQueue);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async void ShowDownloadQueueDetails(Button? button)
    {
        // TODO: Show message box
        try
        {
            var owner = button?.FindLogicalAncestorOfType<Window>();
            if (owner == null)
                return;

            var tag = button?.Tag?.ToString();
            if (tag.IsNullOrEmpty() || !int.TryParse(tag, out var downloadQueueId))
                return;

            var downloadQueue = AppService
                .DownloadQueueService
                .DownloadQueues
                .FirstOrDefault(dq => dq.Id == downloadQueueId);

            if (downloadQueue == null)
                return;

            var vm = new AddEditQueueWindowViewModel(AppService) { IsEditMode = true, DownloadQueue = downloadQueue };
            var window = new AddEditQueueWindow { DataContext = vm };
            await window.ShowDialog(owner);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async void AddNewDownloadQueue(Window? owner)
    {
        // TODO: Show message box
        try
        {
            if (owner == null)
                return;

            var vm = new AddEditQueueWindowViewModel(AppService) { IsEditMode = false };
            var window = new AddEditQueueWindow { DataContext = vm };
            await window.ShowDialog(owner);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void UpdateSpeedTimerOnTick(object? sender, EventArgs e)
    {
        // TODO: Show message box
        try
        {
            var totalSpeed = AppService
                .DownloadFileService
                .DownloadFiles
                .Where(df => df.IsDownloading)
                .Sum(df => df.TransferRate ?? 0);

            TotalSpeed = totalSpeed == 0 ? "0 KB" : totalSpeed.ToFileSize();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var categoryHeaders = await AppService
                .UnitOfWork
                .CategoryHeaderRepository
                .GetAllAsync();

            var categories = await AppService
                .UnitOfWork
                .CategoryRepository
                .GetAllAsync();

            categoryHeaders = categoryHeaders
                .Select(c =>
                {
                    c.Categories = categories;
                    return c;
                })
                .ToList();

            Categories = categoryHeaders.ToObservableCollection();
        }
        catch
        {
            Categories = [];
        }
    }

    private void FilterDownloadList()
    {
        // TODO: Show message box
        try
        {
            var downloadFiles = AppService
                .DownloadFileService
                .DownloadFiles
                .ToList();

            if (SelectedCategoryHeader != null)
            {
                switch (SelectedCategoryHeader.Title)
                {
                    case Constants.UnfinishedCategoryHeaderTitle:
                    {
                        downloadFiles = downloadFiles
                            .Where(df => df.Status != DownloadFileStatus.Completed)
                            .ToList();

                        break;
                    }

                    case Constants.FinishedCategoryHeaderTitle:
                    {
                        downloadFiles = downloadFiles
                            .Where(df => df.Status == DownloadFileStatus.Completed)
                            .ToList();

                        break;
                    }
                }
            }

            if (SelectedCategory != null)
            {
                downloadFiles = downloadFiles
                    .Where(df => df.CategoryId == SelectedCategory.Id)
                    .ToList();
            }

            if (!SearchText.IsNullOrEmpty())
            {
                downloadFiles = downloadFiles
                    .Where(df => (df.Url?.Contains(SearchText!, StringComparison.OrdinalIgnoreCase) ?? true)
                                 || (df.FileName?.Contains(SearchText!, StringComparison.OrdinalIgnoreCase) ?? true)
                                 || (df.SaveLocation?.Contains(SearchText!, StringComparison.OrdinalIgnoreCase) ??
                                     true))
                    .ToList();
            }

            DownloadFiles = downloadFiles.ToObservableCollection();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    protected override void OnDownloadFileServiceDataChanged()
    {
        FilterDownloadList();
    }

    protected override void OnDownloadQueueServiceDataChanged()
    {
        LoadDownloadQueues();
    }
}