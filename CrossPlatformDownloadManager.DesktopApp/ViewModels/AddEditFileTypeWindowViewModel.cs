using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.AppService;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class AddEditFileTypeWindowViewModel : ViewModelBase
{
    #region Properties

    public string Title => IsEditMode ? "CDM - Edit File Type" : "CDM - Add New File Type";

    private bool _isEditMode;

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

    private bool _categoryComboBoxIsEnabled;

    public bool CategoryComboBoxIsEnabled
    {
        get => _categoryComboBoxIsEnabled;
        set => this.RaiseAndSetIfChanged(ref _categoryComboBoxIsEnabled, value);
    }

    private int? _categoryFileExtensionId;

    public int? CategoryFileExtensionId
    {
        get => _categoryFileExtensionId;
        set
        {
            this.RaiseAndSetIfChanged(ref _categoryFileExtensionId, value);
            LoadCategoryFileExtensionDataAsync().GetAwaiter();
        }
    }

    private string? _extension;

    public string? Extension
    {
        get => _extension;
        set => this.RaiseAndSetIfChanged(ref _extension, value);
    }

    private string? _alias;

    public string? Alias
    {
        get => _alias;
        set => this.RaiseAndSetIfChanged(ref _alias, value);
    }

    private ObservableCollection<CategoryViewModel> _categories = [];

    public ObservableCollection<CategoryViewModel> Categories
    {
        get => _categories;
        set => this.RaiseAndSetIfChanged(ref _categories, value);
    }

    private CategoryViewModel? _selectedCategory;

    public CategoryViewModel? SelectedCategory
    {
        get => _selectedCategory;
        set => this.RaiseAndSetIfChanged(ref _selectedCategory, value);
    }

    #endregion

    #region Commands

    public ICommand? SaveCommand { get; set; }

    public ICommand? CloseCommand { get; set; }

    #endregion

    public AddEditFileTypeWindowViewModel(IAppService appService) : base(appService)
    {
        LoadCategoriesAsync().GetAwaiter();

        SaveCommand = ReactiveCommand.Create<Window?>(Save);
        CloseCommand = ReactiveCommand.Create<Window?>(Close);
    }

    private async void Save(Window? owner)
    {
        // TODO: Show message box
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
            Console.WriteLine(ex);
        }
    }

    private void Close(Window? owner)
    {
        // TODO: Show message box
        try
        {
            if (owner == null)
                return;

            owner.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async Task LoadCategoriesAsync()
    {
        // TODO: Show message box
        try
        {
            var categories = await AppService
                .UnitOfWork
                .CategoryRepository
                .GetAllAsync();
            
            var categoryViewModels = AppService
                .Mapper
                .Map<List<CategoryViewModel>>(categories);
            
            Categories = categoryViewModels.ToObservableCollection();
            if (!IsEditMode)
                SelectedCategory = Categories.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async Task LoadCategoryFileExtensionDataAsync()
    {
        // TODO: Show message box
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
            Console.WriteLine(ex);
        }
    }

    public void SetSelectedCategory(int? categoryId, bool disableCategoryComboBox = false)
    {
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == categoryId);
        if (disableCategoryComboBox)
            CategoryComboBoxIsEnabled = false;
    }
}