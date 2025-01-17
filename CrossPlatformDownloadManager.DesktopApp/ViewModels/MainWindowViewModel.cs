using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.Exports;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    #region Private Fields

    private readonly DispatcherTimer _updateDownloadSpeedTimer;
    private readonly DispatcherTimer _updateActiveDownloadQueuesTimer;

    private MainWindow? _mainWindow;
    private List<DownloadFileViewModel> _selectedDownloadFilesToAddToQueue = [];

    // Properties
    private ObservableCollection<CategoryHeader> _categoryHeaders = [];
    private CategoryHeader? _selectedCategoryHeader;
    private Category? _selectedCategory;
    private ObservableCollection<DownloadFileViewModel> _downloadFiles = [];
    private bool _selectAllDownloadFiles;
    private string _downloadSpeed = "0 KB";
    private string? _selectedFilesTotalSize;
    private string? _searchText;
    private ObservableCollection<DownloadQueueViewModel> _downloadQueues = [];
    private ObservableCollection<DownloadQueueViewModel> _activeDownloadQueues = [];
    private ObservableCollection<DownloadQueueViewModel> _addToQueueDownloadQueues = [];
    private ContextFlyoutEnableStateViewMode _contextFlyoutEnableState = new();
    private MainMenuItemsEnabledState _mainMenuItemsEnabledState = new();

    #endregion

    #region Properties

    public ObservableCollection<CategoryHeader> CategoryHeaders
    {
        get => _categoryHeaders;
        set => this.RaiseAndSetIfChanged(ref _categoryHeaders, value);
    }

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

    public Category? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCategory, value);
            FilterDownloadList();
        }
    }

    public ObservableCollection<DownloadFileViewModel> DownloadFiles
    {
        get => _downloadFiles;
        set => this.RaiseAndSetIfChanged(ref _downloadFiles, value);
    }

    public bool SelectAllDownloadFiles
    {
        get => _selectAllDownloadFiles;
        set => this.RaiseAndSetIfChanged(ref _selectAllDownloadFiles, value);
    }

    public string DownloadSpeed
    {
        get => _downloadSpeed;
        set => this.RaiseAndSetIfChanged(ref _downloadSpeed, value);
    }

    public string? SelectedFilesTotalSize
    {
        get => _selectedFilesTotalSize;
        set => this.RaiseAndSetIfChanged(ref _selectedFilesTotalSize, value);
    }

    public string? SearchText
    {
        get => _searchText;
        set
        {
            this.RaiseAndSetIfChanged(ref _searchText, value);
            FilterDownloadList();
        }
    }

    public ObservableCollection<DownloadQueueViewModel> DownloadQueues
    {
        get => _downloadQueues;
        set => this.RaiseAndSetIfChanged(ref _downloadQueues, value);
    }

    public ObservableCollection<DownloadQueueViewModel> ActiveDownloadQueues
    {
        get => _activeDownloadQueues;
        set
        {
            this.RaiseAndSetIfChanged(ref _activeDownloadQueues, value);
            this.RaisePropertyChanged(nameof(ActiveDownloadQueueExists));
            this.RaisePropertyChanged(nameof(ActiveDownloadQueuesToolTipText));
        }
    }

    public bool ActiveDownloadQueueExists => ActiveDownloadQueues.Any();

    public string ActiveDownloadQueuesToolTipText => GetToolTipText();

    public ObservableCollection<DownloadQueueViewModel> AddToQueueDownloadQueues
    {
        get => _addToQueueDownloadQueues;
        set => this.RaiseAndSetIfChanged(ref _addToQueueDownloadQueues, value);
    }

    public ContextFlyoutEnableStateViewMode ContextFlyoutEnableState
    {
        get => _contextFlyoutEnableState;
        set => this.RaiseAndSetIfChanged(ref _contextFlyoutEnableState, value);
    }

    public Flyout? AddToQueueFlyout { get; set; }

    public MainMenuItemsEnabledState MainMenuItemsEnabledState
    {
        get => _mainMenuItemsEnabledState;
        set => this.RaiseAndSetIfChanged(ref _mainMenuItemsEnabledState, value);
    }

    #endregion

    #region Commands

    public ICommand SelectAllRowsCommand { get; }

    public ICommand AddNewLinkCommand { get; }

    public ICommand ResumeDownloadFileCommand { get; }

    public ICommand StopDownloadFileCommand { get; }

    public ICommand StopAllDownloadFilesCommand { get; }

    public ICommand DeleteDownloadFilesCommand { get; }

    public ICommand DeleteCompletedDownloadFilesCommand { get; }

    public ICommand OpenSettingsWindowCommand { get; }

    public ICommand StartStopDownloadQueueCommand { get; }

    public ICommand ShowDownloadQueueDetailsCommand { get; }

    public ICommand AddNewDownloadQueueCommand { get; }

    public ICommand SelectAllRowsContextMenuCommand { get; }

    public ICommand OpenFileContextMenuCommand { get; }

    public ICommand OpenFolderContextMenuCommand { get; }

    public ICommand RenameContextMenuCommand { get; }

    public ICommand ChangeFolderContextMenuCommand { get; }

    public ICommand RedownloadContextMenuCommand { get; }

    public ICommand ResumeContextMenuCommand { get; }

    public ICommand StopContextMenuCommand { get; }

    public ICommand RefreshDownloadAddressContextMenuCommand { get; }

    public ICommand RemoveContextMenuCommand { get; }

    public ICommand AddToQueueContextMenuCommand { get; }

    public ICommand RemoveFromQueueContextMenuCommand { get; }

    public ICommand AddDownloadFileToDownloadQueueContextMenuCommand { get; }

    public ICommand AddNewLinkMenuItemCommand { get; }

    public ICommand ExportDataMenuItemCommand { get; }

    public ICommand ImportDataMenuItemCommand { get; }

    public ICommand ExportSettingsMenuItemCommand { get; }

    public ICommand ImportSettingsMenuItemCommand { get; }

    public ICommand ExitProgramMenuItemCommand { get; }

    public ICommand StartAllDownloadsMenuItemCommand { get; }

    public ICommand StopAllDownloadsMenuItemCommand { get; }

    public ICommand PauseAllDownloadsMenuItemCommand { get; }
    
    public ICommand ResumeAllDownloadsMenuItemCommand { get; }

    public ICommand StartDownloadMenuItemCommand { get; }

    public ICommand StopDownloadMenuItemCommand { get; }

    public ICommand PauseDownloadMenuItemCommand { get; }
    
    public ICommand ResumeDownloadMenuItemCommand { get; }

    public ICommand RedownloadMenuItemCommand { get; }
    
    public ICommand DeleteDownloadMenuItemCommand { get; }
    
    public ICommand DeleteAllCompletedDownloadsMenuItemCommand { get; }
    
    public ICommand OpenSettingsMenuItemCommand { get; }

    #endregion

    public MainWindowViewModel(IAppService appService) : base(appService)
    {
        _updateDownloadSpeedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _updateDownloadSpeedTimer.Tick += UpdateDownloadSpeedTimerOnTick;
        _updateDownloadSpeedTimer.Start();

        _updateActiveDownloadQueuesTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _updateActiveDownloadQueuesTimer.Tick += UpdateActiveDownloadQueuesTimerOnTick;
        _updateActiveDownloadQueuesTimer.Start();

        LoadCategoriesAsync().GetAwaiter();
        FilterDownloadList();
        LoadDownloadQueues();

        DownloadSpeed = "0 KB";
        SelectedFilesTotalSize = "0 KB";

        SelectAllRowsCommand = ReactiveCommand.Create<DataGrid?>(SelectAllRows);
        AddNewLinkCommand = ReactiveCommand.CreateFromTask<Window?>(AddNewLinkAsync);
        ResumeDownloadFileCommand = ReactiveCommand.CreateFromTask<DataGrid?>(ResumeDownloadFileAsync);
        StopDownloadFileCommand = ReactiveCommand.CreateFromTask<DataGrid?>(StopDownloadFileAsync);
        StopAllDownloadFilesCommand = ReactiveCommand.CreateFromTask(StopAllDownloadFilesAsync);
        DeleteDownloadFilesCommand = ReactiveCommand.CreateFromTask<DataGrid?>(DeleteDownloadFilesAsync);
        DeleteCompletedDownloadFilesCommand = ReactiveCommand.CreateFromTask(DeleteCompletedDownloadFilesAsync);
        OpenSettingsWindowCommand = ReactiveCommand.CreateFromTask<Window?>(OpenSettingsWindowAsync);
        StartStopDownloadQueueCommand = ReactiveCommand.CreateFromTask<DownloadQueueViewModel?>(StartStopDownloadQueueAsync);
        ShowDownloadQueueDetailsCommand = ReactiveCommand.CreateFromTask<Button?>(ShowDownloadQueueDetailsAsync);
        AddNewDownloadQueueCommand = ReactiveCommand.CreateFromTask<Window?>(AddNewDownloadQueueAsync);
        SelectAllRowsContextMenuCommand = ReactiveCommand.CreateFromTask<DataGrid?>(SelectAllRowsContextMenuAsync);
        OpenFileContextMenuCommand = ReactiveCommand.CreateFromTask<DataGrid?>(OpenFileContextMenuAsync);
        OpenFolderContextMenuCommand = ReactiveCommand.CreateFromTask<DataGrid?>(OpenFolderContextMenuAsync);
        RenameContextMenuCommand = ReactiveCommand.CreateFromTask<DataGrid?>(RenameContextMenuAsync);
        ChangeFolderContextMenuCommand = ReactiveCommand.CreateFromTask<DataGrid?>(ChangeFolderContextMenuAsync);
        RedownloadContextMenuCommand = ReactiveCommand.CreateFromTask<DataGrid?>(RedownloadContextMenuAsync);
        ResumeContextMenuCommand = ReactiveCommand.CreateFromTask<DataGrid?>(ResumeContextMenuAsync);
        StopContextMenuCommand = ReactiveCommand.CreateFromTask<DataGrid?>(StopContextMenuAsync);
        RefreshDownloadAddressContextMenuCommand = ReactiveCommand.CreateFromTask<DataGrid?>(RefreshDownloadAddressContextMenuAsync);
        RemoveContextMenuCommand = ReactiveCommand.CreateFromTask<DataGrid?>(RemoveContextMenuAsync);
        AddToQueueContextMenuCommand = ReactiveCommand.CreateFromTask<DataGrid?>(AddToQueueContextMenuAsync);
        RemoveFromQueueContextMenuCommand = ReactiveCommand.CreateFromTask<DataGrid?>(RemoveFromQueueContextMenuAsync);
        AddDownloadFileToDownloadQueueContextMenuCommand = ReactiveCommand.CreateFromTask<DownloadQueueViewModel?>(AddDownloadFileToDownloadQueueContextMenuAsync);
        AddNewLinkMenuItemCommand = ReactiveCommand.CreateFromTask<Window?>(AddNewLinkMenuItemAsync);
        ExportDataMenuItemCommand = ReactiveCommand.CreateFromTask(ExportDataMenuItemAsync);
        ImportDataMenuItemCommand = ReactiveCommand.CreateFromTask(ImportDataMenuItemAsync);
        ExportSettingsMenuItemCommand = ReactiveCommand.CreateFromTask(ExportSettingsMenuItemAsync);
        ImportSettingsMenuItemCommand = ReactiveCommand.CreateFromTask(ImportSettingsMenuItemAsync);
        ExitProgramMenuItemCommand = ReactiveCommand.CreateFromTask(ExitProgramMenuItemAsync);
        StartAllDownloadsMenuItemCommand = ReactiveCommand.CreateFromTask(StartAllDownloadsMenuItemAsync);
        StopAllDownloadsMenuItemCommand = ReactiveCommand.CreateFromTask(StopAllDownloadsMenuItemAsync);
        PauseAllDownloadsMenuItemCommand = ReactiveCommand.CreateFromTask(PauseAllDownloadsMenuItemAsync);
        ResumeAllDownloadsMenuItemCommand = ReactiveCommand.CreateFromTask(ResumeAllDownloadsMenuItemAsync);
        StartDownloadMenuItemCommand = ReactiveCommand.CreateFromTask<DataGrid?>(StartDownloadMenuItemAsync);
        StopDownloadMenuItemCommand = ReactiveCommand.CreateFromTask<DataGrid?>(StopDownloadMenuItemAsync);
        PauseDownloadMenuItemCommand = ReactiveCommand.CreateFromTask<DataGrid?>(PauseDownloadMenuItemAsync);
        ResumeDownloadMenuItemCommand = ReactiveCommand.CreateFromTask<DataGrid?>(ResumeDownloadMenuItemAsync);
        RedownloadMenuItemCommand = ReactiveCommand.CreateFromTask<DataGrid?>(RedownloadMenuItemAsync);
        DeleteDownloadMenuItemCommand = ReactiveCommand.CreateFromTask<DataGrid?>(DeleteDownloadMenuItemAsync);
        DeleteAllCompletedDownloadsMenuItemCommand = ReactiveCommand.CreateFromTask(DeleteAllCompletedDownloadsMenuItemAsync);
        OpenSettingsMenuItemCommand = ReactiveCommand.CreateFromTask<Window?>(OpenSettingsMenuItemAsync);
    }

    private void LoadDownloadQueues()
    {
        var downloadQueues = AppService
            .DownloadQueueService
            .DownloadQueues;

        DownloadQueues.UpdateCollection(downloadQueues, dq => dq.Id);
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

    private async Task AddNewLinkAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            var url = string.Empty;
            if (owner.Clipboard != null)
                url = await owner.Clipboard.GetTextAsync();

            url = url?.Replace('\\', '/').Trim();
            var urlIsValid = url.CheckUrlValidation();
            var showStartDownloadDialog = AppService.SettingsService.Settings.ShowStartDownloadDialog;
            // Go to AddDownloadLinkWindow (Start download dialog) and let user choose what he/she want
            if (showStartDownloadDialog)
            {
                var vm = new AddDownloadLinkWindowViewModel(AppService)
                {
                    IsLoadingUrl = urlIsValid,
                    DownloadFile =
                    {
                        Url = urlIsValid ? url : null
                    }
                };

                var window = new AddDownloadLinkWindow { DataContext = vm };
                await window.ShowDialog(owner);

                await LoadCategoriesAsync();
            }
            // Otherwise, add link to database and start it
            else
            {
                if (!urlIsValid)
                    return;

                await AddNewDownloadFileAndStartItAsync(url!);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to add a new link. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task ResumeDownloadFileAsync(DataGrid? dataGrid)
    {
        await ResumeDownloadFileOperationAsync(dataGrid);
    }

    private async Task StopDownloadFileAsync(DataGrid? dataGrid)
    {
        try
        {
            if (dataGrid == null || dataGrid.SelectedItems.Count == 0)
                return;

            var downloadFiles = dataGrid
                .SelectedItems
                .OfType<DownloadFileViewModel>()
                .Where(df => !df.IsStopping && (df.IsDownloading || df.IsPaused))
                .ToList();

            if (downloadFiles.Count == 0)
                return;
            
            var cantOrUndefinedResumeDownloadFiles = downloadFiles
                .Where(df => df.CanResumeDownload != true)
                .ToList();

            if (cantOrUndefinedResumeDownloadFiles.Count > 0)
            {
                var result = await DialogBoxManager.ShowInfoDialogAsync("Stop download",
                    "Some files cannot continue downloading, or their status is uncertain. Do you still want to stop them?",
                    DialogButtons.YesNo);

                if (result == DialogResult.No)
                {
                    foreach (var downloadFile in cantOrUndefinedResumeDownloadFiles)
                    {
                        dataGrid.SelectedItems.Remove(downloadFile);
                        downloadFiles.Remove(downloadFile);
                    }
                }
            }

            foreach (var downloadFile in downloadFiles)
            {
                _ = AppService
                    .DownloadFileService
                    .StopDownloadFileAsync(downloadFile);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to stop a download file. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task StopAllDownloadFilesAsync()
    {
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
                .Where(df => df.IsDownloading || df.IsPaused)
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
            Log.Error(ex, "An error occured while trying to stop all download files. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task DeleteDownloadFilesAsync(DataGrid? dataGrid)
    {
        await RemoveDownloadFilesAsync(dataGrid, excludeFilesInRunningQueues: false);
    }

    private async Task DeleteCompletedDownloadFilesAsync()
    {
        try
        {
            var downloadFiles = AppService
                .DownloadFileService
                .DownloadFiles
                .Where(df => df.IsCompleted)
                .ToList();

            var existingDownloadFiles = downloadFiles
                .Where(df => !df.SaveLocation.IsNullOrEmpty() && !df.FileName.IsNullOrEmpty())
                .Select(df => Path.Combine(df.SaveLocation!, df.FileName!))
                .Where(File.Exists)
                .ToList();

            var deleteFile = false;
            if (existingDownloadFiles.Count > 0)
            {
                var result = await DialogBoxManager.ShowWarningDialogAsync("Delete files",
                    $"Do you want to delete the downloaded file{(existingDownloadFiles.Count > 1 ? "s" : "")} from your computer?",
                    DialogButtons.YesNo);

                if (result == DialogResult.Yes)
                    deleteFile = true;
            }

            foreach (var downloadFile in downloadFiles)
            {
                await AppService
                    .DownloadFileService
                    .DeleteDownloadFileAsync(downloadFile, deleteFile);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to delete completed download files. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task OpenSettingsWindowAsync(Window? owner)
    {
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
            Log.Error(ex, "An error occured while trying to open settings window. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task StartStopDownloadQueueAsync(DownloadQueueViewModel? downloadQueue)
    {
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
            Log.Error(ex, "An error occured while trying to start/stop a download queue. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task ShowDownloadQueueDetailsAsync(Button? button)
    {
        try
        {
            var owner = button?.FindLogicalAncestorOfType<Window>();
            if (owner == null)
                return;

            var tag = button?.Tag?.ToString();
            if (tag.IsNullOrEmpty() || !int.TryParse(tag, out var downloadQueueId))
            {
                await DialogBoxManager.ShowInfoDialogAsync("Queue", "Queue not found", DialogButtons.Ok);
                return;
            }

            var downloadQueue = AppService
                .DownloadQueueService
                .DownloadQueues
                .FirstOrDefault(dq => dq.Id == downloadQueueId);

            if (downloadQueue == null)
            {
                await DialogBoxManager.ShowInfoDialogAsync("Queue", "Queue not found", DialogButtons.Ok);
                return;
            }

            var vm = new AddEditQueueWindowViewModel(AppService, downloadQueue);
            var window = new AddEditQueueWindow { DataContext = vm };
            await window.ShowDialog(owner);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to show download queue details. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task AddNewDownloadQueueAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            var vm = new AddEditQueueWindowViewModel(AppService, null);
            var window = new AddEditQueueWindow { DataContext = vm };
            await window.ShowDialog(owner);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to add new download queue. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private static async Task ExitProgramMenuItemAsync()
    {
        try
        {
            if (App.Desktop == null)
                return;

            var result = await DialogBoxManager.ShowWarningDialogAsync("Exit",
                "Are you sure you want to exit the app?",
                DialogButtons.YesNo);

            if (result != DialogResult.Yes)
                return;

            App.Desktop.Shutdown();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while exit the app. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task StartAllDownloadsMenuItemAsync()
    {
        try
        {
            var downloadFiles = DownloadFiles
                .Where(df => df is { IsDownloading: false, IsCompleted: false, IsStopping: false })
                .ToList();

            if (downloadFiles.Count == 0)
                return;

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
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to start all downloads. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task StopAllDownloadsMenuItemAsync()
    {
        try
        {
            var downloadFiles = DownloadFiles
                .Where(df => !df.IsStopping && (df.IsDownloading || df.IsPaused))
                .ToList();

            if (downloadFiles.Count == 0)
                return;

            await StopAllDownloadFilesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to stop all downloads. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task PauseAllDownloadsMenuItemAsync()
    {
        try
        {
            var downloadFiles = DownloadFiles
                .Where(df => df is { IsStopping: false, IsDownloading: true })
                .ToList();

            if (downloadFiles.Count == 0)
                return;

            foreach (var downloadFile in downloadFiles)
            {
                AppService
                    .DownloadFileService
                    .PauseDownloadFile(downloadFile);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to pause all downloads. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task ResumeAllDownloadsMenuItemAsync()
    {
        try
        {
            var downloadFiles = DownloadFiles
                .Where(df => df is { IsStopping: false, IsPaused: true })
                .ToList();

            if (downloadFiles.Count == 0)
                return;

            foreach (var downloadFile in downloadFiles)
            {
                AppService
                    .DownloadFileService
                    .ResumeDownloadFile(downloadFile);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to resume all downloads. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task StartDownloadMenuItemAsync(DataGrid? dataGrid)
    {
        await ResumeDownloadFileAsync(dataGrid);
    }

    private async Task StopDownloadMenuItemAsync(DataGrid? dataGrid)
    {
        await StopDownloadFileAsync(dataGrid);
    }

    private async Task PauseDownloadMenuItemAsync(DataGrid? dataGrid)
    {
        try
        {
            if (dataGrid == null || dataGrid.SelectedItems.Count == 0)
                return;

            var downloadFiles = dataGrid
                .SelectedItems
                .OfType<DownloadFileViewModel>()
                .Where(df => df is { IsStopping: false, IsDownloading: true })
                .ToList();

            if (downloadFiles.Count == 0)
                return;

            foreach (var downloadFile in downloadFiles)
            {
                AppService
                    .DownloadFileService
                    .PauseDownloadFile(downloadFile);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to pause all downloads. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task ResumeDownloadMenuItemAsync(DataGrid? dataGrid)
    {
        try
        {
            if (dataGrid == null || dataGrid.SelectedItems.Count == 0)
                return;

            var downloadFiles = dataGrid
                .SelectedItems
                .OfType<DownloadFileViewModel>()
                .Where(df => df is { IsStopping: false, IsPaused: true })
                .ToList();

            if (downloadFiles.Count == 0)
                return;

            foreach (var downloadFile in downloadFiles)
            {
                AppService
                    .DownloadFileService
                    .ResumeDownloadFile(downloadFile);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to pause all downloads. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task RedownloadMenuItemAsync(DataGrid? dataGrid)
    {
        await RedownloadContextMenuAsync(dataGrid);
    }

    private async Task DeleteDownloadMenuItemAsync(DataGrid? dataGrid)
    {
        await RemoveDownloadFilesAsync(dataGrid, excludeFilesInRunningQueues: false);
    }

    private async Task DeleteAllCompletedDownloadsMenuItemAsync()
    {
        await DeleteCompletedDownloadFilesAsync();
    }

    private async Task OpenSettingsMenuItemAsync(Window? owner)
    {
        await OpenSettingsWindowAsync(owner);
    }

    private async Task SelectAllRowsContextMenuAsync(DataGrid? dataGrid)
    {
        try
        {
            await HideContextMenuAsync();
            dataGrid?.SelectAll();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to select all rows. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task OpenFileContextMenuAsync(DataGrid? dataGrid)
    {
        try
        {
            await HideContextMenuAsync();

            if (dataGrid?.SelectedItem is not DownloadFileViewModel { IsDownloading: true } downloadFile ||
                downloadFile.SaveLocation.IsNullOrEmpty() ||
                downloadFile.FileName.IsNullOrEmpty())
            {
                return;
            }

            var filePath = Path.Combine(downloadFile.SaveLocation!, downloadFile.FileName!);
            if (!File.Exists(filePath))
            {
                await DialogBoxManager.ShowInfoDialogAsync("Open file", "File not found", DialogButtons.Ok);
                return;
            }

            PlatformSpecificManager.OpenContainingFolderAndSelectFile(filePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to open the file. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task OpenFolderContextMenuAsync(DataGrid? dataGrid)
    {
        try
        {
            await HideContextMenuAsync();

            if (dataGrid == null || dataGrid.SelectedItems.Count == 0)
                return;

            var filePathList = dataGrid
                .SelectedItems
                .OfType<DownloadFileViewModel>()
                .Where(df => !df.SaveLocation.IsNullOrEmpty() && !df.FileName.IsNullOrEmpty())
                .Select(df => Path.Combine(df.SaveLocation!, df.FileName!))
                .Where(File.Exists)
                .Distinct()
                .ToList();

            if (filePathList.Count == 0)
            {
                await DialogBoxManager.ShowInfoDialogAsync("Open folder", "No folders found", DialogButtons.Ok);
                return;
            }

            filePathList.ForEach(PlatformSpecificManager.OpenContainingFolderAndSelectFile);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to open the folder. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task RenameContextMenuAsync(DataGrid? dataGrid)
    {
        try
        {
            await HideContextMenuAsync();

            if (dataGrid?.SelectedItem is not DownloadFileViewModel { IsCompleted: true } downloadFile ||
                downloadFile.FileName.IsNullOrEmpty() ||
                downloadFile.SaveLocation.IsNullOrEmpty())
            {
                return;
            }

            var filePath = Path.Combine(downloadFile.SaveLocation!, downloadFile.FileName!);
            if (!File.Exists(filePath))
            {
                await DialogBoxManager.ShowInfoDialogAsync("Rename", "File not found", DialogButtons.Ok);
                return;
            }

            var vm = new ChangeFileNameWindowViewModel(AppService, downloadFile);
            var window = new ChangeFileNameWindow { DataContext = vm };
            window.Show();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to rename the file. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task ChangeFolderContextMenuAsync(DataGrid? dataGrid)
    {
        try
        {
            await HideContextMenuAsync();

            if (dataGrid?.SelectedItem is not DownloadFileViewModel { IsCompleted: true } downloadFile ||
                downloadFile.FileName.IsNullOrEmpty() ||
                downloadFile.SaveLocation.IsNullOrEmpty() ||
                _mainWindow == null)
            {
                return;
            }

            var filePath = Path.Combine(downloadFile.SaveLocation!, downloadFile.FileName!);
            if (!File.Exists(filePath))
            {
                await DialogBoxManager.ShowInfoDialogAsync("Change folder", "File not found", DialogButtons.Ok);
                return;
            }

            var newSaveLocation = await _mainWindow.ChangeSaveLocationAsync(downloadFile.SaveLocation!);
            if (newSaveLocation.IsNullOrEmpty())
            {
                await DialogBoxManager.ShowInfoDialogAsync("Change folder", "Folder not found", DialogButtons.Ok);
                return;
            }

            var newFilePath = Path.Combine(newSaveLocation!, downloadFile.FileName!);
            if (File.Exists(newFilePath))
            {
                await DialogBoxManager.ShowInfoDialogAsync("Change folder", "File already exists", DialogButtons.Ok);
                return;
            }

            File.Move(filePath, newFilePath);

            downloadFile.SaveLocation = newSaveLocation;

            await AppService
                .DownloadFileService
                .UpdateDownloadFileAsync(downloadFile);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to change the folder. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task RedownloadContextMenuAsync(DataGrid? dataGrid)
    {
        try
        {
            await HideContextMenuAsync();

            if (dataGrid == null || dataGrid.SelectedItems.Count == 0)
                return;

            var downloadFiles = dataGrid
                .SelectedItems
                .OfType<DownloadFileViewModel>()
                .Where(df => df is { IsStopping: false, IsDownloading: false, IsPaused: false, DownloadProgress: > 0 })
                .ToList();

            if (downloadFiles.Count == 0)
                return;

            foreach (var downloadFile in downloadFiles)
            {
                await AppService
                    .DownloadFileService
                    .RedownloadDownloadFileAsync(downloadFile);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to redownload the file. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task ResumeContextMenuAsync(DataGrid? dataGrid)
    {
        try
        {
            if (dataGrid == null || dataGrid.SelectedItems.Count == 0)
                return;

            await ResumeDownloadFileOperationAsync(dataGrid, includePausedFiles: true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to resume the download. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task StopContextMenuAsync(DataGrid? dataGrid)
    {
        await StopDownloadFileAsync(dataGrid);
    }

    private async Task RefreshDownloadAddressContextMenuAsync(DataGrid? dataGrid)
    {
        try
        {
            if (dataGrid?.SelectedItem is not DownloadFileViewModel
                {
                    IsDownloading: false,
                    IsCompleted: false,
                    IsStopping: false
                } downloadFile)
            {
                return;
            }

            var vm = new RefreshDownloadAddressWindowViewModel(AppService, downloadFile);
            var window = new RefreshDownloadAddressWindow { DataContext = vm };
            window.Show();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to refresh the download address. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task RemoveContextMenuAsync(DataGrid? dataGrid)
    {
        await HideContextMenuAsync();
        await RemoveDownloadFilesAsync(dataGrid, excludeFilesInRunningQueues: true);
    }

    private async Task AddToQueueContextMenuAsync(DataGrid? dataGrid)
    {
        try
        {
            // Clear previous data stored in AddToQueueDownloadQueues
            AddToQueueDownloadQueues = [];
            if (dataGrid == null || dataGrid.SelectedItems.Count == 0)
            {
                await HideContextMenuAsync();
                return;
            }

            // Get selected download files that are not completed
            var downloadFiles = dataGrid
                .SelectedItems
                .OfType<DownloadFileViewModel>()
                .Where(df => df is { IsCompleted: false, IsStopping: false })
                .ToList();

            var downloadQueues = AppService
                .DownloadQueueService
                .DownloadQueues;

            if (downloadQueues.Count == 0)
            {
                await HideContextMenuAsync();
                return;
            }

            switch (downloadFiles.Count)
            {
                // If count of selected download files is equal to 0 then hide context menu
                case 0:
                {
                    await HideContextMenuAsync();
                    return;
                }

                // If count of selected download files is equal to 1 then show all download queues except the one that currently in use
                case 1:
                {
                    var downloadQueue = downloadQueues.FirstOrDefault(dq => dq.Id == downloadFiles[0].DownloadQueueId);
                    if (downloadQueue == null)
                    {
                        AddToQueueDownloadQueues = downloadQueues;
                    }
                    else
                    {
                        AddToQueueDownloadQueues = downloadQueues
                            .Where(dq => dq.Id != downloadQueue.Id)
                            .ToObservableCollection();

                        if (AddToQueueDownloadQueues.Count == 0)
                            AddToQueueFlyout?.Hide();
                    }

                    break;
                }

                default:
                {
                    var primaryKeys = downloadFiles
                        .Where(df => df is { DownloadQueueId: not null })
                        .Select(df => df.DownloadQueueId!.Value)
                        .Distinct()
                        .ToList();

                    switch (primaryKeys.Count)
                    {
                        // If count of primary keys is equal to 1 then show all download queues except the one with primary key
                        case 1:
                        {
                            AddToQueueDownloadQueues = downloadQueues
                                .Where(dq => !primaryKeys.Contains(dq.Id))
                                .ToObservableCollection();

                            if (AddToQueueDownloadQueues.Count == 0)
                                AddToQueueFlyout?.Hide();

                            break;
                        }

                        // If count of primary keys is 0 or more than 1 then show all download queues
                        default:
                        {
                            AddToQueueDownloadQueues = downloadQueues;
                            break;
                        }
                    }

                    break;
                }
            }

            _selectedDownloadFilesToAddToQueue = downloadFiles;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to add to queue. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task RemoveFromQueueContextMenuAsync(DataGrid? dataGrid)
    {
        try
        {
            await HideContextMenuAsync();

            if (dataGrid == null || dataGrid.SelectedItems.Count == 0)
                return;

            var downloadQueues = AppService
                .DownloadQueueService
                .DownloadQueues;

            if (downloadQueues.Count == 0)
                return;

            var downloadFiles = dataGrid
                .SelectedItems
                .OfType<DownloadFileViewModel>()
                .Where(df =>
                    df is
                    {
                        DownloadQueueId: not null,
                        IsDownloading: false,
                        IsPaused: false,
                        IsCompleted: false,
                        IsStopping: false
                    } &&
                    downloadQueues.FirstOrDefault(dq => dq.Id == df.DownloadQueueId) != null)
                .ToList();

            foreach (var downloadFile in downloadFiles)
            {
                var downloadQueue = downloadQueues.First(dq => dq.Id == downloadFile.DownloadQueueId);
                await AppService
                    .DownloadQueueService
                    .RemoveDownloadFileFromDownloadQueueAsync(downloadQueue, downloadFile);
            }

            FilterDownloadList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to remove from queue. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task AddDownloadFileToDownloadQueueContextMenuAsync(DownloadQueueViewModel? viewModel)
    {
        try
        {
            await HideContextMenuAsync();

            var downloadQueue = AppService
                .DownloadQueueService
                .DownloadQueues
                .FirstOrDefault(dq => dq.Id == viewModel?.Id);

            if (downloadQueue == null || _selectedDownloadFilesToAddToQueue.Count == 0)
                return;

            await AppService
                .DownloadQueueService
                .AddDownloadFilesToDownloadQueueAsync(downloadQueue, _selectedDownloadFilesToAddToQueue);

            FilterDownloadList();
            _selectedDownloadFilesToAddToQueue.Clear();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to add to queue. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task AddNewLinkMenuItemAsync(Window? owner)
    {
        await AddNewLinkAsync(owner);
    }

    private async Task ExportDataMenuItemAsync()
    {
        try
        {
            // Get main window
            var mainWindow = App.Desktop?.MainWindow ?? throw new InvalidOperationException("Main window not found.");

            // Get temp folder path
            var tempPath = Path.GetTempPath();

            // Check for downloading files
            var downloadFiles = AppService
                .DownloadFileService
                .DownloadFiles
                .Where(df => df.IsDownloading || df.IsPaused)
                .ToList();

            // Stop downloading files
            if (downloadFiles.Count > 0)
            {
                var result = await DialogBoxManager.ShowInfoDialogAsync("Export data",
                    "Some downloads are still in progress. To export now, you'll need to stop them. Continue with the export and stop the downloads?",
                    DialogButtons.YesNo);

                if (result != DialogResult.Yes)
                    return;

                var stopTasks = downloadFiles
                    .ConvertAll(df => AppService.DownloadFileService.StopDownloadFileAsync(df, ensureStopped: true, playSound: false));

                await Task.WhenAll(stopTasks);
            }

            // Export download files
            downloadFiles = AppService
                .DownloadFileService
                .DownloadFiles
                .Where(df => !df.IsCompleted)
                .ToList();

            // Check for completed download files
            var completedDownloadFiles = AppService
                .DownloadFileService
                .DownloadFiles
                .Where(df => df.IsCompleted)
                .ToList();

            // Ask user if they want to export completed download files
            if (completedDownloadFiles.Count > 0)
            {
                var result = await DialogBoxManager.ShowInfoDialogAsync("Export data",
                    "Some downloads are complete. Would you like to export the finished files?",
                    DialogButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    downloadFiles = downloadFiles
                        .Union(completedDownloadFiles)
                        .Distinct()
                        .ToList();
                }
            }

            // Get existing files
            var filePathList = downloadFiles
                .Where(df => !df.SaveLocation.IsNullOrEmpty() && !df.FileName.IsNullOrEmpty())
                .Select(df => Path.Combine(df.SaveLocation!, df.FileName!))
                .Where(File.Exists)
                .ToList();

            // Include original files in export
            var includeFiles = false;
            if (filePathList.Count > 0)
            {
                var result = await DialogBoxManager.ShowWarningDialogAsync("Export data",
                    "Would you like to include the original files in the export? This will create a larger export file, but ensures all data is preserved.",
                    DialogButtons.YesNo);

                includeFiles = result == DialogResult.Yes;
            }

            // Check disk space for creating export
            if (includeFiles)
            {
                var totalFileSizes = filePathList.Sum(f => new FileInfo(f).Length);
                var driveInfo = new DriveInfo(tempPath);
                var availableSpace = driveInfo.AvailableFreeSpace;
                const long ensureExistSpace = 10 * Constants.MegaByte;

                // If total size + 10 MB greater than available space, notify user and don't save files with export
                while (includeFiles && totalFileSizes + ensureExistSpace > availableSpace)
                {
                    var driveName = driveInfo.GetDriveName();
                    var fileSize = totalFileSizes.ToFileSize(roundSize: true, roundToUpper: true);

                    var result = await DialogBoxManager.ShowDangerDialogAsync("Export data",
                        $"There isn't enough space on your '{driveName}' drive (about {fileSize} is needed) to include the downloaded files in the export. Would you like to " +
                        "choose a different save location? If not, the downloaded files will be removed from the export.",
                        DialogButtons.YesNoCancel);

                    switch (result)
                    {
                        case DialogResult.No:
                        {
                            includeFiles = false;
                            break;
                        }

                        case DialogResult.Yes:
                        {
                            var folderPickerOpenOptions = new FolderPickerOpenOptions
                            {
                                Title = "Export location",
                                AllowMultiple = false
                            };

                            var folderPickerResult = await mainWindow.StorageProvider.OpenFolderPickerAsync(folderPickerOpenOptions);
                            if (!folderPickerResult.Any())
                                return;

                            tempPath = folderPickerResult[0].Path.LocalPath;
                            driveInfo = new DriveInfo(tempPath);
                            availableSpace = driveInfo.AvailableFreeSpace;
                            break;
                        }

                        default:
                            return;
                    }
                }
            }

            // Create export download files
            var exportDownloadFiles = downloadFiles
                .ConvertAll(df =>
                {
                    var exportData = ExportDownloadFileViewModel.CreateExportFile(df);
                    if (includeFiles)
                        return exportData;

                    exportData.FileName = string.Empty;
                    exportData.Size = 0;
                    exportData.Status = DownloadFileStatus.None;
                    exportData.DownloadProgress = 0;
                    exportData.DownloadPackage = null;
                    return exportData;
                });

            // Export download queues
            var exportDownloadQueues = new List<ExportDownloadQueueViewModel>();

            // Check for download queues
            var downloadQueues = AppService
                .DownloadQueueService
                .DownloadQueues
                .Where(dq => dq.Title?.Equals(Constants.DefaultDownloadQueueTitle) != true)
                .ToList();

            // Create export download queues
            if (downloadQueues.Count > 0)
            {
                var result = await DialogBoxManager.ShowInfoDialogAsync("Export data",
                    "Would you like to export your current download queues?",
                    DialogButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    // Convert download queues to export model
                    exportDownloadQueues = downloadQueues.ConvertAll(ExportDownloadQueueViewModel.CreateExportFile);
                }
                else
                {
                    exportDownloadFiles = ClearDownloadQueuesFromExport(exportDownloadFiles);
                }
            }
            else
            {
                exportDownloadFiles = ClearDownloadQueuesFromExport(exportDownloadFiles);
            }

            // Export settings
            ExportSettingsViewModel? exportSettings = null;

            // Let user choose for settings
            var exportSettingsResult = await DialogBoxManager.ShowInfoDialogAsync("Export data",
                "Would you like to include your current settings in the export file?",
                DialogButtons.YesNo);

            // Convert settings to export model
            if (exportSettingsResult == DialogResult.Yes)
                exportSettings = ExportSettingsData();

            // Choose a unique directory
            var tempFolderName = Guid.NewGuid().ToString();
            var tempFolderPath = Path.Combine(tempPath, tempFolderName);
            while (Directory.Exists(tempFolderPath))
            {
                tempFolderName = Guid.NewGuid().ToString();
                tempFolderPath = Path.Combine(tempPath, tempFolderName);
            }

            // Define temp folders
            var dataTempFolder = Path.Combine(tempFolderPath, "data");
            var filesTempFolder = Path.Combine(tempFolderPath, "files");

            // Create directories
            Directory.CreateDirectory(tempFolderPath);
            Directory.CreateDirectory(dataTempFolder);
            Directory.CreateDirectory(filesTempFolder);

            // Save download files data
            var json = exportDownloadFiles.ConvertToJson();
            await File.WriteAllTextAsync(Path.Combine(dataTempFolder, "download-files.json"), json);

            // Save download queues data
            json = exportDownloadQueues.ConvertToJson();
            await File.WriteAllTextAsync(Path.Combine(dataTempFolder, "download-queues.json"), json);

            // Save download queues data
            json = exportSettings.ConvertToJson();
            await File.WriteAllTextAsync(Path.Combine(dataTempFolder, "settings.json"), json);

            // Save files
            if (includeFiles)
            {
                foreach (var filePath in filePathList)
                {
                    var fileName = Path.GetFileName(filePath);
                    var destinationFilePath = Path.Combine(filesTempFolder, fileName);
                    await filePath.CopyFileAsync(destinationFilePath);
                }
            }

            // Open save file picker and save export file
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            List<FilePickerFileType> fileTypes = [new("CDM export file") { Patterns = ["*.cdm"] }];

            var savePickerOpenOptions = new FilePickerSaveOptions
            {
                Title = "Export data",
                SuggestedStartLocation = await mainWindow.StorageProvider.TryGetFolderFromPathAsync(desktopPath),
                SuggestedFileName = "cdm-data",
                DefaultExtension = "cdm",
                FileTypeChoices = fileTypes
            };

            var savePickerResult = await mainWindow.StorageProvider.SaveFilePickerAsync(savePickerOpenOptions);
            if (savePickerResult == null)
                return;

            // Zip export file and save it
            // savePickerResult.Path.LocalPath.ZipFolder(tempFolderPath);
            await tempFolderPath.ZipDirectoryAsync(savePickerResult.Path.LocalPath);

            // Remove temp folder
            Directory.Delete(tempFolderPath, true);

            // Show success message
            await DialogBoxManager.ShowSuccessDialogAsync("Export data", "Your export has been created successfully.", DialogButtons.Ok);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to export data. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task ImportDataMenuItemAsync()
    {
        try
        {
            // Get main window
            var mainWindow = App.Desktop?.MainWindow ?? throw new InvalidOperationException("Main window not found.");

            // Open file picker
            var filePickerOpenOptions = new FilePickerOpenOptions
            {
                Title = "Import data",
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("CDM export file") { Patterns = ["*.cdm"] }]
            };

            var filePickerResult = await mainWindow.StorageProvider.OpenFilePickerAsync(filePickerOpenOptions);
            if (!filePickerResult.Any())
                return;

            var filePath = filePickerResult[0].Path.LocalPath;
            if (!filePath.EndsWith(".cdm", StringComparison.OrdinalIgnoreCase))
            {
                await DialogBoxManager.ShowDangerDialogAsync("Import data",
                    "The file format could not be recognized. Please ensure you are importing a CDM export file.",
                    DialogButtons.Ok);

                return;
            }

            // Choose a unique directory
            var tempPath = Path.GetTempPath();
            var tempFolderName = Guid.NewGuid().ToString();
            var tempExportPath = Path.Combine(tempPath, tempFolderName);
            while (Directory.Exists(tempExportPath))
            {
                tempFolderName = Guid.NewGuid().ToString();
                tempExportPath = Path.Combine(tempPath, tempFolderName);
            }

            // Create directory
            Directory.CreateDirectory(tempExportPath);

            // Unzip export file
            await filePath.UnZipFileAsync(tempExportPath);

            var dataTempFolder = Path.Combine(tempExportPath, "data");
            var filesTempFolder = Path.Combine(tempExportPath, "files");

            // Load download files data
            var json = await File.ReadAllTextAsync(Path.Combine(dataTempFolder, "download-files.json"));
            var downloadFiles = json.ConvertFromJson<List<ExportDownloadFileViewModel>>();

            // Load download queues data
            json = await File.ReadAllTextAsync(Path.Combine(dataTempFolder, "download-queues.json"));
            var downloadQueues = json.ConvertFromJson<List<ExportDownloadQueueViewModel>>();

            // Load settings data
            json = await File.ReadAllTextAsync(Path.Combine(dataTempFolder, "settings.json"));
            var settings = json.ConvertFromJson<ExportSettingsViewModel>();

            // Import settings
            await ImportSettingsAsync(settings);

            // Import download queues
            var addedDownloadQueues = await ImportDownloadQueuesAsync(downloadQueues);

            // Import download files
            var addedDownloadFiles = await ImportDownloadFilesAsync(downloadFiles, addedDownloadQueues);

            // Import files
            await ImportFilesAsync(filesTempFolder, addedDownloadFiles);

            // Remove temp folder
            Directory.Delete(tempExportPath, true);

            // Show success message
            await DialogBoxManager.ShowSuccessDialogAsync("Import data",
                "Your data has been successfully imported.",
                DialogButtons.Ok);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to import data. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task ExportSettingsMenuItemAsync()
    {
        try
        {
            // Get main window
            var mainWindow = App.Desktop?.MainWindow ?? throw new InvalidOperationException("Main window not found.");

            // Open save file picker and save export file
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            List<FilePickerFileType> fileTypes =
            [
                new("Json file") { Patterns = ["*.json"] },
                new("Text file") { Patterns = ["*.txt"] }
            ];

            var savePickerOpenOptions = new FilePickerSaveOptions
            {
                Title = "Export settings",
                SuggestedStartLocation = await mainWindow.StorageProvider.TryGetFolderFromPathAsync(desktopPath),
                SuggestedFileName = "cdm-settings",
                DefaultExtension = "json",
                FileTypeChoices = fileTypes
            };

            var savePickerResult = await mainWindow.StorageProvider.SaveFilePickerAsync(savePickerOpenOptions);
            if (savePickerResult == null)
                return;

            var filePath = savePickerResult.Path.LocalPath;
            if (filePath.IsNullOrEmpty())
                return;

            var directory = Path.GetDirectoryName(filePath);
            if (directory.IsNullOrEmpty())
                return;

            if (!Directory.Exists(directory!))
                Directory.CreateDirectory(directory!);

            var exportSettings = ExportSettingsData();
            var json = exportSettings.ConvertToJson();

            await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to export settings. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task ImportSettingsMenuItemAsync()
    {
        try
        {
            // Get main window
            var mainWindow = App.Desktop?.MainWindow ?? throw new InvalidOperationException("Main window not found.");

            // Open file picker
            var filePickerOpenOptions = new FilePickerOpenOptions
            {
                Title = "Import settings",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Json file") { Patterns = ["*.json"] },
                    new FilePickerFileType("Text file") { Patterns = ["*.txt"] }
                ]
            };

            var filePickerResult = await mainWindow.StorageProvider.OpenFilePickerAsync(filePickerOpenOptions);
            if (!filePickerResult.Any() || filePickerResult[0].Path.LocalPath.IsNullOrEmpty())
                return;

            var filePath = filePickerResult[0].Path.LocalPath;
            var json = await File.ReadAllTextAsync(filePath);
            var exportSettings = json.ConvertFromJson<ExportSettingsViewModel?>();
            await ImportSettingsAsync(exportSettings);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to import settings. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private void UpdateDownloadSpeedTimerOnTick(object? sender, EventArgs e)
    {
        DownloadSpeed = AppService
            .DownloadFileService
            .GetDownloadSpeed();
    }

    private void UpdateActiveDownloadQueuesTimerOnTick(object? sender, EventArgs e)
    {
        var downloadQueues = AppService
            .DownloadQueueService
            .DownloadQueues
            .Where(dq => dq.IsRunning)
            .ToObservableCollection();

        bool isChanged;
        if (ActiveDownloadQueues.Count != downloadQueues.Count)
        {
            isChanged = true;
        }
        else
        {
            isChanged = downloadQueues
                .Where((t, i) => t.Id != ActiveDownloadQueues[i].Id)
                .Any();
        }

        if (isChanged)
            ActiveDownloadQueues = downloadQueues;
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

            CategoryHeaders = categoryHeaders.ToObservableCollection();
            SelectedCategoryHeader ??= CategoryHeaders.FirstOrDefault();

            if (CategoryHeaders.FirstOrDefault(ch => ch.Id == SelectedCategoryHeader?.Id) == null)
            {
                SelectedCategoryHeader = CategoryHeaders.FirstOrDefault();
                FilterDownloadList();
            }
        }
        catch
        {
            CategoryHeaders = [];
        }
    }

    private void FilterDownloadList()
    {
        try
        {
            var downloadFiles = AppService
                .DownloadFileService
                .DownloadFiles
                .ToList();

            if (SelectedCategoryHeader != null)
            {
                downloadFiles = SelectedCategoryHeader.Title switch
                {
                    Constants.UnfinishedCategoryHeaderTitle => downloadFiles
                        .Where(df => !df.IsCompleted)
                        .ToList(),

                    Constants.FinishedCategoryHeaderTitle => downloadFiles
                        .Where(df => df.IsCompleted)
                        .ToList(),

                    _ => downloadFiles
                };
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
                    .Where(df => df.Url?.Contains(SearchText!, StringComparison.OrdinalIgnoreCase) != false ||
                                 df.FileName?.Contains(SearchText!, StringComparison.OrdinalIgnoreCase) != false ||
                                 df.SaveLocation?.Contains(SearchText!, StringComparison.OrdinalIgnoreCase) != false)
                    .ToList();
            }

            DownloadFiles.UpdateCollection(downloadFiles.ToObservableCollection(), df => df.Id);
        }
        catch
        {
            var downloadFiles = AppService
                .DownloadFileService
                .DownloadFiles;

            DownloadFiles.UpdateCollection(downloadFiles, df => df.Id);
        }
    }

    protected override void OnDownloadFileServiceDataChanged()
    {
        Dispatcher.UIThread.InvokeAsync(FilterDownloadList);
    }

    protected override void OnDownloadQueueServiceDataChanged()
    {
        Dispatcher.UIThread.InvokeAsync(LoadDownloadQueues);
    }

    private string GetToolTipText()
    {
        if (!ActiveDownloadQueueExists)
            return string.Empty;

        var titles = ActiveDownloadQueues
            .Select(dq => dq.Title)
            .ToList();

        var text = string.Join(", ", titles);
        return $"Active queues: {text}";
    }

    public async Task ChangeContextFlyoutEnableStateAsync(MainWindow? mainWindow)
    {
        try
        {
            _mainWindow = mainWindow;
            var dataGrid = _mainWindow?.FindControl<DataGrid>("DownloadFilesDataGrid");
            if (dataGrid == null)
                return;

            if (DownloadFiles.Count == 0)
            {
                ContextFlyoutEnableState.ChangeAllPropertiesToFalse();
                return;
            }

            var downloadFiles = dataGrid
                .SelectedItems
                .OfType<DownloadFileViewModel>()
                .ToList();

            var downloadQueues = AppService
                .DownloadQueueService
                .DownloadQueues;

            ContextFlyoutEnableState.CanSelectAllRows = DownloadFiles.Count > 0 && downloadFiles.Count != DownloadFiles.Count;
            ContextFlyoutEnableState.CanOpenFile = downloadFiles is [{ IsCompleted: true }];
            ContextFlyoutEnableState.CanOpenFolder = downloadFiles.Count > 0;
            ContextFlyoutEnableState.CanRename = downloadFiles is [{ IsCompleted: true }];
            ContextFlyoutEnableState.CanChangeFolder = downloadFiles is [{ IsCompleted: true }];
            ContextFlyoutEnableState.CanRedownload = downloadFiles.Exists(df => df is { IsStopping: false, IsDownloading: false, IsPaused: false, DownloadProgress: > 0 });
            ContextFlyoutEnableState.CanResume = downloadFiles.Exists(df => df is { IsStopping: false, IsDownloading: false, IsCompleted: false });
            ContextFlyoutEnableState.CanStop = downloadFiles.Exists(df => !df.IsStopping && (df.IsDownloading || df.IsPaused) && df.CanResumeDownload != false);
            ContextFlyoutEnableState.CanRefreshDownloadAddress = downloadFiles is [{ IsStopping: false, IsDownloading: false, IsCompleted: false }];
            ContextFlyoutEnableState.CanRemove = downloadFiles
                .Exists(df => df.DownloadQueueId == null || downloadQueues.FirstOrDefault(dq => dq.Id == df.DownloadQueueId)?.IsRunning != true);

            ContextFlyoutEnableState.CanAddToQueue = downloadFiles.Exists(df => df is { IsCompleted: false, IsStopping: false });
            ContextFlyoutEnableState.CanRemoveFromQueue = downloadFiles
                .Exists(df => df is { DownloadQueueId: not null, IsDownloading: false, IsPaused: false, IsCompleted: false, IsStopping: false } &&
                              downloadQueues.FirstOrDefault(dq => dq.Id == df.DownloadQueueId) != null);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to change context flyout enable state. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    public void ChangeFileSubMenusEnableState(DataGrid? dataGrid)
    {
        if (dataGrid == null)
            return;

        var selectedDownloadFiles = dataGrid
            .SelectedItems
            .OfType<DownloadFileViewModel>()
            .ToList();

        MainMenuItemsEnabledState.IsStartAllDownloadsEnabled = DownloadFiles
            .FirstOrDefault(df => df is { IsDownloading: false, IsCompleted: false, IsStopping: false }) != null;

        MainMenuItemsEnabledState.IsStopAllDownloadsEnabled = DownloadFiles.FirstOrDefault(df => !df.IsStopping && (df.IsDownloading || df.IsPaused)) != null;
        MainMenuItemsEnabledState.IsPauseAllDownloadsEnabled = DownloadFiles.FirstOrDefault(df => df is { IsStopping: false, IsDownloading: true }) != null;
        MainMenuItemsEnabledState.IsResumeAllDownloadsEnabled = DownloadFiles.FirstOrDefault(df => df is { IsStopping: false, IsPaused: true }) != null;
        MainMenuItemsEnabledState.IsStartDownloadsEnabled = selectedDownloadFiles
            .Exists(df => df is { IsStopping: false, IsDownloading: false, IsPaused: false, IsCompleted: false });

        MainMenuItemsEnabledState.IsStopDownloadsEnabled = selectedDownloadFiles.Exists(df => !df.IsStopping && (df.IsDownloading || df.IsPaused));
        MainMenuItemsEnabledState.IsPauseDownloadsEnabled = selectedDownloadFiles.Exists(df => df is { IsStopping: false, IsDownloading: true });
        MainMenuItemsEnabledState.IsResumeDownloadsEnabled = selectedDownloadFiles.Exists(df => df is { IsStopping: false, IsPaused: true });
        MainMenuItemsEnabledState.IsRedownloadEnabled =
            selectedDownloadFiles.Exists(df => df is { IsStopping: false, IsDownloading: false, IsPaused: false, DownloadProgress: > 0 });

        MainMenuItemsEnabledState.IsDeleteDownloadsEnabled = selectedDownloadFiles.Exists(df => df is { IsStopping: false, IsDownloading: false, IsPaused: false });
        MainMenuItemsEnabledState.IsDeleteAllCompletedEnabled = DownloadFiles.FirstOrDefault(df => df is { IsStopping: false, IsCompleted: true }) != null;
    }

    private async Task HideContextMenuAsync()
    {
        try
        {
            await Task.Delay(100);
            _mainWindow?.HideDownloadFilesDataGridContextMenu();
            _mainWindow = null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to hide context menu. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task ResumeDownloadFileOperationAsync(DataGrid? dataGrid, bool includePausedFiles = false)
    {
        try
        {
            if (dataGrid == null || dataGrid.SelectedItems.Count == 0)
                return;

            var downloadFiles = dataGrid
                .SelectedItems
                .OfType<DownloadFileViewModel>()
                .Where(df => df is { IsDownloading: false, IsPaused: false, IsCompleted: false, IsStopping: false })
                .ToList();

            if (includePausedFiles)
            {
                var pausedFiles = dataGrid
                    .SelectedItems
                    .OfType<DownloadFileViewModel>()
                    .Where(df => df is { IsStopping: false, IsPaused: true })
                    .ToList();

                downloadFiles = downloadFiles
                    .Union(pausedFiles)
                    .Distinct()
                    .ToList();
            }

            if (downloadFiles.Count == 0)
                return;

            foreach (var downloadFile in downloadFiles)
            {
                if (includePausedFiles && downloadFile.IsPaused)
                {
                    AppService
                        .DownloadFileService
                        .ResumeDownloadFile(downloadFile);
                }
                else
                {
                    if (downloadFile.IsPaused)
                        continue;

                    _ = AppService
                        .DownloadFileService
                        .StartDownloadFileAsync(downloadFile);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to resume a download file. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task RemoveDownloadFilesAsync(DataGrid? dataGrid, bool excludeFilesInRunningQueues)
    {
        try
        {
            if (dataGrid == null || dataGrid.SelectedItems.Count == 0)
                return;

            var downloadFiles = dataGrid
                .SelectedItems
                .OfType<DownloadFileViewModel>()
                .ToList();

            if (excludeFilesInRunningQueues)
            {
                var downloadQueues = AppService
                    .DownloadQueueService
                    .DownloadQueues;

                downloadFiles = downloadFiles
                    .Where(df => df.DownloadQueueId == null || downloadQueues.FirstOrDefault(dq => dq.Id == df.DownloadQueueId)?.IsRunning != true)
                    .ToList();
            }

            if (downloadFiles.Count == 0)
                return;

            var existingDownloadFiles = downloadFiles
                .Where(df => !df.SaveLocation.IsNullOrEmpty() && !df.FileName.IsNullOrEmpty())
                .Select(df => Path.Combine(df.SaveLocation!, df.FileName!))
                .Where(File.Exists)
                .ToList();

            var deleteFile = false;
            if (existingDownloadFiles.Count > 0)
            {
                var result = await DialogBoxManager.ShowWarningDialogAsync("Delete files",
                    $"Do you want to delete the downloaded file{(existingDownloadFiles.Count > 1 ? "s" : "")} from your computer?",
                    DialogButtons.YesNo);

                if (result == DialogResult.Yes)
                    deleteFile = true;
            }

            var reloadData = downloadFiles.Count == 1;
            for (var i = downloadFiles.Count - 1; i >= 0; i--)
            {
                if (downloadFiles[i].IsDownloading)
                {
                    await AppService
                        .DownloadFileService
                        .StopDownloadFileAsync(downloadFiles[i], ensureStopped: true);
                }

                await AppService
                    .DownloadFileService
                    .DeleteDownloadFileAsync(downloadFiles[i], deleteFile, reloadData);
            }

            if (!reloadData)
            {
                await AppService
                    .DownloadFileService
                    .LoadDownloadFilesAsync();
            }

            dataGrid.SelectedItems.Clear();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to remove download files. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task AddNewDownloadFileAndStartItAsync(string url)
    {
        // Get url details
        var urlDetails = await AppService.DownloadFileService.GetUrlDetailsAsync(url);
        // Validate url details
        var validateResult = AppService.DownloadFileService.ValidateUrlDetails(urlDetails);
        if (!validateResult.IsValid)
        {
            if (validateResult.Title.IsNullOrEmpty() || validateResult.Message.IsNullOrEmpty())
            {
                await DialogBoxManager.ShowDangerDialogAsync("Error downloading file",
                    "An error occurred while downloading the file.",
                    DialogButtons.Ok);
            }
            else
            {
                await DialogBoxManager.ShowDangerDialogAsync(validateResult.Title!, validateResult.Message!, DialogButtons.Ok);
            }

            return;
        }

        DuplicateDownloadLinkAction? duplicateAction = null;
        if (urlDetails.IsUrlDuplicate)
        {
            var savedDuplicateAction = AppService.SettingsService.Settings.DuplicateDownloadLinkAction;
            if (savedDuplicateAction == DuplicateDownloadLinkAction.LetUserChoose)
            {
                duplicateAction = await AppService
                    .DownloadFileService
                    .GetUserDuplicateActionAsync(urlDetails.Url, urlDetails.FileName, urlDetails.Category!.CategorySaveDirectory!);
            }
            else
            {
                duplicateAction = savedDuplicateAction;
            }
        }

        var downloadFile = new DownloadFileViewModel
        {
            Url = urlDetails.Url,
            FileName = urlDetails.FileName,
            CategoryId = urlDetails.Category?.Id,
            Size = urlDetails.FileSize
        };

        await AppService.DownloadFileService.AddDownloadFileAsync(downloadFile,
            isUrlDuplicate: urlDetails.IsUrlDuplicate,
            duplicateAction: duplicateAction,
            isFileNameDuplicate: urlDetails.IsFileNameDuplicate,
            startDownloading: true);
    }

    private List<ExportDownloadFileViewModel> ClearDownloadQueuesFromExport(List<ExportDownloadFileViewModel> exportDownloadFiles)
    {
        var defaultDownloadQueue = AppService
            .DownloadQueueService
            .DownloadQueues
            .FirstOrDefault(dq => dq.Title?.Equals(Constants.DefaultDownloadQueueTitle) == true);

        // Remove download queue data from export download files
        return exportDownloadFiles
            .ConvertAll(df =>
            {
                df.AddedToDefaultQueue = df.DownloadQueueId != null && defaultDownloadQueue != null && defaultDownloadQueue.Id == df.DownloadQueueId;
                df.DownloadQueueId = null;
                df.DownloadQueuePriority = null;
                return df;
            });
    }

    private ExportSettingsViewModel ExportSettingsData()
    {
        var settings = AppService.SettingsService.Settings;
        var proxies = settings.Proxies.ToList();
        return ExportSettingsViewModel.CreateExportFile(settings, proxies);
    }

    private async Task ImportSettingsAsync(ExportSettingsViewModel? exportSettings)
    {
        if (exportSettings == null)
            return;

        var settings = AppService.SettingsService.Settings;

        // Update settings with new values
        settings.StartOnSystemStartup = exportSettings.StartOnSystemStartup;
        settings.UseBrowserExtension = exportSettings.UseBrowserExtension;
        settings.DarkMode = exportSettings.DarkMode;
        settings.AlwaysKeepManagerOnTop = exportSettings.AlwaysKeepManagerOnTop;
        settings.ShowStartDownloadDialog = exportSettings.ShowStartDownloadDialog;
        settings.ShowCompleteDownloadDialog = exportSettings.ShowCompleteDownloadDialog;
        settings.DuplicateDownloadLinkAction = exportSettings.DuplicateDownloadLinkAction;
        settings.MaximumConnectionsCount = exportSettings.MaximumConnectionsCount;
        settings.IsSpeedLimiterEnabled = exportSettings.IsSpeedLimiterEnabled;
        settings.LimitSpeed = exportSettings.LimitSpeed;
        settings.LimitUnit = exportSettings.LimitUnit;
        settings.ProxyMode = exportSettings.ProxyMode;
        settings.ProxyType = exportSettings.ProxyType;
        settings.UseDownloadCompleteSound = exportSettings.UseDownloadCompleteSound;
        settings.UseDownloadStoppedSound = exportSettings.UseDownloadStoppedSound;
        settings.UseDownloadFailedSound = exportSettings.UseDownloadFailedSound;
        settings.UseQueueStartedSound = exportSettings.UseQueueStartedSound;
        settings.UseQueueStoppedSound = exportSettings.UseQueueStoppedSound;
        settings.UseQueueFinishedSound = exportSettings.UseQueueFinishedSound;
        settings.UseSystemNotifications = exportSettings.UseSystemNotifications;

        await AppService.SettingsService.SaveSettingsAsync(settings);

        // Get saved proxies in database
        var proxiesInDb = AppService.SettingsService.Settings.Proxies.ToList();

        // Add new proxies
        var newProxies = exportSettings
            .Proxies
            .Where(proxy => proxiesInDb.Find(p => proxy.Type.Equals(p.Type) && proxy.Host.Equals(p.Host) && proxy.Port.Equals(p.Port)) == null)
            .Select(proxy => new ProxySettingsViewModel
            {
                Name = proxy.Name,
                Type = proxy.Type,
                Host = proxy.Host,
                Port = proxy.Port,
                Username = proxy.Username,
                Password = proxy.Password
            })
            .ToList();

        foreach (var proxy in newProxies)
            await AppService.SettingsService.AddProxySettingsAsync(proxy);
    }

    private async Task<List<ExportAddedDownloadQueueDataViewModel>> ImportDownloadQueuesAsync(List<ExportDownloadQueueViewModel>? exportDownloadQueues)
    {
        if (exportDownloadQueues == null || exportDownloadQueues.Count == 0)
            return [];

        // Get saved download queues in database
        var downloadQueuesInDb = AppService.DownloadQueueService.DownloadQueues.ToList();

        // We have to store added data for adding download files to download queues
        var data = new List<ExportAddedDownloadQueueDataViewModel>();
        // Add download queues
        foreach (var exportDownloadQueue in exportDownloadQueues)
        {
            var downloadQueueInDb = downloadQueuesInDb.Find(dq => dq.Title?.Equals(exportDownloadQueue.Title) == true);
            if (downloadQueueInDb != null)
            {
                var result = await DialogBoxManager.ShowWarningDialogAsync("Import data",
                    $"A queue named '{exportDownloadQueue.Title}' already exists. Do you want to create another queue with the same name?",
                    DialogButtons.YesNo);

                // Add current primary key and continue
                if (result != DialogResult.Yes)
                {
                    data.Add(new ExportAddedDownloadQueueDataViewModel(downloadQueueInDb.Id, exportDownloadQueue.Id));
                    continue;
                }

                // Stop download queue if is already running
                if (downloadQueueInDb.IsRunning)
                {
                    await AppService
                        .DownloadQueueService
                        .StopDownloadQueueAsync(downloadQueueInDb, playSound: false);
                }
            }

            var downloadQueue = new DownloadQueue
            {
                Title = exportDownloadQueue.Title,
                StartOnApplicationStartup = exportDownloadQueue.StartOnApplicationStartup,
                StartDownloadSchedule = exportDownloadQueue.StartDownloadSchedule,
                StopDownloadSchedule = exportDownloadQueue.StopDownloadSchedule,
                IsDaily = exportDownloadQueue.IsDaily,
                JustForDate = exportDownloadQueue.JustForDate,
                DaysOfWeek = exportDownloadQueue.DaysOfWeek,
                RetryOnDownloadingFailed = exportDownloadQueue.RetryOnDownloadingFailed,
                RetryCount = exportDownloadQueue.RetryCount,
                ShowAlarmWhenDone = exportDownloadQueue.ShowAlarmWhenDone,
                ExitProgramWhenDone = exportDownloadQueue.ExitProgramWhenDone,
                TurnOffComputerWhenDone = exportDownloadQueue.TurnOffComputerWhenDone,
                TurnOffComputerMode = exportDownloadQueue.TurnOffComputerMode,
                DownloadCountAtSameTime = exportDownloadQueue.DownloadCountAtSameTime,
                IncludePausedFiles = exportDownloadQueue.IncludePausedFiles
            };

            var id = await AppService.DownloadQueueService.AddNewDownloadQueueAsync(downloadQueue);
            data.Add(new ExportAddedDownloadQueueDataViewModel(id, exportDownloadQueue.Id));
        }

        return data;
    }

    private async Task<List<ExportAddedDownloadFileDataViewModel>> ImportDownloadFilesAsync(List<ExportDownloadFileViewModel>? exportDownloadFiles,
        List<ExportAddedDownloadQueueDataViewModel> addedDownloadQueues)
    {
        if (exportDownloadFiles == null || exportDownloadFiles.Count == 0)
            return [];

        // Get saved download files in database
        var downloadFilesInDb = AppService.DownloadFileService.DownloadFiles.ToList();

        // We have to store added data for adding download files to storage
        var data = new List<ExportAddedDownloadFileDataViewModel>();
        // Get all file extensions
        var fileExtensions = await AppService
            .UnitOfWork
            .CategoryFileExtensionRepository
            .GetAllAsync(includeProperties: ["Category.CategorySaveDirectory"]);

        // Get general category
        var generalCategory = await AppService
            .UnitOfWork
            .CategoryRepository
            .GetAsync(where: c => c.Title == Constants.GeneralCategoryTitle, includeProperties: ["CategorySaveDirectory"]);

        // Add download files
        foreach (var exportDownloadFile in exportDownloadFiles)
        {
            // Make sure download file has file name
            // If file name is null or empty, we must get url details and use of it
            var fileName = exportDownloadFile.FileName;
            if (fileName.IsNullOrEmpty())
            {
                var urlDetails = await AppService.DownloadFileService.GetUrlDetailsAsync(exportDownloadFile.Url);
                var urlDetailsValidation = AppService.DownloadFileService.ValidateUrlDetails(urlDetails);
                if (!urlDetailsValidation.IsValid)
                    continue;

                fileName = urlDetails.FileName;
                exportDownloadFile.Size = urlDetails.FileSize;
            }

            Category? category = null;
            var downloadFileInDb = downloadFilesInDb.Find(df => df.Url?.Equals(exportDownloadFile.Url) == true);
            if (downloadFileInDb != null)
            {
                var result = await DialogBoxManager.ShowWarningDialogAsync("Import data",
                    $"You already have a download with URL '{exportDownloadFile.Url}' in your list. \nWould you like to add it again?",
                    DialogButtons.YesNo);

                // Add current primary key and continue
                if (result != DialogResult.Yes)
                    continue;

                // Find category and save location for the file
                var extension = Path.GetExtension(fileName);
                var fileExtension = fileExtensions.Find(fe => fe.Extension.Equals(extension));

                // Check category and save location. If save location is null or empty, we must use general category for the file
                if (fileExtension?.Category?.CategorySaveDirectory?.SaveDirectory.IsNullOrEmpty() != false)
                {
                    // If general category not found, set file name empty
                    if (generalCategory?.CategorySaveDirectory?.SaveDirectory.IsNullOrEmpty() != false)
                    {
                        fileName = string.Empty;
                    }
                    else
                    {
                        var saveLocation = generalCategory.CategorySaveDirectory.SaveDirectory;
                        fileName = AppService.DownloadFileService.GetNewFileName(fileName, saveLocation);
                        category = generalCategory;
                    }
                }
                else
                {
                    var saveLocation = fileExtension.Category.CategorySaveDirectory.SaveDirectory;
                    fileName = AppService.DownloadFileService.GetNewFileName(fileName, saveLocation);
                    category = fileExtension.Category;
                }
            }

            // Make sure file name has value
            if (fileName.IsNullOrEmpty())
                continue;

            // Find category if it is null
            if (category == null)
            {
                var extension = Path.GetExtension(fileName);
                var fileExtension = fileExtensions.Find(fe => fe.Extension.Equals(extension));

                if (fileExtension?.Category != null)
                {
                    category = fileExtension.Category;
                }
                else
                {
                    if (generalCategory != null)
                    {
                        category = generalCategory;
                    }
                    else
                    {
                        continue;
                    }
                }
            }

            // Find download queue id
            int? downloadQueueId;
            if (exportDownloadFile.AddedToDefaultQueue)
            {
                var userDefaultQueue = AppService.DownloadQueueService.DownloadQueues.FirstOrDefault(dq => dq.IsDefault);
                var appDefaultQueue = AppService.DownloadQueueService.DownloadQueues.FirstOrDefault(dq => dq.Title?.Equals(Constants.DefaultDownloadQueueTitle) == true);

                downloadQueueId = userDefaultQueue?.Id ?? appDefaultQueue?.Id;
            }
            else
            {
                downloadQueueId = addedDownloadQueues
                    .Find(dq => dq.OldDownloadQueueId == exportDownloadFile.DownloadQueueId)?
                    .NewDownloadQueueId;
            }

            // Find download queue priority
            int? downloadQueuePriority = null;
            if (downloadQueueId != null)
            {
                var maxDownloadQueuePriority = AppService
                    .DownloadFileService
                    .DownloadFiles
                    .Where(df => df.DownloadQueueId == downloadQueueId)
                    .Sum(df => df.DownloadQueuePriority ?? 0) + 1;

                var sortedDownloadFiles = exportDownloadFiles
                    .Where(df => df.DownloadQueueId == exportDownloadFile.DownloadQueueId && df.DownloadQueuePriority != null)
                    .OrderBy(df => df.DownloadQueuePriority)
                    .ToList();

                if (sortedDownloadFiles.Count > 0)
                {
                    var currentIndex = sortedDownloadFiles.IndexOf(exportDownloadFile);
                    downloadQueuePriority = maxDownloadQueuePriority + currentIndex;
                }
            }

            // Create new instance of DownloadFileViewModel
            var downloadFile = new DownloadFileViewModel
            {
                Url = exportDownloadFile.Url,
                FileName = fileName,
                DownloadQueueId = downloadQueueId,
                Size = exportDownloadFile.Size,
                Description = exportDownloadFile.Description,
                Status = exportDownloadFile.Status,
                DownloadQueuePriority = downloadQueuePriority,
                DownloadProgress = exportDownloadFile.DownloadProgress,
                DownloadPackage = exportDownloadFile.DownloadPackage,
                CategoryId = category.Id
            };

            // Save download file
            downloadFile = await AppService.DownloadFileService.AddDownloadFileAsync(downloadFile);
            // Add required data to result
            var viewModel = new ExportAddedDownloadFileDataViewModel(oldFileName: exportDownloadFile.FileName,
                newFileName: downloadFile?.FileName ?? string.Empty,
                saveLocation: category.CategorySaveDirectory?.SaveDirectory ?? string.Empty,
                newDownloadFileId: downloadFile?.Id ?? 0);

            data.Add(viewModel);
        }

        return data;
    }

    private async Task ImportFilesAsync(string tempFolder, List<ExportAddedDownloadFileDataViewModel> addedDownloadFiles)
    {
        // Make sure temp folder has value and exist
        if (tempFolder.IsNullOrEmpty() || !Directory.Exists(tempFolder))
            return;

        // Get all files exists in temp folder
        var files = Directory.GetFiles(tempFolder);
        if (files.Length == 0)
            return;

        // Move files from temp folder to new save location
        foreach (var file in files)
        {
            var oldFileName = Path.GetFileName(file);
            var data = addedDownloadFiles.Find(df => df.OldFileName.Equals(oldFileName));
            if (data == null || data.NewFileName.IsNullOrEmpty() || data.SaveLocation.IsNullOrEmpty())
                continue;

            var filePath = Path.Combine(data.SaveLocation, data.NewFileName);
            if (File.Exists(filePath))
            {
                var result = await DialogBoxManager.ShowWarningDialogAsync("Import data",
                    $"The file '{data.NewFileName}' already exists. Do you want to replace it?",
                    DialogButtons.YesNo);

                if (result != DialogResult.Yes)
                {
                    await RemoveStorageDataFromDownloadFileAsync(data.NewDownloadFileId);
                    continue;
                }
            }

            var fileInfo = new FileInfo(file);
            var driveInfo = new DriveInfo(filePath);
            if (fileInfo.Length > driveInfo.AvailableFreeSpace)
            {
                var driveName = driveInfo.GetDriveName();

                await DialogBoxManager.ShowDangerDialogAsync("Import data",
                    $"There isn't enough free space on your '{driveName}' drive to transfer the files. The transfer of '{data.NewFileName}' has been canceled.",
                    DialogButtons.Ok);

                await RemoveStorageDataFromDownloadFileAsync(data.NewDownloadFileId);
                continue;
            }

            File.Move(file, filePath, true);
        }
    }

    private async Task RemoveStorageDataFromDownloadFileAsync(int downloadFileId)
    {
        var downloadFile = AppService
            .DownloadFileService
            .DownloadFiles
            .FirstOrDefault(df => df.Id == downloadFileId);

        if (downloadFile == null)
            return;

        downloadFile.Status = DownloadFileStatus.None;
        downloadFile.DownloadProgress = 0;
        downloadFile.DownloadPackage = null;

        await AppService.DownloadFileService.UpdateDownloadFileAsync(downloadFile);
    }
}