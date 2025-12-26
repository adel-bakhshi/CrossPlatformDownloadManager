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
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Models;
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
    private readonly Debouncer _saveColumnsSettingsDebouncer;
    private readonly Debouncer _loadDownloadFilesDebouncer;

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
    private MainGridColumnSettings _dataGridColumnSettings = new();
    private string _globalSpeedLimit = "0 KB";
    private bool _isGlobalSpeedLimitVisible;
    private string? _activeProxyTitle;
    private ObservableCollection<DownloadFilesDataGridShortcut> _downloadFilesDataGridShortcuts = [];

    #endregion

    #region Properties

    public CategoriesTreeViewModel? CategoriesTreeViewModel
    {
        get => _categoriesTreeViewModel;
        set
        {
            this.RaiseAndSetIfChanged(ref _categoriesTreeViewModel, value);
            this.RaisePropertyChanged(nameof(CategoriesTreeView));
        }
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

    public bool ShowCategoriesPanel
    {
        get => _showCategoriesPanel;
        set
        {
            this.RaiseAndSetIfChanged(ref _showCategoriesPanel, value);
            _ = Dispatcher.UIThread.InvokeAsync(SaveShowCategoriesPanelOptionAsync);
        }
    }

    public MainGridColumnSettings DataGridColumnSettings
    {
        get => _dataGridColumnSettings;
        private set => this.RaiseAndSetIfChanged(ref _dataGridColumnSettings, value);
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

    public string? ActiveProxyTitle
    {
        get => _activeProxyTitle;
        set => this.RaiseAndSetIfChanged(ref _activeProxyTitle, value);
    }

    public ObservableCollection<DownloadFilesDataGridShortcut> DownloadFilesDataGridShortcuts
    {
        get => _downloadFilesDataGridShortcuts;
        set => this.RaiseAndSetIfChanged(ref _downloadFilesDataGridShortcuts, value);
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

    public ICommand OpenPropertiesCommand { get; }

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

    public ICommand DownloadBrowserExtensionCommand { get; }

    public ICommand HowToInstallBrowserExtensionCommand { get; }

    #endregion

    public MainWindowViewModel(IAppService appService) : base(appService)
    {
        _updateDownloadSpeedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _updateDownloadSpeedTimer.Tick += UpdateDownloadSpeedTimerOnTick;
        _updateDownloadSpeedTimer.Start();

        _updateActiveDownloadQueuesTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _updateActiveDownloadQueuesTimer.Tick += UpdateActiveDownloadQueuesTimerOnTick;
        _updateActiveDownloadQueuesTimer.Start();

        _saveColumnsSettingsDebouncer = new Debouncer(TimeSpan.FromSeconds(5));
        _loadDownloadFilesDebouncer = new Debouncer(TimeSpan.FromSeconds(0.5));

        LoadCategories();
        FilterDownloadList();
        LoadDownloadQueues();

        DownloadSpeed = "0 KB";
        SelectedFilesTotalSize = "0 KB";
        ShowCategoriesPanel = AppService.SettingsService.Settings.ShowCategoriesPanel;
        DataGridColumnSettings = AppService.SettingsService.Settings.DataGridColumnSettings;

        CalculateGlobalSpeedLimit();
        UpdateActiveProxy();

        LoadDownloadFilesDataGridShortcuts();

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
        OpenPropertiesCommand = ReactiveCommand.CreateFromTask<DataGrid?>(OpenPropertiesCommandAsync);
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
        DownloadBrowserExtensionCommand = ReactiveCommand.CreateFromTask<Window?>(DownloadBrowserExtensionAsync);
        HowToInstallBrowserExtensionCommand = ReactiveCommand.CreateFromTask(HowToInstallBrowserExtensionAsync);
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

    /// <summary>
    /// Asynchronously deletes all completed download files from the application.
    /// This method checks for existing files, prompts the user for confirmation,
    /// and then proceeds with deletion based on user's choice.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    private async Task DeleteCompletedDownloadFilesAsync()
    {
        try
        {
            // Get all download files that have been marked as completed
            var downloadFiles = AppService
                .DownloadFileService
                .DownloadFiles
                .Where(df => df.IsCompleted)
                .ToList();

            // Filter files that have valid save locations and filenames, and actually exist on disk
            var existingDownloadFiles = downloadFiles
                .Where(df => !df.SaveLocation.IsStringNullOrEmpty() && !df.FileName.IsStringNullOrEmpty())
                .Select(df => Path.Combine(df.SaveLocation!, df.FileName!))
                .Where(File.Exists)
                .ToList();

            // Show warning dialog to confirm deletion of files
            var result = await DialogBoxManager.ShowWarningDialogAsync("Delete files",
                $"Would you like to remove the downloaded file{(existingDownloadFiles.Count > 1 ? "s" : "")} from your system as well?",
                DialogButtons.YesNoCancel);

            // Return if user cancels the operation
            if (result == DialogResult.Cancel)
                return;

            // Determine whether to actually delete files (Yes) or just remove from app (No)
            var deleteFile = result == DialogResult.Yes;

            // Process each download file
            foreach (var downloadFile in downloadFiles)
            {
                await AppService
                    .DownloadFileService
                    .DeleteDownloadFileAsync(downloadFile, deleteFile);
            }
        }
        catch (Exception ex)
        {
            // Log any errors that occur during the process
            Log.Error(ex, "An error occurred while trying to delete completed download files. Error message: {ErrorMessage}", ex.Message);
            // Show error dialog to inform user about the failure
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
            // Run debouncer to save column settings
            await _saveColumnsSettingsDebouncer.RunAsync(SaveColumnsSettingsDataAsync);
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
            // Make sure the AppVersion exists and has value
            if (appVersion?.Version.IsStringNullOrEmpty() != false)
                throw new InvalidOperationException("Unable to get version from server.");

            // Get the current version of the application
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            // Make sure current version is not null
            if (currentVersion.IsStringNullOrEmpty())
                throw new InvalidOperationException("Unable to get current version of the application.");

            // Change the format of the versions
            appVersion.Version = appVersion.Version.Replace("v", "");

            // Compare versions and if current version is greater than or equal to the latest version,
            // Show a dialog box to the user and inform them that they are using the latest version
            if (CompareVersions(currentVersion!, appVersion.Version) >= 0)
            {
                // If owner is not null, this means that the user want to check for updates from the menu.
                // So, we have to notify the user that they are using the latest version.
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

    /// <summary>
    /// Compares two version strings.
    /// </summary>
    /// <param name="versionA">The first version string.</param>
    /// <param name="versionB">The second version string.</param>
    /// <returns>A negative number if versionA is less than versionB, a positive number if versionA is greater than versionB, and 0 if they are equal.</returns>
    private static int CompareVersions(string versionA, string versionB)
    {
        // Split the version strings into parts
        var partsA = versionA.Split('.').Select(int.Parse).ToArray();
        var partsB = versionB.Split('.').Select(int.Parse).ToArray();

        // Get the maximum length of the two arrays
        var maxLength = Math.Max(partsA.Length, partsB.Length);

        // Compare each part of the version strings
        for (var i = 0; i < maxLength; i++)
        {
            var partA = i < partsA.Length ? partsA[i] : 0;
            var partB = i < partsB.Length ? partsB[i] : 0;

            if (partA > partB)
                return 1;

            if (partA < partB)
                return -1;
        }

        return 0;
    }

    /// <summary>
    /// Downloads the latest version of the browser extension.
    /// </summary>
    /// <param name="owner">The owner window of the current command.</param>
    private async Task DownloadBrowserExtensionAsync(Window? owner)
    {
        try
        {
            // Make sure the owner and the clipboard is not null
            if (owner?.Clipboard == null)
                return;

            // Copy the latest download URL of the browser extension to the clipboard
            await owner.Clipboard.SetTextAsync(Constants.LastestDownloadUrlOfBrowserExtension);
            // Add the copied URL to the download list
            await AddNewLinkAsync(owner);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to download browser extension. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    /// <summary>
    /// Guides the user to the browser-extension page of the CDM website.
    /// </summary>
    private static async Task HowToInstallBrowserExtensionAsync()
    {
        try
        {
            // Open the browser-extension page of the CDM website
            var processStartInfo = new ProcessStartInfo
            {
                FileName = Constants.CdmBrowserExtensionUrl,
                UseShellExecute = true
            };

            Process.Start(processStartInfo);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to show how to install browser extension. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
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

            PlatformSpecificManager.Current.OpenFile(filePath);
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

                        PlatformSpecificManager.Current.OpenFolder(directoryPath);
                        openedFolders.Add(directoryPath);
                    }
                    else
                    {
                        if (openedFolders.Contains(directoryPath))
                            continue;

                        var filePath = Path.Combine(directoryPath, downloadFile.FileName!);
                        if (File.Exists(filePath))
                        {
                            PlatformSpecificManager.Current.OpenContainingFolderAndSelectFile(filePath);
                        }
                        else
                        {
                            PlatformSpecificManager.Current.OpenFolder(directoryPath);
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
                _mainWindow?.StorageProvider == null)
            {
                return;
            }

            // Check if file exists
            var filePath = downloadFile.GetFilePath();
            if (!File.Exists(filePath))
            {
                await DialogBoxManager.ShowInfoDialogAsync("Change folder", "File not found.", DialogButtons.Ok);
                return;
            }

            // Get storage provider
            var storageProvider = _mainWindow!.StorageProvider;
            // Create folder picker options
            var options = new FolderPickerOpenOptions
            {
                Title = "Select Directory",
                AllowMultiple = false,
                SuggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(downloadFile.SaveLocation!)
            };

            // Open folder picker window and let user choose a folder
            var directories = await storageProvider.OpenFolderPickerAsync(options);
            // Check if user canceled the operation
            if (!directories.Any())
                return;

            // Use first path as new path
            var newSaveLocation = directories[0].Path.IsAbsoluteUri ? directories[0].Path.AbsolutePath : directories[0].Path.OriginalString;
            // Create new file path
            var newFilePath = Path.Combine(newSaveLocation, downloadFile.FileName!);
            // Check if new file path already exists
            if (File.Exists(newFilePath))
            {
                await DialogBoxManager.ShowInfoDialogAsync("Change folder", "File already exists.", DialogButtons.Ok);
                return;
            }

            // Move file to new location
            await filePath.MoveFileAsync(newFilePath);

            // Update download file save location
            downloadFile.SaveLocation = newSaveLocation;
            await AppService.DownloadFileService.UpdateDownloadFileAsync(downloadFile);
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
            if (dataGrid == null ||
                dataGrid.SelectedItems.Count == 0 ||
                AppService.DownloadQueueService.DownloadQueues.Count == 0 ||
                _selectedDownloadFilesToAddToQueue.Count == 0)
            {
                await HideContextMenuAsync();
                return;
            }

            if (AddToQueueDownloadQueues.Count == 0)
                AddToQueueFlyout?.Hide();
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

    /// <summary>
    /// Asynchronously opens the properties window for the selected download file.
    /// </summary>
    /// <param name="dataGrid">The DataGrid control containing the download files.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    private async Task OpenPropertiesCommandAsync(DataGrid? dataGrid)
    {
        try
        {
            // Hide the context menu before opening properties window
            await HideContextMenuAsync();

            // Return if dataGrid is null, no items are selected, or main window is not available
            if (dataGrid == null || dataGrid.SelectedItems.Count == 0 || _mainWindow == null)
                return;

            // Get the first selected item as a DownloadFileViewModel
            var downloadFile = dataGrid
                .SelectedItems
                .OfType<DownloadFileViewModel>()
                .FirstOrDefault();

            // Return if no valid download file is selected
            if (downloadFile == null)
                return;

            // Open properties window for the selected download file
            await ShowDownloadDetailsAsync(downloadFile, _mainWindow);
        }
        catch (Exception ex)
        {
            // Log the error and show error dialog to user
            Log.Error(ex, "An error occurred while trying to open properties. Error message: {ErrorMessage}", ex.Message);
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
        await AppService.ExportImportService.ImportDataAsync();
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
        _loadDownloadFilesDebouncer.Run(() =>
        {
            try
            {
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
        });
    }

    protected override void OnDownloadFileServiceDataChanged()
    {
        FilterDownloadList();
    }

    private void LoadDownloadQueues()
    {
        DownloadQueues = AppService.DownloadQueueService.DownloadQueues;
    }

    protected override void OnDownloadQueueServiceDataChanged()
    {
        LoadDownloadQueues();
    }

    protected override void OnSettingsServiceDataChanged()
    {
        DataGridColumnSettings = AppService.SettingsService.Settings.DataGridColumnSettings;
        CalculateGlobalSpeedLimit();
        FilterDownloadList();
        UpdateActiveProxy();
    }

    private void UpdateActiveProxy()
    {
        var proxyMode = AppService
            .SettingsService
            .Settings
            .ProxyMode;

        switch (proxyMode)
        {
            case ProxyMode.UseSystemProxySettings:
            {
                ActiveProxyTitle = "System proxy";
                break;
            }

            case ProxyMode.UseCustomProxy:
            {
                var activeProxy = AppService.SettingsService.Settings.Proxies.FirstOrDefault(p => p.IsActive);
                if (activeProxy != null)
                    ActiveProxyTitle = activeProxy.Name;

                break;
            }

            case ProxyMode.DisableProxy:
            default:
            {
                ActiveProxyTitle = null;
                break;
            }
        }
    }

    private void LoadDownloadFilesDataGridShortcuts()
    {
        DownloadFilesDataGridShortcuts =
        [
            new DownloadFilesDataGridShortcut { Title = "Copy", Shortcut = "Ctrl + Alt + C", IsEven = true },
            new DownloadFilesDataGridShortcut { Title = "Delete", Shortcut = "Delete", IsEven = false },
            new DownloadFilesDataGridShortcut { Title = "Open file", Shortcut = "Double click", IsEven = true }
        ];
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

            ContextFlyoutEnableState.CanOpenProperties = downloadFiles.Count > 0;

            LoadAvailableQueuesForContextMenu(dataGrid);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to change context flyout enable state. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private void LoadAvailableQueuesForContextMenu(DataGrid? dataGrid)
    {
        // Clear previous data stored in AddToQueueDownloadQueues
        AddToQueueDownloadQueues = [];
        _selectedDownloadFilesToAddToQueue = [];

        // If no download files are selected then return
        if (dataGrid == null || dataGrid.SelectedItems.Count == 0)
            return;

        // Get download queues
        var downloadQueues = AppService.DownloadQueueService.DownloadQueues;
        if (downloadQueues.Count == 0)
            return;

        // Get selected download files that are not completed
        var downloadFiles = dataGrid
            .SelectedItems
            .OfType<DownloadFileViewModel>()
            .Where(df => df is { IsCompleted: false, IsStopping: false })
            .ToList();

        switch (downloadFiles.Count)
        {
            // If count of selected download files is equal to 1 then show all download queues except the one that currently in use
            case 1:
            {
                var downloadQueue = downloadQueues.FirstOrDefault(dq => dq.Id == downloadFiles[0].DownloadQueueId);
                AddToQueueDownloadQueues = downloadQueue != null
                    ? downloadQueues.Where(dq => dq.Id != downloadQueue.Id).ToObservableCollection()
                    : downloadQueues;

                break;
            }

            default:
            {
                // Get primary keys of download queues from the download files
                var primaryKeys = downloadFiles
                    .Where(df => df is { DownloadQueueId: not null })
                    .Select(df => df.DownloadQueueId!.Value)
                    .Distinct()
                    .ToList();

                AddToQueueDownloadQueues = primaryKeys.Count switch
                {
                    // If count of primary keys is equal to 1 then show all download queues except the one with primary key
                    1 => downloadQueues.Where(dq => !primaryKeys.Contains(dq.Id)).ToObservableCollection(),
                    _ => downloadQueues
                };

                break;
            }
        }

        _selectedDownloadFilesToAddToQueue = downloadFiles;
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
            var result = await DialogBoxManager.ShowWarningDialogAsync("Delete files",
                $"Would you like to remove the downloaded file{(existingDownloadFiles.Count > 1 ? "s" : "")} from your system as well?",
                DialogButtons.YesNoCancel);

            // Cancel delete operation if user pressed cancel
            if (result == DialogResult.Cancel)
                return;

            // Set delete file flag to true if user pressed yes
            var deleteFile = result == DialogResult.Yes;

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

    private async Task SaveColumnsSettingsDataAsync()
    {
        try
        {
            var settings = AppService.SettingsService.Settings;
            settings.DataGridColumnSettings = DataGridColumnSettings;
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

    /// <summary>
    /// Handles the double tap action on a data grid row for a download file.
    /// </summary>
    /// <param name="downloadFile">The download file item that was double-tapped.</param>
    /// <param name="owner">The window that owns the details dialog to be opened.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public async Task DataGridRowDoubleTapActionAsync(DownloadFileViewModel? downloadFile, Window? owner)
    {
        try
        {
            if (downloadFile == null || owner == null)
                return;

            switch (downloadFile.Status)
            {
                case DownloadFileStatus.Downloading:
                case DownloadFileStatus.Paused:
                {
                    AppService.DownloadFileService.ShowOrFocusDownloadWindow(downloadFile);
                    break;
                }

                case DownloadFileStatus.Completed:
                {
                    // Get download file path
                    var filePath = downloadFile.GetFilePath() ?? string.Empty;
                    // Check if download is completed and file exists, then open it
                    if (File.Exists(filePath))
                        PlatformSpecificManager.Current.OpenFile(filePath);

                    break;
                }

                default:
                {
                    await ShowDownloadDetailsAsync(downloadFile, owner);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            // Log the error with detailed information
            Log.Error(ex, "An error occurred while trying to handle double click event on data grid. Error message: {ErrorMessage}", ex.Message);
            // Show an error dialog to the user
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    /// <summary>
    /// Shows the download details window.
    /// </summary>
    /// <param name="downloadFile">The download file to show details for.</param>
    /// <param name="owner">The owner window of the download details window.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ShowDownloadDetailsAsync(DownloadFileViewModel? downloadFile, Window? owner)
    {
        try
        {
            if (downloadFile == null || owner == null)
                return;

            // Create a new download details window and show it
            var viewModel = new DownloadDetailsWindowViewModel(AppService, downloadFile);
            var window = new DownloadDetailsWindow { DataContext = viewModel };
            await window.ShowDialog(owner);
        }
        catch (Exception ex)
        {
            // Log error
            Log.Error(ex, "An error occurred while trying to open download details window. Error message: {ErrorMessage}", ex.Message);
            // Show error dialog
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }
}