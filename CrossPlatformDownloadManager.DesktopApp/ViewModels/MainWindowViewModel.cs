using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using ReactiveUI;

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

    public ICommand ExitProgramCommand { get; }

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

        SelectAllRowsCommand = ReactiveCommand.CreateFromTask<DataGrid?>(SelectAllRowsAsync);
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
        ExitProgramCommand = ReactiveCommand.CreateFromTask(ExitProgramAsync);
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
    }

    private void LoadDownloadQueues()
    {
        var downloadQueues = AppService
            .DownloadQueueService
            .DownloadQueues;

        DownloadQueues.UpdateCollection(downloadQueues, dq => dq.Id);
    }

    private async Task SelectAllRowsAsync(DataGrid? dataGrid)
    {
        try
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
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
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
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task ResumeDownloadFileAsync(DataGrid? dataGrid)
    {
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
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
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

            var isFileExists = downloadFiles
                .Where(df => !df.SaveLocation.IsNullOrEmpty() && !df.FileName.IsNullOrEmpty())
                .Select(df => Path.Combine(df.SaveLocation!, df.FileName!))
                .Where(File.Exists)
                .ToList();

            var deleteFile = false;
            if (isFileExists.Count > 0)
            {
                var result = await DialogBoxManager.ShowWarningDialogAsync("Delete files",
                    $"Do you want to delete file{(isFileExists.Count == 1 ? "" : "s")}?",
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

            var vm = new AddEditQueueWindowViewModel(AppService) { IsEditMode = true, DownloadQueue = downloadQueue };
            var window = new AddEditQueueWindow { DataContext = vm };
            await window.ShowDialog(owner);
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task AddNewDownloadQueueAsync(Window? owner)
    {
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
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private static async Task ExitProgramAsync()
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
                .Where(df => df is { IsDownloading: false, IsPaused: false, DownloadProgress: > 0 })
                .ToList();

            if (downloadFiles.Count == 0)
                return;

            foreach (var downloadFile in downloadFiles)
            {
                await AppService
                    .DownloadFileService
                    .RedownloadDownloadFileAsync(downloadFile);

                var vm = new DownloadWindowViewModel(AppService, downloadFile);
                var window = new DownloadWindow { DataContext = vm };
                window.Show();
            }
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task ResumeContextMenuAsync(DataGrid? dataGrid)
    {
        try
        {
            if (dataGrid == null || dataGrid.SelectedItems.Count == 0)
                return;

            await ResumeDownloadFileAsync(dataGrid);
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task StopContextMenuAsync(DataGrid? dataGrid)
    {
        try
        {
            if (dataGrid == null || dataGrid.SelectedItems.Count == 0)
                return;

            var downloadFiles = dataGrid
                .SelectedItems
                .OfType<DownloadFileViewModel>()
                .Where(df => df.IsDownloading)
                .ToList();

            if (downloadFiles.Count == 0)
                return;

            var cantResumeDownloadFiles = downloadFiles
                .Where(df => df.CanResumeDownload == false)
                .ToList();

            var undefinedResumeDownloadFiles = downloadFiles
                .Where(df => df.CanResumeDownload == null)
                .ToList();

            if (cantResumeDownloadFiles.Count > 0 || undefinedResumeDownloadFiles.Count > 0)
            {
                var result = await DialogBoxManager.ShowInfoDialogAsync("Stop download",
                    "Some files cannot continue downloading, or their status is uncertain. Do you still want to stop them?",
                    DialogButtons.YesNo);

                if (result == DialogResult.No)
                {
                    var removeDownloadFiles = cantResumeDownloadFiles
                        .Union(undefinedResumeDownloadFiles)
                        .ToList();

                    foreach (var downloadFile in removeDownloadFiles)
                        dataGrid.SelectedItems.Remove(downloadFile);
                }
            }

            await StopDownloadFileAsync(dataGrid);
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task RefreshDownloadAddressContextMenuAsync(DataGrid? dataGrid)
    {
        try
        {
            if (dataGrid?.SelectedItem is not DownloadFileViewModel
                {
                    IsDownloading: false,
                    IsCompleted: false
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
                .Where(df => !df.IsCompleted)
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
                                await HideContextMenuAsync();

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
                        IsCompleted: false
                    } &&
                    downloadQueues.FirstOrDefault(dq =>
                        dq.Id == df.DownloadQueueId) != null)
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

            ContextFlyoutEnableState.CanSelectAllRows =
                DownloadFiles.Count > 0 && downloadFiles.Count != DownloadFiles.Count;

            ContextFlyoutEnableState.CanOpenFile = downloadFiles is [{ IsCompleted: true }];
            ContextFlyoutEnableState.CanOpenFolder = downloadFiles.Count > 0;
            ContextFlyoutEnableState.CanRename = downloadFiles is [{ IsCompleted: true }];
            ContextFlyoutEnableState.CanChangeFolder = downloadFiles is [{ IsCompleted: true }];
            ContextFlyoutEnableState.CanRedownload = downloadFiles.Exists(df => df is { IsDownloading: false, IsPaused: false, DownloadProgress: > 0 });
            ContextFlyoutEnableState.CanResume = downloadFiles.Exists(df => df is { IsDownloading: false, IsCompleted: false });
            ContextFlyoutEnableState.CanStop = downloadFiles.Exists(df => df.IsDownloading && df.CanResumeDownload != false);
            ContextFlyoutEnableState.CanRefreshDownloadAddress = downloadFiles is [{ IsDownloading: false, IsCompleted: false }];
            ContextFlyoutEnableState.CanRemove =
                downloadFiles.Count > 0 &&
                downloadFiles.Exists(df => df.DownloadQueueId == null || downloadQueues.FirstOrDefault(dq => dq.Id == df.DownloadQueueId)?.IsRunning != true);

            ContextFlyoutEnableState.CanAddToQueue = downloadFiles.Count > 0 && downloadFiles.Exists(df => !df.IsCompleted);
            ContextFlyoutEnableState.CanRemoveFromQueue =
                downloadFiles.Count > 0 &&
                downloadFiles.Exists(df => df is { DownloadQueueId: not null, IsDownloading: false, IsPaused: false, IsCompleted: false } &&
                                           downloadQueues.FirstOrDefault(dq => dq.Id == df.DownloadQueueId) != null);
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
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

            var isFileExists = downloadFiles
                .Where(df => !df.SaveLocation.IsNullOrEmpty() && !df.FileName.IsNullOrEmpty())
                .Select(df => Path.Combine(df.SaveLocation!, df.FileName!))
                .Where(File.Exists)
                .ToList();

            var deleteFile = false;
            if (isFileExists.Count > 0)
            {
                var result = await DialogBoxManager.ShowWarningDialogAsync("Delete files",
                    $"Do you want to delete file{(isFileExists.Count == 1 ? "" : "s")}?",
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
        if (urlDetails.IsDuplicate)
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

        await AppService.DownloadFileService.AddDownloadFileAsync(downloadFile, urlDetails.IsDuplicate, duplicateAction, startDownloading: true);
    }
}