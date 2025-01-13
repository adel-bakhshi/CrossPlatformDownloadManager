using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Models;
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
            LoadCategoryFileExtensionDataAsync().GetAwaiter();
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
        LoadCategoriesAsync().GetAwaiter();

        SaveCommand = ReactiveCommand.CreateFromTask<Window?>(SaveAsync);
        CloseCommand = ReactiveCommand.CreateFromTask<Window?>(CloseAsync);
    }

    private async Task SaveAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            if (Extension.IsNullOrEmpty() || Alias.IsNullOrEmpty() || SelectedCategory == null)
                return;

            Extension = "." + Extension!.Trim().Replace(".", "").Replace(" ", "").ToLower();
            Alias = Alias!.Trim();

            if (IsEditMode)
            {
                var fileExtension = await AppService
                    .UnitOfWork
                    .CategoryFileExtensionRepository
                    .GetAsync(where: fe => fe.Id == CategoryFileExtensionId);

                if (fileExtension == null)
                    return;

                fileExtension.Extension = Extension!;
                fileExtension.Alias = Alias!;
            }
            else
            {
                // If there is a CategoryFileExtension with Extension and CategoryId entries, it should not be added to the database 
                var fileExtension = await AppService
                    .UnitOfWork
                    .CategoryFileExtensionRepository
                    .GetAsync(where: fe =>
                        fe.Extension.ToLower() == Extension.ToLower() && fe.CategoryId == SelectedCategory.Id);

                if (fileExtension != null)
                    return;

                fileExtension = new CategoryFileExtension
                {
                    Extension = Extension!,
                    Alias = Alias!,
                    CategoryId = SelectedCategory.Id
                };

                await AppService
                    .UnitOfWork
                    .CategoryFileExtensionRepository
                    .AddAsync(fileExtension);
            }

            await AppService
                .UnitOfWork
                .SaveAsync();

            owner.Close(true);
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occured while trying to save the file type. Error message: {ErrorMessage}", ex.Message);
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
            Log.Error(ex, "And error occured while trying to close the window. Error message: {ErrorMessage}", ex.Message);
        }
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var categories = await AppService
                .UnitOfWork
                .CategoryRepository
                .GetAllAsync();

            var categoryViewModels = AppService.Mapper.Map<List<CategoryViewModel>>(categories);
            Categories = categoryViewModels
                .Where(c => c.Title?.Equals(Constants.GeneralCategoryTitle) != true)
                .ToObservableCollection();

            if (!IsEditMode)
                SelectedCategory = Categories.FirstOrDefault();
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occured while trying to load categories. Error message: {ErrorMessage}", ex.Message);
        }
    }

    private async Task LoadCategoryFileExtensionDataAsync()
    {
        try
        {
            var fileExtension = await AppService
                .UnitOfWork
                .CategoryFileExtensionRepository
                .GetAsync(where: fe => fe.Id == CategoryFileExtensionId);

            if (fileExtension == null)
                return;

            Extension = fileExtension.Extension;
            Alias = fileExtension.Alias;
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == fileExtension.CategoryId);
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occured while trying to load category file extension data. Error message: {ErrorMessage}", ex.Message);
        }
    }

    public void SetSelectedCategory(int? categoryId, bool disableCategoryComboBox = false)
    {
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == categoryId);
        if (disableCategoryComboBox)
            CategoryComboBoxIsEnabled = false;
    }
}