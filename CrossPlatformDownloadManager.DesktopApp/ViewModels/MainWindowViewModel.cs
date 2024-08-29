using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
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

    #endregion

    #region Commands

    public ICommand SelectAllRowsCommand { get; }

    public ICommand AddNewLinkCommand { get; }

    #endregion

    public MainWindowViewModel(IUnitOfWork unitOfWork, IDownloadFileService downloadFileService, MainWindow mainWindow)
        : base(unitOfWork, downloadFileService)
    {
        _mainWindow = mainWindow;

        // Create basic data before application startup
        UnitOfWork.CreateCategoriesAsync().GetAwaiter().GetResult();

        LoadCategoriesAsync().GetAwaiter().GetResult();
        SelectedCategory = Categories.FirstOrDefault();
        UpdateDownloadList();

        SelectAllRowsCommand = ReactiveCommand.Create<object>(SelectAllRows);
        AddNewLinkCommand = ReactiveCommand.Create(AddNewLink);
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
}