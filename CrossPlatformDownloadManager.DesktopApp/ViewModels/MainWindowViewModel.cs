using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
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
    private ObservableCollection<CategoryHeader> _categories;

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
    private ObservableCollection<DownloadFileViewModel> _downloadFiles;

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

    public MainWindowViewModel(MainWindow mainWindow, IUnitOfWork unitOfWork) : base(unitOfWork)
    {
        _mainWindow = mainWindow;

        Categories = LoadCategories();
        SelectedCategory = Categories.FirstOrDefault();
        DownloadFiles = GetDownloadList();

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
            var vm = new AddDownloadLinkWindowViewModel(UnitOfWork)
            {
                Url = urlIsValid ? url : null,
                IsLoadingUrl = urlIsValid
            };

            var window = new AddDownloadLinkWindow { DataContext = vm };
            await window.ShowDialog(_mainWindow);
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

    private ObservableCollection<CategoryHeader> LoadCategories()
    {
        try
        {
            UnitOfWork.CreateCategories();

            var categories = UnitOfWork.CategoryHeaderRepository.GetAll();
            var categoryItems = UnitOfWork.CategoryRepository.GetAll();

            categories = categories
                .Select(c =>
                {
                    c.Categories = categoryItems;
                    return c;
                })
                .ToList();

            return categories.ToObservableCollection();
        }
        catch
        {
            return new ObservableCollection<CategoryHeader>();
        }
    }

    private void FilterDownloadList()
    {
    }

    private ObservableCollection<DownloadFileViewModel> GetDownloadList()
    {
        try
        {
            var downloadFiles = new List<DownloadFileViewModel>();
            for (int i = 0; i < 5; i++)
            {
                downloadFiles.Add(new DownloadFileViewModel
                {
                    Id = i + 1,
                    FileName = $"File name {i + 1}.exe",
                    FileType = "General",
                    QueueName = "Main Queue",
                    Size = 15099494.4.ToFileSize(),
                    IsCompleted = i == 0,
                    IsDownloading = i == 1,
                    IsPaused = i == 2,
                    IsError = i == 3,
                    DownloadProgress = i == 1 ? 49.21 : i == 2 ? 78.64 : i == 3 ? 8.44 : null,
                    TimeLeft = "24 min, 41 sec",
                    TransferRate = "6.8 Mbps",
                    LastTryDate = DateTime.Now.ToString("yyyy/MM/dd - hh:mm tt"),
                    DateAdded = DateTime.Now.ToString("yyyy/MM/dd - hh:mm tt"),
                });
            }

            return downloadFiles.ToObservableCollection();
        }
        catch
        {
            return new ObservableCollection<DownloadFileViewModel>();
        }
    }
}