using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.DownloadFileService;
using CrossPlatformDownloadManager.Data.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    #region Private Fields

    private readonly MainWindow _mainWindow;
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

    #endregion

    #region Commands

    public ICommand SelectAllRowsCommand { get; }

    public ICommand AddNewLinkCommand { get; }

    public ICommand ResumeDownloadCommand { get; }

    public ICommand StopDownloadCommand { get; }

    public ICommand StopAllDownloadsCommand { get; }

    public ICommand DeleteDownloadFilesCommand { get; }

    public ICommand DeleteCompletedDownloadFilesCommand { get; }

    #endregion

    public MainWindowViewModel(IUnitOfWork unitOfWork, IDownloadFileService downloadFileService, MainWindow mainWindow)
        : base(unitOfWork, downloadFileService)
    {
        _mainWindow = mainWindow;

        _updateSpeedTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _updateSpeedTimer.Tick += UpdateSpeedTimerOnTick;
        _updateSpeedTimer.Start();

        // Create basic data before application startup
        UnitOfWork.CreateCategoriesAsync().GetAwaiter().GetResult();

        LoadCategoriesAsync().GetAwaiter().GetResult();
        SelectedCategoryHeader = Categories.FirstOrDefault();
        UpdateDownloadList();

        TotalSpeed = "0 KB";
        SelectedFilesTotalSize = "0 KB";

        SelectAllRowsCommand = ReactiveCommand.Create<object>(SelectAllRows);
        AddNewLinkCommand = ReactiveCommand.Create(AddNewLink);
        ResumeDownloadCommand = ReactiveCommand.Create<DataGrid?>(ResumeDownload);
        StopDownloadCommand = ReactiveCommand.Create<DataGrid?>(StopDownload);
        StopAllDownloadsCommand = ReactiveCommand.Create(StopAllDownloads);
        DeleteDownloadFilesCommand = ReactiveCommand.Create<DataGrid?>(DeleteDownloadFiles);
        DeleteCompletedDownloadFilesCommand = ReactiveCommand.Create(DeleteCompletedDownloadFiles);
    }

    private void ResumeDownload(DataGrid? dataGrid)
    {
        // TODO: Show message box
        try
        {
            if (dataGrid == null || dataGrid.SelectedItems.Count == 0)
                return;

            foreach (var selectedItem in dataGrid.SelectedItems)
            {
                var id = (selectedItem as DownloadFileViewModel)?.Id;
                var downloadFile = DownloadFileService.DownloadFiles.FirstOrDefault(df => df.Id == id);
                if (downloadFile == null)
                    continue;

                if (downloadFile.IsDownloading || downloadFile.IsCompleted)
                    continue;

                var vm = new DownloadWindowViewModel(UnitOfWork, DownloadFileService, downloadFile);
                var window = new DownloadWindow { DataContext = vm };
                window.Show();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async void StopDownload(DataGrid? dataGrid)
    {
        // TODO: Show message box
        try
        {
            if (dataGrid == null || dataGrid.SelectedItems.Count == 0)
                return;

            foreach (var selectedItem in dataGrid.SelectedItems)
            {
                var id = (selectedItem as DownloadFileViewModel)?.Id;
                var downloadFile = DownloadFileService.DownloadFiles.FirstOrDefault(df => df.Id == id);
                if (downloadFile == null)
                    continue;

                if (!downloadFile.IsDownloading)
                    continue;

                await DownloadFileService.StopDownloadFileAsync(downloadFile, true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async void StopAllDownloads()
    {
        // TODO: Show message box
        try
        {
            var downloadFiles = DownloadFileService.DownloadFiles
                .Where(df => df.IsDownloading)
                .ToList();

            foreach (var downloadFile in downloadFiles)
                await DownloadFileService.StopDownloadFileAsync(downloadFile, true);
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

            foreach (var selectedItem in dataGrid.SelectedItems)
            {
                var id = (selectedItem as DownloadFileViewModel)?.Id;
                var downloadFile = DownloadFileService.DownloadFiles.FirstOrDefault(df => df.Id == id);
                if (downloadFile == null)
                    continue;

                if (downloadFile.IsDownloading)
                    await DownloadFileService.StopDownloadFileAsync(downloadFile, true);

                await DownloadFileService.DeleteDownloadFileAsync(downloadFile, true);
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
            var downloadFiles = DownloadFileService.DownloadFiles
                .Where(df => df.IsCompleted)
                .ToList();

            foreach (var downloadFile in downloadFiles)
                await DownloadFileService.DeleteDownloadFileAsync(downloadFile, false);
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
            var totalSpeed = DownloadFileService.DownloadFiles
                .Where(df => df.IsDownloading)
                .Sum(df => df.TransferRate ?? 0);

            TotalSpeed = totalSpeed == 0 ? "0 KB" : totalSpeed.ToFileSize();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async void AddNewLink()
    {
        try
        {
            var url = string.Empty;
            if (_mainWindow.Clipboard != null)
                url = await _mainWindow.Clipboard.GetTextAsync();

            var urlIsValid = url.CheckUrlValidation();
            var vm = new AddDownloadLinkWindowViewModel(UnitOfWork, DownloadFileService)
            {
                Url = urlIsValid ? url : null,
                IsLoadingUrl = urlIsValid
            };

            var window = new AddDownloadLinkWindow { DataContext = vm };
            var result = await window.ShowDialog<bool>(_mainWindow);
            if (!result)
                return;

            // await LoadCategoriesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void SelectAllRows(object? obj)
    {
        if (obj == null || obj.GetType() != typeof(DataGrid))
            return;

        var grd = obj as DataGrid;
        if (!SelectAllDownloadFiles)
            grd!.SelectedIndex = -1;
        else
            grd!.SelectAll();
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var categoryHeaders = await UnitOfWork.CategoryHeaderRepository.GetAllAsync();
            var categories = await UnitOfWork.CategoryRepository.GetAllAsync();

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
            Categories = new ObservableCollection<CategoryHeader>();
        }
    }

    private void FilterDownloadList()
    {
        // TODO: Show message box
        try
        {
            var downloadFiles = DownloadFileService.DownloadFiles.ToList();
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
                                 || (df.SaveLocation?.Contains(SearchText!, StringComparison.OrdinalIgnoreCase) ?? true))
                    .ToList();
            }

            DownloadFiles = downloadFiles.ToObservableCollection();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void UpdateDownloadList()
    {
        DownloadFiles = DownloadFileService.DownloadFiles;
    }

    protected override void DownloadFileServiceDataChanged(DownloadFileServiceEventArgs eventArgs)
    {
        FilterDownloadList();
    }

    public async Task LoadDownloadFilesAsync()
    {
        await DownloadFileService.LoadFilesAsync();
    }
}