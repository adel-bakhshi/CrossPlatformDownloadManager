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
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.SettingsWindowViewModels;

public class SaveLocationsViewModel : ViewModelBase
{
    #region Private Fields

    private ObservableCollection<CategoryViewModel> _categories = [];
    private CategoryViewModel? _selectedCategory;
    private FileTypesViewModel? _fileTypesViewModel;

    #endregion

    #region Properties

    public ObservableCollection<CategoryViewModel> Categories
    {
        get => _categories;
        set => this.RaiseAndSetIfChanged(ref _categories, value);
    }

    public CategoryViewModel? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCategory, value);
            LoadFileExtensions();
        }
    }

    public FileTypesViewModel? FileTypesViewModel
    {
        get => _fileTypesViewModel;
        set => this.RaiseAndSetIfChanged(ref _fileTypesViewModel, value);
    }

    #endregion

    #region Commands

    public ICommand AddNewCategoryCommand { get; set; }

    public ICommand EditCategoryCommand { get; set; }

    public ICommand DeleteCategoryCommand { get; set; }

    #endregion

    public SaveLocationsViewModel(IAppService appService) : base(appService)
    {
        FileTypesViewModel = new FileTypesViewModel(appService) { DependsOnCategory = true };

        LoadCategoriesAsync().GetAwaiter();

        AddNewCategoryCommand = ReactiveCommand.CreateFromTask<Window?>(AddNewCategoryAsync);
        EditCategoryCommand = ReactiveCommand.CreateFromTask<Window?>(EditCategoryAsync);
        DeleteCategoryCommand = ReactiveCommand.CreateFromTask(DeleteCategoryAsync);
    }

    private async Task AddNewCategoryAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            var vm = new AddEditCategoryWindowViewModel(AppService)
            {
                IsEditMode = false,
            };

            var window = new AddEditCategoryWindow { DataContext = vm };
            var result = await window.ShowDialog<bool?>(owner);
            if (result != true)
                return;

            await LoadCategoriesAsync();
            SelectedCategory = Categories.LastOrDefault();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to add new category.");
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task EditCategoryAsync(Window? owner)
    {
        try
        {
            if (owner == null || SelectedCategory == null)
                return;

            var selectedCategoryId = SelectedCategory.Id;
            var vm = new AddEditCategoryWindowViewModel(AppService)
            {
                IsEditMode = true,
                CategoryId = SelectedCategory?.Id,
            };

            var window = new AddEditCategoryWindow { DataContext = vm };
            var result = await window.ShowDialog<bool?>(owner);
            if (result != true)
                return;

            await LoadCategoriesAsync();
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == selectedCategoryId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to edit category.");
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task DeleteCategoryAsync()
    {
        try
        {
            if (SelectedCategory == null)
                return;

            var category = await AppService
                .UnitOfWork
                .CategoryRepository
                .GetAsync(where: c => c.Id == SelectedCategory.Id, includeProperties: ["CategorySaveDirectory", "FileExtensions", "DownloadFiles"]);

            if (category == null)
                return;

            if (category.IsDefault)
            {
                await DialogBoxManager.ShowInfoDialogAsync("Delete category", "This default category cannot be deleted.", DialogButtons.Ok);
                return;
            }

            var result = await DialogBoxManager.ShowDangerDialogAsync("Delete category",
                "All data within this category will be permanently deleted. Are you sure you want to proceed?",
                DialogButtons.YesNo);

            if (result != DialogResult.Yes)
                return;

            await AppService
                .UnitOfWork
                .CategorySaveDirectoryRepository
                .DeleteAsync(category.CategorySaveDirectory);

            await AppService
                .UnitOfWork
                .CategoryFileExtensionRepository
                .DeleteAllAsync(category.FileExtensions);

            await AppService
                .UnitOfWork
                .DownloadFileRepository
                .DeleteAllAsync(category.DownloadFiles);

            await AppService
                .UnitOfWork
                .CategoryRepository
                .DeleteAsync(category);

            await AppService
                .UnitOfWork
                .SaveAsync();

            await AppService
                .DownloadFileService
                .LoadDownloadFilesAsync();

            await LoadCategoriesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to delete category.");
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var categories = await AppService
                .UnitOfWork
                .CategoryRepository
                .GetAllAsync(includeProperties: ["CategorySaveDirectory"]);

            var categoryViewModels = AppService.Mapper.Map<List<CategoryViewModel>>(categories);
            Categories = categoryViewModels
                .Where(c => c.Title?.Equals(Constants.GeneralCategoryTitle) != true)
                .ToObservableCollection();

            SelectedCategory = Categories.FirstOrDefault();
            LoadFileExtensions();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to load categories.");
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    public void LoadFileExtensions()
    {
        if (FileTypesViewModel == null || SelectedCategory == null)
            return;

        FileTypesViewModel.CategoryId = SelectedCategory.Id;
    }
}