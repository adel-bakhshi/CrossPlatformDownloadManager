using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.SettingsWindowViewModels;

public class FileTypesViewModel : ViewModelBase
{
    #region Private Fields

    private readonly List<CategoryFileExtensionViewModel> _dbFileExtensions;

    private string? _searchText;
    private ObservableCollection<CategoryFileExtensionViewModel> _fileExtensions = [];
    private CategoryFileExtensionViewModel? _selectedFileExtension;
    private int? _categoryId;
    private bool _dependsOnCategory;

    #endregion

    #region Properties

    public string? SearchText
    {
        get => _searchText;
        set
        {
            this.RaiseAndSetIfChanged(ref _searchText, value);
            FilterFileExtensions();
        }
    }

    public ObservableCollection<CategoryFileExtensionViewModel> FileExtensions
    {
        get => _fileExtensions;
        set => this.RaiseAndSetIfChanged(ref _fileExtensions, value);
    }

    public CategoryFileExtensionViewModel? SelectedFileExtension
    {
        get => _selectedFileExtension;
        set => this.RaiseAndSetIfChanged(ref _selectedFileExtension, value);
    }

    public int? CategoryId
    {
        get => _categoryId;
        set
        {
            this.RaiseAndSetIfChanged(ref _categoryId, value);
            _ = LoadFileExtensionsAsync();
        }
    }

    public bool DependsOnCategory
    {
        get => _dependsOnCategory;
        set
        {
            this.RaiseAndSetIfChanged(ref _dependsOnCategory, value);
            _ = LoadFileExtensionsAsync();
        }
    }

    #endregion

    #region Commands

    public ICommand AddNewFileTypeCommand { get; set; }

    public ICommand EditFileTypeCommand { get; set; }

    public ICommand DeleteFileTypeCommand { get; set; }

    #endregion

    public FileTypesViewModel(IAppService appService) : base(appService)
    {
        _dbFileExtensions = [];

        _ = LoadFileExtensionsAsync();

        AddNewFileTypeCommand = ReactiveCommand.CreateFromTask<Window?>(AddNewFileTypeAsync);
        EditFileTypeCommand = ReactiveCommand.CreateFromTask<Window?>(EditFileTypeAsync);
        DeleteFileTypeCommand = ReactiveCommand.CreateFromTask(DeleteFileTypeAsync);
    }

    private async Task AddNewFileTypeAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            var vm = new AddEditFileTypeWindowViewModel(AppService) { IsEditMode = false };
            vm.SetSelectedCategory(CategoryId, DependsOnCategory);
            var window = new AddEditFileTypeWindow { DataContext = vm };
            var result = await window.ShowDialog<bool?>(owner);
            if (result != true)
                return;

            await LoadFileExtensionsAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while adding a new file type. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task EditFileTypeAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            var vm = new AddEditFileTypeWindowViewModel(AppService)
            {
                IsEditMode = true,
                CategoryFileExtensionId = SelectedFileExtension?.Id
            };

            var window = new AddEditFileTypeWindow { DataContext = vm };
            var result = await window.ShowDialog<bool?>(owner);
            if (result != true)
                return;

            await LoadFileExtensionsAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while editing a file type.");
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task DeleteFileTypeAsync()
    {
        try
        {
            if (SelectedFileExtension == null)
                return;

            var category = AppService
                .CategoryService
                .Categories
                .FirstOrDefault(c => c.Id == CategoryId);

            if (category == null)
                return;

            var fileExtension = AppService
                .CategoryService
                .Categories
                .SelectMany(c => c.FileExtensions)
                .FirstOrDefault(fe => fe.Id == SelectedFileExtension.Id);

            if (fileExtension == null)
                return;

            await AppService.CategoryService.DeleteFileExtensionAsync(category, fileExtension);
            await LoadFileExtensionsAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while deleting a file type.");
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    public async Task LoadFileExtensionsAsync()
    {
        try
        {
            _dbFileExtensions.Clear();

            List<CategoryFileExtensionViewModel> fileExtensions;
            if (DependsOnCategory)
            {
                fileExtensions = AppService
                    .CategoryService
                    .Categories
                    .SelectMany(c => c.FileExtensions)
                    .Where(fe => fe.CategoryId == CategoryId)
                    .OrderBy(fe => fe.Category!.Id)
                    .ThenBy(fe => fe.Extension)
                    .ToList();
            }
            else
            {
                fileExtensions = AppService
                    .CategoryService
                    .Categories
                    .SelectMany(c => c.FileExtensions)
                    .OrderBy(fe => fe.CategoryId)
                    .ThenBy(fe => fe.Extension)
                    .ToList();
            }

            _dbFileExtensions.AddRange(fileExtensions);
            FilterFileExtensions();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to load file extensions. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    #region Helpers

    private void FilterFileExtensions()
    {
        var selectedFileExtensionId = SelectedFileExtension?.Id;
        if (SearchText.IsNullOrEmpty())
        {
            FileExtensions = _dbFileExtensions.ToObservableCollection();
        }
        else
        {
            FileExtensions = _dbFileExtensions
                .FindAll(fe => fe.Extension.Contains(SearchText!, StringComparison.OrdinalIgnoreCase)
                               || fe.Alias.Contains(SearchText!, StringComparison.OrdinalIgnoreCase))
                .OrderBy(fe => fe.Category?.Title)
                .ThenBy(fe => fe.Extension)
                .ToObservableCollection();
        }

        if (selectedFileExtensionId != null)
            SelectedFileExtension = FileExtensions.FirstOrDefault(fe => fe.Id == selectedFileExtensionId);
    }

    #endregion
}