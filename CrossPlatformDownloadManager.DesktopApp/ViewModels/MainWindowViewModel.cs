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
    private CategoryHeader? _selectedCategory = null;

    public CategoryHeader? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (_selectedCategory != null && value != _selectedCategory)
                SelectedCategoryItem = null;

            this.RaiseAndSetIfChanged(ref _selectedCategory, value);
            FilterDownloadList();
        }
    }

    // Selected category item
    private Category? _selectedCategoryItem = null;

    public Category? SelectedCategoryItem
    {
        get => _selectedCategoryItem;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCategoryItem, value);
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

    #endregion

    #region Commands

    public ICommand SelectAllRowsCommand { get; }

    public ICommand AddNewLinkCommand { get; }

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
        SelectedCategory = Categories.FirstOrDefault();
        UpdateDownloadList();

        TotalSpeed = "0 KB";
        SelectedFilesTotalSize = "0 KB";

        SelectAllRowsCommand = ReactiveCommand.Create<object>(SelectAllRows);
        AddNewLinkCommand = ReactiveCommand.Create(AddNewLink);
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
        // TODO: Complete this method
    }

    private void UpdateDownloadList()
    {
        DownloadFiles = DownloadFileService.DownloadFiles;
    }

    protected override void DownloadFileServiceDataChanged(DownloadFileServiceEventArgs eventArgs)
    {
        DownloadFiles = eventArgs.DownloadFiles.ToObservableCollection();
    }

    public async Task LoadDownloadFilesAsync()
    {
        await DownloadFileService.LoadFilesAsync();
    }
}