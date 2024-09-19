using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Services.DownloadFileService;
using CrossPlatformDownloadManager.Data.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.SettingsWindowViewModels;

public class SaveLocationsViewModel : ViewModelBase
{
    #region Private Fields

    private readonly List<CategoryFileExtensionViewModel> _dbFileExtensions;

    #endregion

    #region Properties

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
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCategory, value);
            LoadFileExtensions();
        }
    }

    private FileTypesViewModel? _fileTypesViewModel;

    public FileTypesViewModel? FileTypesViewModel
    {
        get => _fileTypesViewModel;
        set => this.RaiseAndSetIfChanged(ref _fileTypesViewModel, value);
    }

    #endregion

    #region Commands

    public ICommand? AddNewCategoryCommand { get; set; }

    public ICommand? EditCategoryCommand { get; set; }

    public ICommand? DeleteCategoryCommand { get; set; }

    #endregion

    public SaveLocationsViewModel(IUnitOfWork unitOfWork, IDownloadFileService downloadFileService, IMapper mapper) :
        base(unitOfWork, downloadFileService, mapper)
    {
        _dbFileExtensions = [];

        FileTypesViewModel = new FileTypesViewModel(unitOfWork, downloadFileService, mapper)
        {
            DependsOnCategory = true
        };

        LoadCategoriesAsync().GetAwaiter();

        AddNewCategoryCommand = ReactiveCommand.Create<Window?>(AddNewCategory);
        EditCategoryCommand = ReactiveCommand.Create<Window?>(EditCategory);
        DeleteCategoryCommand = ReactiveCommand.Create(DeleteCategory);
    }

    private async void AddNewCategory(Window? owner)
    {
        // TODO: Show message box
        try
        {
            if (owner == null)
                return;

            var vm = new AddEditCategoryWindowViewModel(UnitOfWork, DownloadFileService, Mapper)
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
            Console.WriteLine(ex);
        }
    }

    private async void EditCategory(Window? owner)
    {
        // TODO: Show message box
        try
        {
            if (owner == null || SelectedCategory == null)
                return;
            
            var selectedCategoryId = SelectedCategory.Id;
            var vm = new AddEditCategoryWindowViewModel(UnitOfWork, DownloadFileService, Mapper)
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
            Console.WriteLine(ex);
        }
    }

    private async void DeleteCategory()
    {
        // TODO: Show message box
        try
        {
            if (SelectedCategory == null)
                return;

            var category = await UnitOfWork.CategoryRepository
                .GetAsync(where: c => c.Id == SelectedCategory.Id,
                    includeProperties: ["CategorySaveDirectory", "FileExtensions", "DownloadFiles"]);
            
            if (category == null)
                return;
            
            if (category.IsDefault)
                return;
            
            UnitOfWork.CategorySaveDirectoryRepository.Delete(category.CategorySaveDirectory);
            UnitOfWork.CategoryFileExtensionRepository.DeleteAll(category.FileExtensions);
            UnitOfWork.DownloadFileRepository.DeleteAll(category.DownloadFiles);
            UnitOfWork.CategoryRepository.Delete(category);
            await UnitOfWork.SaveAsync();

            await DownloadFileService.LoadFilesAsync();
            await LoadCategoriesAsync();
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
            var categories = await UnitOfWork.CategoryRepository
                .GetAllAsync(includeProperties: ["CategorySaveDirectory"]);

            var categoryViewModels = Mapper.Map<List<CategoryViewModel>>(categories);
            Categories = categoryViewModels.ToObservableCollection();
            SelectedCategory = Categories.FirstOrDefault();

            LoadFileExtensions();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public void LoadFileExtensions()
    {
        if (FileTypesViewModel == null || SelectedCategory == null)
            return;

        FileTypesViewModel.CategoryId = SelectedCategory.Id;
    }
}