﻿using System;
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
using CrossPlatformDownloadManager.Utils.Enums;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    #region Private Fields

    private readonly IAppFinisher _appFinisher;

    private readonly DispatcherTimer _updateSpeedTimer;
    private readonly DispatcherTimer _updateActiveDownloadQueuesTimer;

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

    public ICommand AddToQueueContextMenuCommand { get; }

    #endregion

    public MainWindowViewModel(IAppService appService, IAppFinisher appFinisher) : base(appService)
    {
        _appFinisher = appFinisher;

        _updateSpeedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _updateSpeedTimer.Tick += UpdateSpeedTimerOnTick;
        _updateSpeedTimer.Start();

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
        StopDownloadFileCommand = ReactiveCommand.Create<DataGrid?>(StopDownloadFile);
        StopAllDownloadFilesCommand = ReactiveCommand.Create(StopAllDownloadFiles);
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
        AddToQueueContextMenuCommand = ReactiveCommand.Create<DataGrid?>(AddToQueueContextMenu);
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
    }

    private void OpenFileContextMenu(DataGrid? dataGrid)
    {
        // TODO: Show message box
        try
        {
            if (dataGrid == null)
                return;

            var filePaths = dataGrid
                .SelectedItems
                .OfType<DownloadFileViewModel>()
                .Where(df => df.IsCompleted && !df.SaveLocation.IsNullOrEmpty() && !df.FileName.IsNullOrEmpty())
                .Select(df => Path.Combine(df.SaveLocation!, df.FileName!))
                .Distinct()
                .Where(File.Exists)
                .ToList();

            foreach (var filePath in filePaths)
            {
                var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true,
                };

                process.Start();
            }
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
            if (dataGrid == null)
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

    private void AddToQueueContextMenu(DataGrid? dataGrid)
    {
        // TODO: Show message box
        try
        {
            AddToQueueDownloadQueues = [];
            if (dataGrid?.SelectedItem is not DownloadFileViewModel downloadFile)
                return;

            var downloadQueues = AppService
                .DownloadQueueService
                .DownloadQueues;

            var downloadQueue = downloadQueues.FirstOrDefault(dq => dq.Id == downloadFile.DownloadQueueId);
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
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void UpdateSpeedTimerOnTick(object? sender, EventArgs e)
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
}