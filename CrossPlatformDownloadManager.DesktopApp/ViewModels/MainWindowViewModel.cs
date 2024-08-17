using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.CategoryViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.FileTypeViewModels;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    #region Properties

    // Categories list data
    private ObservableCollection<CategoryViewModel> _categories;

    public ObservableCollection<CategoryViewModel> Categories
    {
        get => _categories;
        set => this.RaiseAndSetIfChanged(ref _categories, value);
    }

    // Selected category
    private CategoryViewModel? _selectedCategory = null;

    public CategoryViewModel? SelectedCategory
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
    private CategoryItemViewModel? _selectedCategoryItem = null;

    public CategoryItemViewModel? SelectedCategoryItem
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

    #endregion

    public MainWindowViewModel()
    {
        Categories = LoadCategories();
        SelectedCategory = Categories.FirstOrDefault();
        DownloadFiles = GetDownloadList();

        SelectAllRowsCommand = ReactiveCommand.Create<object>(SelectAllRows);
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

    private ObservableCollection<CategoryViewModel> LoadCategories()
    {
        try
        {
            var assetName = "avares://CrossPlatformDownloadManager.DesktopApp/Assets/categories.json";
            var assetsUri = new Uri(assetName);
            using var stream = AssetLoader.Open(assetsUri);
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var categories = json.DeserializeJson<List<CategoryViewModel>>();
            return new ObservableCollection<CategoryViewModel>(categories ?? new List<CategoryViewModel>());
        }
        catch
        {
            return new ObservableCollection<CategoryViewModel>();
        }
    }

    private void FilterDownloadList()
    {
    }

    private ObservableCollection<DownloadFileViewModel> GetDownloadList()
    {
        try
        {
            var assetName = "avares://CrossPlatformDownloadManager.DesktopApp/Assets/file-types.json";
            var assetsUri = new Uri(assetName);
            using var stream = AssetLoader.Open(assetsUri);
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var fileTypes = json.DeserializeJson<List<FileTypeViewModel>>();

            var downloadFiles = new List<DownloadFileViewModel>();
            for (int i = 0; i < 5; i++)
            {
                downloadFiles.Add(new DownloadFileViewModel
                {
                    Id = i + 1,
                    FileName = $"File name {i + 1}.exe",
                    FileType = fileTypes
                        .Where(f => f.FileType.Equals("Programs"))
                        .SelectMany(f => f.TypeExtensions)
                        .FirstOrDefault(f => f.Extension.Equals(".exe"))?
                        .Alias ?? "General",
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

            return new ObservableCollection<DownloadFileViewModel>(downloadFiles);
        }
        catch
        {
            return new ObservableCollection<DownloadFileViewModel>();
        }
    }
}