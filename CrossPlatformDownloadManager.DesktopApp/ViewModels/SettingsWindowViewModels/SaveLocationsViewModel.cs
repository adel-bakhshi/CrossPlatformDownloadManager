using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
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

    private bool _disableCategories;
    private string? _globalSaveDirectory = string.Empty;
    private ObservableCollection<CategoryViewModel> _categories = [];
    private CategoryViewModel? _selectedCategory;
    private FileTypesViewModel? _fileTypesViewModel;

    #endregion

    #region Properties

    public bool DisableCategories
    {
        get => _disableCategories;
        set
        {
            this.RaiseAndSetIfChanged(ref _disableCategories, value);
            this.RaisePropertyChanged(nameof(IsFileTypesViewVisible));
            if (!DisableCategories)
                GlobalSaveDirectory = null;
        }
    }

    public string? GlobalSaveDirectory
    {
        get => _globalSaveDirectory;
        set => this.RaiseAndSetIfChanged(ref _globalSaveDirectory, value);
    }

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
            this.RaisePropertyChanged(nameof(IsFileTypesViewVisible));
            _ = LoadFileExtensionsAsync();
        }
    }

    public FileTypesViewModel? FileTypesViewModel
    {
        get => _fileTypesViewModel;
        set => this.RaiseAndSetIfChanged(ref _fileTypesViewModel, value);
    }

    public bool IsFileTypesViewVisible
    {
        get
        {
            if (SelectedCategory == null)
                return false;

            if (SelectedCategory.Title.Equals(Constants.GeneralCategoryTitle, StringComparison.OrdinalIgnoreCase))
                return false;

            return !DisableCategories;
        }
    }

    #endregion

    #region Commands

    public ICommand BrowseGlobalSaveLocationCommand { get; }

    public ICommand AddNewCategoryCommand { get; }

    public ICommand EditCategoryCommand { get; }

    public ICommand DeleteCategoryCommand { get; }
    
    public ICommand BrowseSaveLocationCommand { get; }

    #endregion

    public SaveLocationsViewModel(IAppService appService) : base(appService)
    {
        FileTypesViewModel = new FileTypesViewModel(appService) { DependsOnCategory = true };

        CheckDisableCategories();
        LoadCategories();

        BrowseGlobalSaveLocationCommand = ReactiveCommand.CreateFromTask(BrowseGlobalSaveLocationAsync);
        AddNewCategoryCommand = ReactiveCommand.CreateFromTask<Window?>(AddNewCategoryAsync);
        EditCategoryCommand = ReactiveCommand.CreateFromTask<Window?>(EditCategoryAsync);
        DeleteCategoryCommand = ReactiveCommand.CreateFromTask(DeleteCategoryAsync);
        BrowseSaveLocationCommand = ReactiveCommand.CreateFromTask(BrowseSaveLocationAsync);
    }

    private async Task BrowseGlobalSaveLocationAsync()
    {
        try
        {
            var directoryPath = await OpenFolderPickerAsync();
            if (directoryPath.IsStringNullOrEmpty())
                return;

            GlobalSaveDirectory = directoryPath;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to browse global save location. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
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

            LoadCategories();
            SelectedCategory = Categories.LastOrDefault();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to add new category. Error message: {ErrorMessage}", ex.Message);
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

            LoadCategories();
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == selectedCategoryId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to edit category.");
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task DeleteCategoryAsync()
    {
        try
        {
            if (SelectedCategory == null)
                return;

            var category = AppService
                .CategoryService
                .Categories
                .FirstOrDefault(c => c.Id == SelectedCategory.Id);

            if (category == null)
                return;

            if (category.IsDefault)
            {
                await DialogBoxManager.ShowInfoDialogAsync("Delete category",
                    "This is a default category and cannot be deleted.",
                    DialogButtons.Ok);

                return;
            }

            var result = await DialogBoxManager.ShowWarningDialogAsync("Delete category",
                $"Deleting the '{category.Title}' category will permanently remove all associated information. Do you wish to continue?",
                DialogButtons.YesNo);

            if (result != DialogResult.Yes)
                return;

            await AppService
                .CategoryService
                .DeleteCategoryAsync(category);

            // When user choose to delete download files in this category, we must to update the list of download files
            // In any case we have to do that
            await AppService
                .DownloadFileService
                .LoadDownloadFilesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to delete category.");
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task BrowseSaveLocationAsync()
    {
        try
        {
            if (SelectedCategory?.CategorySaveDirectory == null)
                return;
            
            var directoryPath = await OpenFolderPickerAsync();
            if (directoryPath.IsStringNullOrEmpty())
                return;

            SelectedCategory.CategorySaveDirectory.SaveDirectory = directoryPath!;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to browse save location. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private void CheckDisableCategories()
    {
        var settings = AppService.SettingsService.Settings;
        DisableCategories = settings.DisableCategories;
        GlobalSaveDirectory = settings.GlobalSaveLocation;
    }

    private void LoadCategories()
    {
        var selectedCategoryId = SelectedCategory?.Id;
        Categories = AppService
            .CategoryService
            .Categories
            .Select(c => c.DeepCopy(ignoreLoops: true)!)
            .ToObservableCollection();

        SelectedCategory = Categories.FirstOrDefault(c => c.Id == selectedCategoryId) ?? Categories.FirstOrDefault();
        _ = LoadFileExtensionsAsync();
    }

    public async Task LoadFileExtensionsAsync()
    {
        try
        {
            if (FileTypesViewModel == null || SelectedCategory == null)
                return;

            FileTypesViewModel.CategoryId = SelectedCategory.Id;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to load file extensions. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    protected override void OnSettingsServiceDataChanged()
    {
        base.OnSettingsServiceDataChanged();
        CheckDisableCategories();
    }

    protected override void OnCategoryServiceCategoriesChanged()
    {
        base.OnCategoryServiceCategoriesChanged();
        LoadCategories();
    }

    private static async Task<string?> OpenFolderPickerAsync()
    {
        var storageProvider = App.Desktop?.MainWindow?.StorageProvider;
        if (storageProvider == null)
            throw new InvalidOperationException("Failed to access to storage. Storage provider is null or undefined.");

        var options = new FolderPickerOpenOptions
        {
            Title = "Select Directory",
            AllowMultiple = false
        };

        var directories = await storageProvider.OpenFolderPickerAsync(options);
        return directories.Any()
            ? directories[0].Path.IsAbsoluteUri
                ? directories[0].Path.LocalPath
                : directories[0].Path.OriginalString
            : null;
    }
}