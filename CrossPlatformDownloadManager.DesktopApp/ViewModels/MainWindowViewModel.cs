using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.ViewModels.MainWindow;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.DesktopApp.Views.UserControls.MainWindow;
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
    private readonly DispatcherTimer _saveColumnsSettingsTimer;

    private Views.MainWindow? _mainWindow;
    private List<DownloadFileViewModel> _selectedDownloadFilesToAddToQueue = [];

    // Properties
    private CategoriesTreeViewModel? _categoriesTreeViewModel;
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
    private bool _showCategoriesPanel = true;
    private MainDownloadFilesDataGridColumnsSettings _dataGridColumnsSettings = new();
    private string _globalSpeedLimit = "0 KB";
    private bool _isGlobalSpeedLimitVisible;

    #endregion

    #region Properties

    public CategoriesTreeViewModel? CategoriesTreeViewModel
    {
        get => _categoriesTreeViewModel;
        set => this.RaiseAndSetIfChanged(ref _categoriesTreeViewModel, value);
    }

    public CategoriesTreeView CategoriesTreeView => new CategoriesTreeView { DataContext = CategoriesTreeViewModel };

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

    public bool IsUpdatingDownloadFiles { get; private set; }

    public bool ShowCategoriesPanel
    {
        get => _showCategoriesPanel;
        set
        {
            this.RaiseAndSetIfChanged(ref _showCategoriesPanel, value);
            _ = Dispatcher.UIThread.InvokeAsync(SaveShowCategoriesPanelOptionAsync);
        }
    }

    public MainDownloadFilesDataGridColumnsSettings DataGridColumnsSettings
    {
        get => _dataGridColumnsSettings;
        set => this.RaiseAndSetIfChanged(ref _dataGridColumnsSettings, value);
    }

    public string GlobalSpeedLimit
    {
        get => _globalSpeedLimit;
        set => this.RaiseAndSetIfChanged(ref _globalSpeedLimit, value);
    }

    public bool IsGlobalSpeedLimitVisible
    {
        get => _isGlobalSpeedLimitVisible;
        set => this.RaiseAndSetIfChanged(ref _isGlobalSpeedLimitVisible, value);
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

    public ICommand ShareCommand { get; }

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

    public ICommand ExportCdmDataMenuItemCommand { get; }

    public ICommand ExportTextDataMenuItemCommand { get; }

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

    public ICommand SaveColumnsSettingsCommand { get; }

    public ICommand OpenAboutUsWindowCommand { get; }

    public ICommand CheckForUpdatesCommand { get; }

    #endregion

    public MainWindowViewModel(IAppService appService) : base(appService)
    {
        _updateDownloadSpeedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _updateDownloadSpeedTimer.Tick += UpdateDownloadSpeedTimerOnTick;
        _updateDownloadSpeedTimer.Start();

        _updateActiveDownloadQueuesTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _updateActiveDownloadQueuesTimer.Tick += UpdateActiveDownloadQueuesTimerOnTick;
        _updateActiveDownloadQueuesTimer.Start();

        _saveColumnsSettingsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _saveColumnsSettingsTimer.Tick += SaveColumnsSettingsTimerOnTick;

        LoadCategories();
        FilterDownloadList();
        LoadDownloadQueues();

        DownloadSpeed = "0 KB";
        SelectedFilesTotalSize = "0 KB";
        ShowCategoriesPanel = AppService.SettingsService.Settings.ShowCategoriesPanel;
        DataGridColumnsSettings = AppService.SettingsService.Settings.DataGridColumnsSettings;

        CalculateGlobalSpeedLimit();

        SelectAllRowsCommand = ReactiveCommand.Create<DataGrid?>(SelectAllRows);
        AddNewLinkCommand = ReactiveCommand.CreateFromTask<Window?>(AddNewLinkAsync);
        ResumeDownloadFileCommand = ReactiveCommand.CreateFromTask<DataGrid?>(ResumeDownloadFileAsync);
        StopDownloadFileCommand = ReactiveCommand.CreateFromTask<DataGrid?>(StopDownloadFileAsync);
        StopAllDownloadFilesCommand = ReactiveCommand.CreateFromTask(StopAllDownloadFilesAsync);
        DeleteDownloadFilesCommand = ReactiveCommand.CreateFromTask<DataGrid?>(DeleteDownloadFilesAsync);
        DeleteCompletedDownloadFilesCommand = ReactiveCommand.CreateFromTask(DeleteCompletedDownloadFilesAsync);
        OpenSettingsWindowCommand = ReactiveCommand.CreateFromTask<Window?>(OpenSettingsWindowAsync);
        ShareCommand = ReactiveCommand.CreateFromTask(ShareAsync);
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
        ExportCdmDataMenuItemCommand = ReactiveCommand.CreateFromTask(ExportCdmDataMenuItemAsync);
        ExportTextDataMenuItemCommand = ReactiveCommand.CreateFromTask(ExportTextDataMenuItemAsync);
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
        SaveColumnsSettingsCommand = ReactiveCommand.CreateFromTask(SaveColumnsSettingsAsync);
        OpenAboutUsWindowCommand = ReactiveCommand.CreateFromTask<Window?>(OpenAboutUsWindowAsync);
        CheckForUpdatesCommand = ReactiveCommand.CreateFromTask<Window?>(CheckForUpdatesAsync);
    }

    private void LoadDownloadQueues()
    {
        DownloadQueues = AppService.DownloadQueueService.DownloadQueues;
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
        {
            dataGrid.SelectedIndex = -1;
        }
        else
        {
            dataGrid.SelectAll();
        }
    }

    private async Task AddNewLinkAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            var vm = new CaptureUrlWindowViewModel(AppService);
            var window = new CaptureUrlWindow { DataContext = vm };
            window.Show(owner);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to add a new link. Error message: {ErrorMessage}", ex.Message);
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
            Log.Error(ex, "An error occurred while trying to stop a download file. Error message: {ErrorMessage}", ex.Message);
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
            Log.Error(ex, "An error occurred while trying to stop all download files. Error message: {ErrorMessage}", ex.Message);
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
                .Where(df => !df.SaveLocation.IsStringNullOrEmpty() && !df.FileName.IsStringNullOrEmpty())
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
            Log.Error(ex, "An error occurred while trying to delete completed download files. Error message: {ErrorMessage}", ex.Message);
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
            Log.Error(ex, "An error occurred while trying to open settings window. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private static async Task ShareAsync()
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = Constants.CdmWebsiteUrl,
                UseShellExecute = true
            };

            Process.Start(processStartInfo);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to share. Error message: {ErrorMessage}", ex.Message);
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
            Log.Error(ex, "An error occurred while trying to start/stop a download queue. Error message: {ErrorMessage}", ex.Message);
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
            if (tag.IsStringNullOrEmpty() || !int.TryParse(tag, out var downloadQueueId))
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
            Log.Error(ex, "An error occurred while trying to show download queue details. Error message: {ErrorMessage}", ex.Message);
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
            Log.Error(ex, "An error occurred while trying to add new download queue. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private static async Task ExitProgramMenuItemAsync()
    {
        try
        {
            if (App.Desktop == null)
                return;

            var result = await DialogBoxManager.ShowInfoDialogAsync("Exit", "Are you sure you want to exit the app?", DialogButtons.YesNo);
            if (result != DialogResult.Yes)
                return;

            App.Desktop.Shutdown();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while exit the app. Error message: {ErrorMessage}", ex.Message);
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
            Log.Error(ex, "An error occurred while trying to start all downloads. Error message: {ErrorMessage}", ex.Message);
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
            Log.Error(ex, "An error occurred while trying to stop all downloads. Error message: {ErrorMessage}", ex.Message);
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
            Log.Error(ex, "An error occurred while trying to pause all downloads. Error message: {ErrorMessage}", ex.Message);
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
            Log.Error(ex, "An error occurred while trying to resume all downloads. Error message: {ErrorMessage}", ex.Message);
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
            Log.Error(ex, "An error occurred while trying to pause all downloads. Error message: {ErrorMessage}", ex.Message);
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
            Log.Error(ex, "An error occurred while trying to pause all downloads. Error message: {ErrorMessage}", ex.Message);
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

    private async Task SaveColumnsSettingsAsync()
    {
        try
        {
            _saveColumnsSettingsTimer.Stop();
            _saveColumnsSettingsTimer.Start();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while saving columns settings. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task OpenAboutUsWindowAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            var vm = new AboutUsWindowViewModel(AppService);
            var window = new AboutUsWindow { DataContext = vm };
            await window.ShowDialog(owner);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to open about us window. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    /// <summary>
    /// Checks for new versions of the application.
    /// </summary>
    /// <exception cref="InvalidOperationException">When unable to get version from server.</exception>
    public async Task CheckForUpdatesAsync(Window? owner)
    {
        try
        {
            // Get new version from server
            const string url = $"{Constants.CdmApiUrl}/version";
            using var httpClient = new HttpClient();
            using var request = await httpClient.GetAsync(url);
            request.EnsureSuccessStatusCode();

            // Convert response to AppVersion class
            var response = await request.Content.ReadAsStringAsync();
            var appVersion = response.ConvertFromJson<AppVersion?>();
            // Make sure the AppVersion exists
            if (appVersion == null)
                throw new InvalidOperationException("Unable to get version from server.");

            // Get the current version of the application
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            // Compare the versions
            if (currentVersion?.Equals($"{appVersion.Version.TrimStart('v')}.0") == true)
            {
                // If the versions are the same, show a dialog box to the user and inform them that they are using the latest version
                if (owner != null)
                {
                    await DialogBoxManager.ShowInfoDialogAsync("No Updates Available",
                        "You are using the latest version of Cross Platform Download Manager.",
                        DialogButtons.Ok);
                }

                return;
            }

            // If the versions are different, show a dialog box to the user and ask if they want to download the new version
            var result = await DialogBoxManager.ShowInfoDialogAsync("New Version Available",
                "A new version of Cross Platform Download Manager is available. Would you like to download it now?",
                DialogButtons.YesNo);

            if (result == DialogResult.No)
                return;

            // Open website to download the new version
            await ShareAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to check for updates. Error message: {ErrorMessage}", ex.Message);
        }
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
            Log.Error(ex, "An error occurred while trying to select all rows. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task OpenFileContextMenuAsync(DataGrid? dataGrid)
    {
        try
        {
            await HideContextMenuAsync();

            if (dataGrid?.SelectedItem is not DownloadFileViewModel { IsCompleted: true, IsStopping: false } downloadFile
                || downloadFile.SaveLocation.IsStringNullOrEmpty()
                || downloadFile.FileName.IsStringNullOrEmpty())
            {
                return;
            }

            var filePath = Path.Combine(downloadFile.SaveLocation!, downloadFile.FileName!);
            if (!File.Exists(filePath))
            {
                await DialogBoxManager.ShowInfoDialogAsync("Open file",
                    "The specified file could not be found. It is possible that the file has been deleted or relocated.",
                    DialogButtons.Ok);

                return;
            }

            PlatformSpecificManager.OpenFile(filePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to open the file. Error message: {ErrorMessage}", ex.Message);
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

            var groupDownloadFiles = dataGrid
                .SelectedItems
                .OfType<DownloadFileViewModel>()
                .Where(df => !df.SaveLocation.IsStringNullOrEmpty() && Directory.Exists(df.SaveLocation!))
                .GroupBy(df => df.SaveLocation!)
                .ToList();

            if (groupDownloadFiles.Count == 0)
            {
                await DialogBoxManager.ShowInfoDialogAsync("Open folder",
                    "The specified folder(s) could not be found. It is possible that the folder(s) have been deleted or relocated.",
                    DialogButtons.Ok);

                return;
            }

            var openedFolders = new List<string>();
            foreach (var group in groupDownloadFiles)
            {
                var directoryPath = group.Key;
                foreach (var downloadFile in group)
                {
                    if (downloadFile.FileName.IsStringNullOrEmpty())
                    {
                        if (openedFolders.Contains(directoryPath))
                            continue;

                        PlatformSpecificManager.OpenFolder(directoryPath);
                        openedFolders.Add(directoryPath);
                    }
                    else
                    {
                        if (openedFolders.Contains(directoryPath))
                            continue;

                        var filePath = Path.Combine(directoryPath, downloadFile.FileName!);
                        if (File.Exists(filePath))
                        {
                            PlatformSpecificManager.OpenContainingFolderAndSelectFile(filePath);
                        }
                        else
                        {
                            PlatformSpecificManager.OpenFolder(directoryPath);
                        }

                        openedFolders.Add(directoryPath);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to open the folder. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task RenameContextMenuAsync(DataGrid? dataGrid)
    {
        try
        {
            await HideContextMenuAsync();

            if (dataGrid?.SelectedItem is not DownloadFileViewModel { IsCompleted: true } downloadFile ||
                downloadFile.FileName.IsStringNullOrEmpty() ||
                downloadFile.SaveLocation.IsStringNullOrEmpty())
            {
                return;
            }

            var filePath = Path.Combine(downloadFile.SaveLocation!, downloadFile.FileName!);
            if (!File.Exists(filePath))
            {
                await DialogBoxManager.ShowInfoDialogAsync("Rename", "File not found.", DialogButtons.Ok);
                return;
            }

            var vm = new ChangeFileNameWindowViewModel(AppService, downloadFile);
            var window = new ChangeFileNameWindow { DataContext = vm };
            window.Show();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to rename the file. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task ChangeFolderContextMenuAsync(DataGrid? dataGrid)
    {
        try
        {
            await HideContextMenuAsync();

            if (dataGrid?.SelectedItem is not DownloadFileViewModel { IsCompleted: true } downloadFile ||
                downloadFile.FileName.IsStringNullOrEmpty() ||
                downloadFile.SaveLocation.IsStringNullOrEmpty() ||
                _mainWindow == null)
            {
                return;
            }

            var filePath = Path.Combine(downloadFile.SaveLocation!, downloadFile.FileName!);
            if (!File.Exists(filePath))
            {
                await DialogBoxManager.ShowInfoDialogAsync("Change folder", "File not found.", DialogButtons.Ok);
                return;
            }

            var newSaveLocation = await _mainWindow.ChangeSaveLocationAsync(downloadFile.SaveLocation!);
            if (newSaveLocation.IsStringNullOrEmpty())
            {
                await DialogBoxManager.ShowInfoDialogAsync("Change folder", "Folder not found.", DialogButtons.Ok);
                return;
            }

            var newFilePath = Path.Combine(newSaveLocation!, downloadFile.FileName!);
            if (File.Exists(newFilePath))
            {
                await DialogBoxManager.ShowInfoDialogAsync("Change folder", "File already exists.", DialogButtons.Ok);
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
            Log.Error(ex, "An error occurred while trying to change the folder. Error message: {ErrorMessage}", ex.Message);
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
            Log.Error(ex, "An error occurred while trying to redownload the file. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task ResumeContextMenuAsync(DataGrid? dataGrid)
    {
        await ResumeDownloadFileOperationAsync(dataGrid);
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
            Log.Error(ex, "An error occurred while trying to refresh the download address. Error message: {ErrorMessage}", ex.Message);
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
            AddToQueueDownloadQueues.Clear();
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
            Log.Error(ex, "An error occurred while trying to add to queue. Error message: {ErrorMessage}", ex.Message);
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
            Log.Error(ex, "An error occurred while trying to remove from queue. Error message: {ErrorMessage}", ex.Message);
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
            Log.Error(ex, "An error occurred while trying to add to queue. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task AddNewLinkMenuItemAsync(Window? owner)
    {
        await AddNewLinkAsync(owner);
    }

    private async Task ExportCdmDataMenuItemAsync()
    {
        await AppService.ExportImportService.ExportDataAsync(exportAsCdmFile: true);
    }

    private async Task ExportTextDataMenuItemAsync()
    {
        await AppService.ExportImportService.ExportDataAsync(exportAsCdmFile: false);
    }

    private async Task ImportDataMenuItemAsync()
    {
    }

    private async Task ExportSettingsMenuItemAsync()
    {
        await AppService.ExportImportService.ExportSettingsAsync();
    }

    private async Task ImportSettingsMenuItemAsync()
    {
        await AppService.ExportImportService.ImportSettingsAsync();
    }

    private void UpdateDownloadSpeedTimerOnTick(object? sender, EventArgs e)
    {
        DownloadSpeed = AppService
            .DownloadFileService
            .GetDownloadSpeed();
    }

    private void UpdateActiveDownloadQueuesTimerOnTick(object? sender, EventArgs e)
    {
        // Get running queues
        var downloadQueues = AppService
            .DownloadQueueService
            .DownloadQueues
            .Where(dq => dq.IsRunning)
            .ToObservableCollection();

        // Check for any changes
        bool isChanged;
        // Check if count is different
        if (ActiveDownloadQueues.Count != downloadQueues.Count)
        {
            isChanged = true;
        }
        // Or check if any items are different
        else
        {
            isChanged = downloadQueues
                .Where((t, i) => t.Id != ActiveDownloadQueues[i].Id)
                .Any();
        }

        // Check if changed
        if (!isChanged)
            return;

        // Update active download queues
        ActiveDownloadQueues = downloadQueues;
    }

    private void LoadCategories()
    {
        if (CategoriesTreeViewModel != null)
            CategoriesTreeViewModel.SelectedItemChanged -= CategoriesTreeViewModelOnSelectedItemChanged;

        CategoriesTreeViewModel = new CategoriesTreeViewModel(AppService);
        CategoriesTreeViewModel.SelectedItemChanged += CategoriesTreeViewModelOnSelectedItemChanged;
    }

    private void CategoriesTreeViewModelOnSelectedItemChanged(object? sender, EventArgs e)
    {
        FilterDownloadList();
    }

    private void FilterDownloadList()
    {
        try
        {
            if (IsUpdatingDownloadFiles)
                return;

            IsUpdatingDownloadFiles = true;

            var downloadFiles = AppService
                .DownloadFileService
                .DownloadFiles
                .ToList();

            if (CategoriesTreeViewModel?.SelectedCategoriesTreeItemViewModel != null)
            {
                downloadFiles = CategoriesTreeViewModel.SelectedCategoriesTreeItemViewModel.Title switch
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

            if (CategoriesTreeViewModel?.SelectedCategoriesTreeItemViewModel?.SelectedCategory != null)
            {
                downloadFiles = downloadFiles
                    .Where(df => df.CategoryId == CategoriesTreeViewModel.SelectedCategoriesTreeItemViewModel.SelectedCategory.Id)
                    .ToList();
            }

            if (!SearchText.IsStringNullOrEmpty())
            {
                downloadFiles = downloadFiles
                    .Where(df => df.Url?.Contains(SearchText!, StringComparison.OrdinalIgnoreCase) != false ||
                                 df.FileName?.Contains(SearchText!, StringComparison.OrdinalIgnoreCase) != false ||
                                 df.SaveLocation?.Contains(SearchText!, StringComparison.OrdinalIgnoreCase) != false)
                    .ToList();
            }

            DownloadFiles = downloadFiles.ToObservableCollection();
        }
        catch
        {
            try
            {
                DownloadFiles = AppService.DownloadFileService.DownloadFiles;
            }
            catch
            {
                DownloadFiles = [];
            }
        }
        finally
        {
            IsUpdatingDownloadFiles = false;
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

    protected override void OnSettingsServiceDataChanged()
    {
        DataGridColumnsSettings = AppService.SettingsService.Settings.DataGridColumnsSettings;
        CalculateGlobalSpeedLimit();
        FilterDownloadList();
    }

    private void CalculateGlobalSpeedLimit()
    {
        var settings = AppService.SettingsService.Settings;
        var limitSpeed = settings.LimitSpeed ?? 0;
        var limitUnit = settings.LimitUnit;

        if (limitSpeed <= 0 || limitUnit.IsStringNullOrEmpty())
        {
            IsGlobalSpeedLimitVisible = false;
            return;
        }

        IsGlobalSpeedLimitVisible = true;
        GlobalSpeedLimit = $"{limitSpeed} {limitUnit}";
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

    public async Task ChangeContextFlyoutEnableStateAsync(Views.MainWindow? mainWindow)
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
            Log.Error(ex, "An error occurred while trying to change context flyout enable state. Error message: {ErrorMessage}", ex.Message);
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
            Log.Error(ex, "An error occurred while trying to hide context menu. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task ResumeDownloadFileOperationAsync(DataGrid? dataGrid)
    {
        try
        {
            if (dataGrid == null || dataGrid.SelectedItems.Count == 0)
                return;

            var downloadFiles = dataGrid
                .SelectedItems
                .OfType<DownloadFileViewModel>()
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
            Log.Error(ex, "An error occurred while trying to resume a download file. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    public async Task RemoveDownloadFilesAsync(DataGrid? dataGrid, bool excludeFilesInRunningQueues)
    {
        try
        {
            // Make sure data grid is not null and has selected items
            if (dataGrid == null || dataGrid.SelectedItems.Count == 0)
                return;

            // Convert data grid selected items to download files
            var downloadFiles = dataGrid
                .SelectedItems
                .OfType<DownloadFileViewModel>()
                .ToList();

            // Check if we should exclude files in running queues
            if (excludeFilesInRunningQueues)
            {
                // Get download queues
                var downloadQueues = AppService
                    .DownloadQueueService
                    .DownloadQueues;

                // Remove download files that are in running queues
                downloadFiles = downloadFiles
                    .Where(df => df.DownloadQueueId == null || downloadQueues.FirstOrDefault(dq => dq.Id == df.DownloadQueueId)?.IsRunning != true)
                    .ToList();
            }

            // Check if there are any download files
            if (downloadFiles.Count == 0)
                return;

            // Check if there are any existing download files in the system
            var existingDownloadFiles = downloadFiles
                .Where(df => !df.SaveLocation.IsStringNullOrEmpty() && !df.FileName.IsStringNullOrEmpty())
                .Select(df => Path.Combine(df.SaveLocation!, df.FileName!))
                .Where(File.Exists)
                .ToList();

            // Ask user if they want to remove the files from the system
            var deleteFile = false;
            if (existingDownloadFiles.Count > 0)
            {
                var result = await DialogBoxManager.ShowWarningDialogAsync("Delete files",
                    $"Would you like to remove the downloaded file{(existingDownloadFiles.Count > 1 ? "s" : "")} from your system as well?",
                    DialogButtons.YesNo);

                deleteFile = result == DialogResult.Yes;
            }

            // Get all download files that are currently downloading
            var stopDownloadTasks = downloadFiles
                .Where(df => df.IsDownloading)
                .Select(df => AppService.DownloadFileService.StopDownloadFileAsync(df, ensureStopped: true))
                .ToList();

            // Wait for all download files to stop
            if (stopDownloadTasks.Count > 0)
                await Task.WhenAll(stopDownloadTasks);

            // Remove download files
            var reloadData = downloadFiles.Count == 1;
            for (var i = downloadFiles.Count - 1; i >= 0; i--)
            {
                await AppService
                    .DownloadFileService
                    .DeleteDownloadFileAsync(downloadFiles[i], deleteFile, reloadData);
            }

            // Reload download files when count is greater than 1
            if (!reloadData)
            {
                await AppService
                    .DownloadFileService
                    .LoadDownloadFilesAsync();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to remove download files. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task SaveShowCategoriesPanelOptionAsync()
    {
        try
        {
            var showCategoriesPanel = AppService.SettingsService.Settings.ShowCategoriesPanel;
            if (showCategoriesPanel == ShowCategoriesPanel)
                return;

            AppService.SettingsService.Settings.ShowCategoriesPanel = ShowCategoriesPanel;
            await AppService.SettingsService.SaveSettingsAsync(AppService.SettingsService.Settings);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to save settings. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async void SaveColumnsSettingsTimerOnTick(object? sender, EventArgs e)
    {
        try
        {
            _saveColumnsSettingsTimer.Stop();

            var settings = AppService.SettingsService.Settings;
            settings.DataGridColumnsSettings = DataGridColumnsSettings;
            await AppService.SettingsService.SaveSettingsAsync(settings);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while saving columns settings. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    protected override void OnCategoryServiceCategoriesChanged()
    {
        base.OnCategoryServiceCategoriesChanged();
        LoadCategories();
    }

    public async Task DataGridRowDoubleTapActionAsync(DownloadFileViewModel? downloadFile)
    {
        try
        {
            if (downloadFile == null || downloadFile.IsStopping)
                return;

            switch (downloadFile.Status)
            {
                case DownloadFileStatus.None:
                {
                    _ = AppService.DownloadFileService.StartDownloadFileAsync(downloadFile);
                    break;
                }

                case DownloadFileStatus.Downloading:
                case DownloadFileStatus.Paused:
                {
                    AppService.DownloadFileService.ShowOrFocusDownloadWindow(downloadFile);
                    break;
                }

                default:
                {
                    if (downloadFile.SaveLocation.IsStringNullOrEmpty() || downloadFile.FileName.IsStringNullOrEmpty())
                        break;

                    var filePath = Path.Combine(downloadFile.SaveLocation!, downloadFile.FileName!);
                    if (!File.Exists(filePath) && !Directory.Exists(downloadFile.SaveLocation))
                    {
                        await DialogBoxManager.ShowInfoDialogAsync("Unable to Locate File or Folder",
                            "We were unable to locate the file or folder you attempted to open. It is possible that the file or folder has been deleted.",
                            DialogButtons.Ok);

                        break;
                    }

                    if (File.Exists(filePath))
                    {
                        if (downloadFile.IsCompleted)
                        {
                            PlatformSpecificManager.OpenFile(filePath);
                        }
                        else
                        {
                            PlatformSpecificManager.OpenContainingFolderAndSelectFile(filePath);
                        }
                    }
                    else
                    {
                        PlatformSpecificManager.OpenFolder(downloadFile.SaveLocation!);
                    }

                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to handle double click event on data grid. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }
}