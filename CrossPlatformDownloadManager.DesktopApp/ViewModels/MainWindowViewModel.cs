using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.AppService;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.AppFinisher;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    #region Private Fields

    private readonly IAppFinisher _appFinisher;

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
    private string? _totalSpeed;
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

    public string? TotalSpeed
    {
        get => _totalSpeed;
        set => this.RaiseAndSetIfChanged(ref _totalSpeed, value);
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

    public MainWindowViewModel(IAppService appService, IAppFinisher appFinisher) : base(appService)
    {
        _appFinisher = appFinisher;

        _updateDownloadSpeedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _updateDownloadSpeedTimer.Tick += UpdateDownloadSpeedTimerOnTick;
        _updateDownloadSpeedTimer.Start();

        _updateActiveDownloadQueuesTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _updateActiveDownloadQueuesTimer.Tick += UpdateActiveDownloadQueuesTimerOnTick;
        _updateActiveDownloadQueuesTimer.Start();

        LoadCategoriesAsync().GetAwaiter();
        FilterDownloadList();
        LoadDownloadQueues();

        TotalSpeed = "0 KB";
        SelectedFilesTotalSize = "0 KB";

        SelectAllRowsCommand = ReactiveCommand.Create<DataGrid?>(SelectAllRows);
        AddNewLinkCommand = ReactiveCommand.Create<Window?>(AddNewLink);
        ResumeDownloadFileCommand = ReactiveCommand.Create<DataGrid?>(ResumeDownloadFile);
        StopDownloadFileCommand = ReactiveCommand.CreateFromTask<DataGrid?>(StopDownloadFileAsync);
        StopAllDownloadFilesCommand = ReactiveCommand.CreateFromTask(StopAllDownloadFilesAsync);
        DeleteDownloadFilesCommand = ReactiveCommand.Create<DataGrid?>(DeleteDownloadFiles);
        DeleteCompletedDownloadFilesCommand = ReactiveCommand.Create(DeleteCompletedDownloadFiles);
        OpenSettingsWindowCommand = ReactiveCommand.Create<Window?>(OpenSettingsWindow);
        StartStopDownloadQueueCommand =
            ReactiveCommand.CreateFromTask<DownloadQueueViewModel?>(StartStopDownloadQueueAsync);
        ShowDownloadQueueDetailsCommand = ReactiveCommand.Create<Button?>(ShowDownloadQueueDetails);
        AddNewDownloadQueueCommand = ReactiveCommand.Create<Window?>(AddNewDownloadQueue);
        ExitProgramCommand = ReactiveCommand.CreateFromTask(ExitProgramAsync);
        SelectAllRowsContextMenuCommand = ReactiveCommand.Create<DataGrid?>(SelectAllRowsContextMenu);
        OpenFileContextMenuCommand = ReactiveCommand.Create<DataGrid?>(OpenFileContextMenu);
        OpenFolderContextMenuCommand = ReactiveCommand.Create<DataGrid?>(OpenFolderContextMenu);
        RenameContextMenuCommand = ReactiveCommand.Create<DataGrid?>(RenameContextMenu);
        ChangeFolderContextMenuCommand = ReactiveCommand.CreateFromTask<DataGrid?>(ChangeFolderContextMenuAsync);
        RedownloadContextMenuCommand = ReactiveCommand.CreateFromTask<DataGrid?>(RedownloadContextMenuAsync);
        ResumeContextMenuCommand = ReactiveCommand.Create<DataGrid?>(ResumeContextMenu);
        StopContextMenuCommand = ReactiveCommand.CreateFromTask<DataGrid?>(StopContextMenuAsync);
        RefreshDownloadAddressContextMenuCommand = ReactiveCommand.Create<DataGrid?>(RefreshDownloadAddressContextMenu);
        RemoveContextMenuCommand = ReactiveCommand.CreateFromTask<DataGrid?>(RemoveContextMenuAsync);
        AddToQueueContextMenuCommand = ReactiveCommand.Create<DataGrid?>(AddToQueueContextMenu);
        RemoveFromQueueContextMenuCommand = ReactiveCommand.CreateFromTask<DataGrid?>(RemoveFromQueueContextMenuAsync);
        AddDownloadFileToDownloadQueueContextMenuCommand =
            ReactiveCommand.CreateFromTask<DownloadQueueViewModel?>(AddDownloadFileToDownloadQueueContextMenuAsync);
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

    private async Task StopDownloadFileAsync(DataGrid? dataGrid)
    {
        // TODO: Show message box
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

    private async Task StopAllDownloadFilesAsync()
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
                .Where(df => df.IsDownloading || df.IsPaused)
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

    private async Task ExitProgramAsync()
    {
        // TODO: Show message box
        try
        {
            await _appFinisher.FinishAppAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void SelectAllRowsContextMenu(DataGrid? dataGrid)
    {
        dataGrid?.SelectAll();
        HideContextMenu();
    }

    private void OpenFileContextMenu(DataGrid? dataGrid)
    {
        // TODO: Show message box
        try
        {
            if (dataGrid?.SelectedItem is not DownloadFileViewModel { IsDownloading: true } downloadFile ||
                downloadFile.SaveLocation.IsNullOrEmpty() ||
                downloadFile.FileName.IsNullOrEmpty())
            {
                HideContextMenu();
                return;
            }

            var filePath = Path.Combine(downloadFile.SaveLocation!, downloadFile.FileName!);
            if (!File.Exists(filePath))
            {
                HideContextMenu();
                return;
            }

            var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true,
            };

            process.Start();
            HideContextMenu();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void OpenFolderContextMenu(DataGrid? dataGrid)
    {
        // TODO: Show message box
        try
        {
            HideContextMenu();

            if (dataGrid == null || dataGrid.SelectedItems.Count == 0)
                return;

            var folderPaths = dataGrid
                .SelectedItems
                .OfType<DownloadFileViewModel>()
                .Where(df => !df.SaveLocation.IsNullOrEmpty() && Directory.Exists(df.SaveLocation!))
                .Select(df => df.SaveLocation!)
                .Distinct()
                .ToList();

            foreach (var folderPath in folderPaths)
            {
                var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = folderPath,
                    UseShellExecute = true
                };

                process.Start();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void RenameContextMenu(DataGrid? dataGrid)
    {
        // TODO: Show message box
        try
        {
            if (dataGrid?.SelectedItem is not DownloadFileViewModel { IsCompleted: true } downloadFile ||
                downloadFile.FileName.IsNullOrEmpty() ||
                downloadFile.SaveLocation.IsNullOrEmpty())
            {
                HideContextMenu();
                return;
            }

            var filePath = Path.Combine(downloadFile.SaveLocation!, downloadFile.FileName!);
            if (!File.Exists(filePath))
            {
                HideContextMenu();
                return;
            }

            var vm = new ChangeFileNameWindowViewModel(AppService, downloadFile);
            var window = new ChangeFileNameWindow { DataContext = vm };
            window.Show();

            HideContextMenu();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async Task ChangeFolderContextMenuAsync(DataGrid? dataGrid)
    {
        // TODO: Show message box
        try
        {
            HideContextMenu();

            if (dataGrid?.SelectedItem is not DownloadFileViewModel { IsCompleted: true } downloadFile ||
                downloadFile.FileName.IsNullOrEmpty() ||
                downloadFile.SaveLocation.IsNullOrEmpty() ||
                _mainWindow == null)
            {
                return;
            }

            var filePath = Path.Combine(downloadFile.SaveLocation!, downloadFile.FileName!);
            if (!File.Exists(filePath))
                return;

            var newSaveLocation = await _mainWindow.ChangeSaveLocationAsync(downloadFile.SaveLocation!);
            if (newSaveLocation.IsNullOrEmpty())
                return;

            var newFilePath = Path.Combine(newSaveLocation!, downloadFile.FileName!);
            if (File.Exists(newFilePath))
                return;

            File.Move(filePath, newFilePath);

            downloadFile.SaveLocation = newSaveLocation;

            await AppService
                .DownloadFileService
                .UpdateDownloadFileAsync(downloadFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async Task RedownloadContextMenuAsync(DataGrid? dataGrid)
    {
        // TODO: Show message box
        try
        {
            HideContextMenu();

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
            Console.WriteLine(ex);
        }
    }

    private void ResumeContextMenu(DataGrid? dataGrid)
    {
        // TODO: Show message box
        try
        {
            if (dataGrid == null || dataGrid.SelectedItems.Count == 0)
                return;

            ResumeDownloadFile(dataGrid);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async Task StopContextMenuAsync(DataGrid? dataGrid)
    {
        // TODO: Show message box
        try
        {
            if (dataGrid == null || dataGrid.SelectedItems.Count == 0)
                return;

            await StopDownloadFileAsync(dataGrid);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void RefreshDownloadAddressContextMenu(DataGrid? dataGrid)
    {
        // TODO: Show message box
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
            Console.WriteLine(ex);
        }
    }

    private async Task RemoveContextMenuAsync(DataGrid? dataGrid)
    {
        // TODO: Show message box
        try
        {
            HideContextMenu();

            if (dataGrid == null || dataGrid.SelectedItems.Count == 0)
                return;

            var downloadFiles = dataGrid
                .SelectedItems
                .OfType<DownloadFileViewModel>()
                .ToList();

            await AppService
                .DownloadFileService
                .DeleteDownloadFilesAsync(downloadFiles, alsoDeleteFile: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void AddToQueueContextMenu(DataGrid? dataGrid)
    {
        // TODO: Show message box
        try
        {
            // Clear previous data stored in AddToQueueDownloadQueues
            AddToQueueDownloadQueues = [];
            if (dataGrid == null || dataGrid.SelectedItems.Count == 0)
            {
                HideContextMenu();
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
                HideContextMenu();
                return;
            }

            switch (downloadFiles.Count)
            {
                // If count of selected download files is equal to 0 then hide context menu
                case 0:
                {
                    HideContextMenu();
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
                                HideContextMenu();

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
            Console.WriteLine(ex);
        }
    }

    private async Task RemoveFromQueueContextMenuAsync(DataGrid? dataGrid)
    {
        // TODO: Show message box
        try
        {
            HideContextMenu();

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
            Console.WriteLine(ex);
        }
    }

    private async Task AddDownloadFileToDownloadQueueContextMenuAsync(DownloadQueueViewModel? viewModel)
    {
        // TODO: Show message box
        try
        {
            HideContextMenu();

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
            Console.WriteLine(ex);
        }
    }

    private void UpdateDownloadSpeedTimerOnTick(object? sender, EventArgs e)
    {
        var totalSpeed = AppService
            .DownloadFileService
            .DownloadFiles
            .Where(df => df.IsDownloading)
            .Sum(df => df.TransferRate ?? 0);

        TotalSpeed = totalSpeed == 0 ? "0 KB" : totalSpeed.ToFileSize();
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
            SelectedCategoryHeader = CategoryHeaders.FirstOrDefault();
        }
        catch
        {
            CategoryHeaders = [];
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

    public void ChangeContextFlyoutEnableState(MainWindow? mainWindow)
    {
        // TODO: Show message box
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
            ContextFlyoutEnableState.CanRedownload =
                downloadFiles.Exists(df => df is { IsDownloading: false, IsPaused: false, DownloadProgress: > 0 });

            ContextFlyoutEnableState.CanResume =
                downloadFiles.Exists(df => df is { IsDownloading: false, IsCompleted: false });

            ContextFlyoutEnableState.CanStop = downloadFiles.Exists(df => df.IsDownloading);
            ContextFlyoutEnableState.CanRefreshDownloadAddress =
                downloadFiles is [{ IsDownloading: false, IsCompleted: false }];

            ContextFlyoutEnableState.CanRemove = downloadFiles.Count > 0;
            ContextFlyoutEnableState.CanAddToQueue =
                downloadFiles.Count > 0 && downloadFiles.Exists(df => !df.IsCompleted);

            ContextFlyoutEnableState.CanRemoveFromQueue = downloadFiles.Count > 0 &&
                                                          downloadFiles.Exists(df =>
                                                              df is
                                                              {
                                                                  DownloadQueueId: not null,
                                                                  IsDownloading: false,
                                                                  IsPaused: false,
                                                                  IsCompleted: false
                                                              } &&
                                                              downloadQueues.FirstOrDefault(dq =>
                                                                  dq.Id == df.DownloadQueueId) != null);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async void HideContextMenu()
    {
        await Task.Delay(100);
        _mainWindow?.HideDownloadFilesDataGridContextMenu();
        _mainWindow = null;
    }
}