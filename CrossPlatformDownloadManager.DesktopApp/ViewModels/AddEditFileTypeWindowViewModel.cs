using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class AddEditFileTypeWindowViewModel : ViewModelBase
{
    #region Private Fields

    private bool _isEditMode;
    private bool _categoryComboBoxIsEnabled;
    private int? _categoryFileExtensionId;
    private string? _extension;
    private string? _alias;
    private ObservableCollection<CategoryViewModel> _categories = [];
    private CategoryViewModel? _selectedCategory;

    #endregion

    #region Properties

    public string Title => IsEditMode ? "CDM - Edit File Type" : "CDM - Add New File Type";

    public bool IsEditMode
    {
        get => _isEditMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _isEditMode, value);
            this.RaisePropertyChanged(nameof(Title));
            CategoryComboBoxIsEnabled = !IsEditMode;
        }
    }

    public bool CategoryComboBoxIsEnabled
    {
        get => _categoryComboBoxIsEnabled;
        set => this.RaiseAndSetIfChanged(ref _categoryComboBoxIsEnabled, value);
    }

    public int? CategoryFileExtensionId
    {
        get => _categoryFileExtensionId;
        set
        {
            this.RaiseAndSetIfChanged(ref _categoryFileExtensionId, value);
            LoadCategoryFileExtensionData();
        }
    }

    public string? Extension
    {
        get => _extension;
        set => this.RaiseAndSetIfChanged(ref _extension, value);
    }

    public string? Alias
    {
        get => _alias;
        set => this.RaiseAndSetIfChanged(ref _alias, value);
    }

    public ObservableCollection<CategoryViewModel> Categories
    {
        get => _categories;
        set => this.RaiseAndSetIfChanged(ref _categories, value);
    }

    public CategoryViewModel? SelectedCategory
    {
        get => _selectedCategory;
        set => this.RaiseAndSetIfChanged(ref _selectedCategory, value);
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; set; }

    public ICommand CloseCommand { get; set; }

    #endregion

    public AddEditFileTypeWindowViewModel(IAppService appService) : base(appService)
    {
        LoadCategories();

        SaveCommand = ReactiveCommand.CreateFromTask<Window?>(SaveAsync);
        CloseCommand = ReactiveCommand.CreateFromTask<Window?>(CloseAsync);
    }

    private async Task SaveAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            Extension = "." + Extension?.Trim('.').Replace(".", "").Replace(" ", "").ToLower();
            Alias = Alias?.Trim();
            if (Extension.IsStringNullOrEmpty() || Alias.IsStringNullOrEmpty() || SelectedCategory == null)
                return;

            if (IsEditMode)
            {
                var fileExtension = AppService
                    .CategoryService
                    .Categories
                    .SelectMany(c => c.FileExtensions)
                    .FirstOrDefault(fe => fe.Id == CategoryFileExtensionId);

                if (fileExtension == null)
                    return;

                fileExtension.Extension = Extension!;
                fileExtension.Alias = Alias!;

                await AppService.CategoryService.UpdateFileExtensionAsync(SelectedCategory, fileExtension);
            }
            else
            {
                var fileExtension = AppService
                    .CategoryService
                    .Categories
                    .SelectMany(c => c.FileExtensions)
                    .FirstOrDefault(fe => fe.Extension.Equals(Extension, StringComparison.OrdinalIgnoreCase)
                                          && fe.Category != null
                                          && fe.Category.Id == SelectedCategory.Id);

                if (fileExtension != null)
                    return;

                fileExtension = new CategoryFileExtensionViewModel
                {
                    Extension = Extension!,
                    Alias = Alias!,
                    CategoryId = SelectedCategory.Id
                };

                await AppService.CategoryService.AddFileExtensionAsync(SelectedCategory, fileExtension);
            }

            owner.Close(true);
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred while trying to save the file type. Error message: {ErrorMessage}", ex.Message);
        }
    }

    private static async Task CloseAsync(Window? owner)
    {
        try
        {
            owner?.Close();
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "And error occurred while trying to close the window. Error message: {ErrorMessage}", ex.Message);
        }
    }

    private void LoadCategories()
    {
        Categories = AppService
            .CategoryService
            .Categories
            .Where(c => !c.Title.Equals(Constants.GeneralCategoryTitle))
            .ToObservableCollection();

        if (!IsEditMode)
            SelectedCategory = Categories.FirstOrDefault();
    }

    private void LoadCategoryFileExtensionData()
    {
        var fileExtension = AppService
            .CategoryService
            .Categories
            .SelectMany(c => c.FileExtensions)
            .FirstOrDefault(fe => fe.Id == CategoryFileExtensionId);

        if (fileExtension == null)
            return;

        Extension = fileExtension.Extension;
        Alias = fileExtension.Alias;
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == fileExtension.CategoryId);
    }

    public void SetSelectedCategory(int? categoryId, bool disableCategoryComboBox = false)
    {
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == categoryId);
        if (disableCategoryComboBox)
            CategoryComboBoxIsEnabled = false;
    }

    protected override void OnCategoryServiceCategoriesChanged()
    {
        base.OnCategoryServiceCategoriesChanged();
        LoadCategories();
    }
}