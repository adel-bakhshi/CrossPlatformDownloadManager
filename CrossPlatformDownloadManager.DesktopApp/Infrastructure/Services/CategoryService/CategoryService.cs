using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.PropertyChanged;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.CategoryService;

public class CategoryService : PropertyChangedBase, ICategoryService
{
    #region Private Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    private ObservableCollection<CategoryViewModel> _categories = [];
    private ObservableCollection<CategoryHeaderViewModel> _categoryHeaders = [];

    #endregion

    #region Properties

    public ObservableCollection<CategoryViewModel> Categories
    {
        get => _categories;
        // ReSharper disable once UnusedMember.Local
        private set => SetField(ref _categories, value);
    }

    public ObservableCollection<CategoryHeaderViewModel> CategoryHeaders
    {
        get => _categoryHeaders;
        // ReSharper disable once UnusedMember.Local
        private set => SetField(ref _categoryHeaders, value);
    }

    #endregion

    #region Events

    public event EventHandler? CategoriesChanged;
    public event EventHandler? CategoryHeadersChanged;

    #endregion

    public CategoryService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task LoadCategoriesAsync(bool loadHeaders = true)
    {
        // Get all categories
        var categories = await _unitOfWork
            .CategoryRepository
            .GetAllAsync(includeProperties: ["CategorySaveDirectory", "FileExtensions"]);

        // Find deleted categories and remove them
        var deletedCategories = Categories
            .Where(vm => !categories.Exists(c => c.Id == vm.Id))
            .ToList();

        foreach (var deletedCategory in deletedCategories)
            Categories.Remove(deletedCategory);

        // Find new categories and add them
        var addedCategories = categories
            .Where(c => Categories.All(vm => vm.Id != c.Id))
            .Select(c => _mapper.Map<CategoryViewModel>(c))
            .ToList();

        foreach (var addedCategory in addedCategories)
            Categories.Add(addedCategory);

        // For remains categories, update them
        Categories
            .Where(vm => !addedCategories.Exists(c => c.Id == vm.Id))
            .ToList()
            .ForEach(vm =>
            {
                var category = categories.Find(c => c.Id == vm.Id);
                if (category == null)
                    return;

                var viewModel = _mapper.Map<CategoryViewModel>(category);
                vm.FileExtensions = viewModel.FileExtensions;
                vm.CategorySaveDirectory = viewModel.CategorySaveDirectory;
            });

        // Load headers
        if (loadHeaders)
            await LoadCategoryHeadersAsync();

        // Raise CategoriesChanged event
        CategoriesChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task LoadCategoryHeadersAsync()
    {
        // Get all category headers
        var categoryHeaders = await _unitOfWork
            .CategoryHeaderRepository
            .GetAllAsync();

        // Find deleted category headers and remove them
        var deletedCategoryHeaders = CategoryHeaders
            .Where(vm => !categoryHeaders.Exists(ch => ch.Id == vm.Id))
            .ToList();

        foreach (var deletedCategoryHeader in deletedCategoryHeaders)
            CategoryHeaders.Remove(deletedCategoryHeader);

        // Find new category headers and add them
        var addedCategoryHeaders = categoryHeaders
            .Where(ch => CategoryHeaders.All(vm => vm.Id != ch.Id))
            .Select(ch => _mapper.Map<CategoryHeaderViewModel>(ch))
            .ToList();

        foreach (var addedCategoryHeader in addedCategoryHeaders)
            CategoryHeaders.Add(addedCategoryHeader);

        // Replace categories in category headers
        foreach (var categoryHeader in CategoryHeaders)
            categoryHeader.Categories = Categories;

        // Raise CategoryHeadersChanged event
        CategoryHeadersChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task<int> AddNewCategoryAsync(CategoryViewModel? viewModel)
    {
        if (viewModel == null || viewModel.Id > 0)
            return 0;

        var category = _mapper.Map<Category>(viewModel);
        await _unitOfWork.CategoryRepository.AddAsync(category);
        await _unitOfWork.SaveAsync();

        await LoadCategoriesAsync(loadHeaders: false);
        return category.Id;
    }

    public async Task UpdateCategoryAsync(CategoryViewModel? viewModel)
    {
        var category = Categories.FirstOrDefault(c => c.Id == viewModel?.Id);
        if (category == null)
            return;

        var categoryInDb = _mapper.Map<Category>(category);
        await _unitOfWork.CategoryRepository.UpdateAsync(categoryInDb);
        await _unitOfWork.SaveAsync();

        await LoadCategoriesAsync(loadHeaders: false);
    }

    public async Task DeleteCategoryAsync(CategoryViewModel? viewModel)
    {
        var category = Categories.FirstOrDefault(c => c.Id == viewModel?.Id);
        if (category == null)
            return;

        var categoryInDb = await _unitOfWork
            .CategoryRepository
            .GetAsync(where: c => c.Id == category.Id, includeProperties: ["CategorySaveDirectory", "FileExtensions", "DownloadFiles"]);

        if (categoryInDb == null)
            return;

        // Delete all category save directories related to this category
        await _unitOfWork.CategorySaveDirectoryRepository.DeleteAsync(categoryInDb.CategorySaveDirectory);
        // Delete all category file extensions related to this category
        await _unitOfWork.CategoryFileExtensionRepository.DeleteAllAsync(categoryInDb.FileExtensions);

        // Let user choose for download files related to this category 
        if (categoryInDb.DownloadFiles.Count > 0)
        {
            var result = await DialogBoxManager.ShowWarningDialogAsync("Delete category",
                "Would you also like to delete the downloaded files in this category?",
                DialogButtons.YesNo);

            if (result == DialogResult.Yes)
            {
                // Delete all download files related to this category
                await _unitOfWork.DownloadFileRepository.DeleteAllAsync(categoryInDb.DownloadFiles);
            }
            else
            {
                var generalCategory = Categories.FirstOrDefault(c => c.Title.Equals(Constants.GeneralCategoryTitle, StringComparison.OrdinalIgnoreCase));
                if (generalCategory == null)
                {
                    await DialogBoxManager.ShowDangerDialogAsync("Delete category",
                        $"We encountered an error and the '{categoryInDb.Title}' category could not be found. As a result, your downloads have been deleted. We sincerely apologize for this inconvenience.",
                        DialogButtons.Ok);

                    // Delete all download files related to this category
                    await _unitOfWork.DownloadFileRepository.DeleteAllAsync(categoryInDb.DownloadFiles);
                }
                else
                {
                    // Change CategoryId for all download files related to this category
                    foreach (var downloadFile in categoryInDb.DownloadFiles)
                        downloadFile.CategoryId = generalCategory.Id;
                }
            }
        }

        // Delete category
        await _unitOfWork.CategoryRepository.DeleteAsync(categoryInDb);
        await _unitOfWork.SaveAsync();

        await LoadCategoriesAsync(loadHeaders: false);
    }

    public async Task DeleteFileExtensionAsync(CategoryViewModel? viewModel, CategoryFileExtensionViewModel? fileExtension)
    {
        var category = Categories.FirstOrDefault(c => c.Id == viewModel?.Id);
        if (category == null || fileExtension == null)
            return;

        var categoryInDb = await _unitOfWork
            .CategoryRepository
            .GetAsync(where: c => c.Id == category.Id);

        if (categoryInDb == null)
            return;

        var fileExtensionInDb = await _unitOfWork
            .CategoryFileExtensionRepository
            .GetAsync(where: fe => fe.Id == fileExtension.Id && fe.CategoryId == categoryInDb.Id);

        if (fileExtensionInDb == null)
            return;

        fileExtensionInDb = _mapper.Map<CategoryFileExtension>(fileExtension);

        await _unitOfWork.CategoryFileExtensionRepository.DeleteAsync(fileExtensionInDb);
        await _unitOfWork.SaveAsync();

        await LoadCategoriesAsync(loadHeaders: false);
    }

    public async Task DeleteAllFileExtensionsAsync(CategoryViewModel? viewModel)
    {
        var category = Categories.FirstOrDefault(c => c.Id == viewModel?.Id);
        if (category == null)
            return;

        var fileExtensions = _mapper.Map<List<CategoryFileExtension>>(category.FileExtensions);
        await _unitOfWork.CategoryFileExtensionRepository.DeleteAllAsync(fileExtensions);
        await _unitOfWork.SaveAsync();

        await LoadCategoriesAsync(loadHeaders: false);
    }

    public async Task AddFileExtensionAsync(CategoryViewModel? viewModel, CategoryFileExtensionViewModel? fileExtension)
    {
        var category = Categories.FirstOrDefault(c => c.Id == viewModel?.Id);
        if (category == null || fileExtension == null || fileExtension.Id > 0)
            return;

        var categoryInDb = await _unitOfWork
            .CategoryRepository
            .GetAsync(where: c => c.Id == category.Id);

        if (categoryInDb == null)
            return;

        var categoryFileExtension = _mapper.Map<CategoryFileExtension>(fileExtension);
        categoryFileExtension.CategoryId = categoryInDb.Id;

        await _unitOfWork.CategoryFileExtensionRepository.AddAsync(categoryFileExtension);
        await _unitOfWork.SaveAsync();

        await LoadCategoriesAsync(loadHeaders: false);
    }

    public async Task AddFileExtensionsAsync(CategoryViewModel? viewModel, List<CategoryFileExtensionViewModel>? fileExtensions)
    {
        var category = Categories.FirstOrDefault(c => c.Id == viewModel?.Id);
        if (category == null || fileExtensions == null || fileExtensions.Count == 0)
            return;

        fileExtensions = fileExtensions
            .Where(fe => fe.Id == 0)
            .ToList();

        if (fileExtensions.Count == 0)
            return;

        var categoryInDb = await _unitOfWork
            .CategoryRepository
            .GetAsync(where: c => c.Id == category.Id);

        if (categoryInDb == null)
            return;

        var categoryFileExtensions = _mapper.Map<List<CategoryFileExtension>>(fileExtensions);
        categoryFileExtensions = categoryFileExtensions
            .ConvertAll(fe =>
            {
                fe.CategoryId = category.Id;
                return fe;
            });

        await _unitOfWork.CategoryFileExtensionRepository.AddRangeAsync(categoryFileExtensions);
        await _unitOfWork.SaveAsync();

        await LoadCategoriesAsync(loadHeaders: false);
    }

    public async Task UpdateFileExtensionAsync(CategoryViewModel? viewModel, CategoryFileExtensionViewModel? fileExtension)
    {
        var category = Categories.FirstOrDefault(c => c.Id == viewModel?.Id);
        if (category == null || fileExtension is not { Id: > 0 })
            return;

        var categoryInDb = await _unitOfWork
            .CategoryRepository
            .GetAsync(where: c => c.Id == category.Id);

        if (categoryInDb == null)
            return;

        var fileExtensionInDb = await _unitOfWork
            .CategoryFileExtensionRepository
            .GetAsync(where: fe => fe.Id == fileExtension.Id);

        if (fileExtensionInDb == null)
            return;

        fileExtensionInDb = _mapper.Map<CategoryFileExtension>(fileExtension);
        fileExtensionInDb.CategoryId = categoryInDb.Id;

        await _unitOfWork.CategoryFileExtensionRepository.UpdateAsync(fileExtensionInDb);
        await _unitOfWork.SaveAsync();

        await LoadCategoriesAsync(loadHeaders: false);
    }

    public async Task AddSaveDirectoryAsync(CategoryViewModel? viewModel, CategorySaveDirectoryViewModel? saveDirectory)
    {
        var category = Categories.FirstOrDefault(c => c.Id == viewModel?.Id);
        if (category == null || saveDirectory == null || saveDirectory.Id > 0)
            return;

        var categoryInDb = await _unitOfWork
            .CategoryRepository
            .GetAsync(where: c => c.Id == category.Id);

        if (categoryInDb == null)
            return;

        var saveDirectoryInDb = _mapper.Map<CategorySaveDirectory>(saveDirectory);
        saveDirectoryInDb.CategoryId = categoryInDb.Id;

        await _unitOfWork.CategorySaveDirectoryRepository.AddAsync(saveDirectoryInDb);
        await _unitOfWork.SaveAsync();

        await LoadCategoriesAsync(loadHeaders: false);
    }

    public async Task UpdateSaveDirectoryAsync(CategoryViewModel? viewModel, CategorySaveDirectoryViewModel? saveDirectory)
    {
        var category = Categories.FirstOrDefault(c => c.Id == viewModel?.Id);
        if (category == null || saveDirectory == null)
            return;

        var categoryInDb = await _unitOfWork
            .CategoryRepository
            .GetAsync(where: c => c.Id == category.Id);

        if (categoryInDb == null)
            return;

        var saveDirectoryInDb = await _unitOfWork
            .CategorySaveDirectoryRepository
            .GetAsync(where: sd => sd.Id == saveDirectory.Id && sd.CategoryId == categoryInDb.Id);

        if (saveDirectoryInDb == null)
            return;

        saveDirectoryInDb = _mapper.Map<CategorySaveDirectory>(saveDirectory);
        saveDirectoryInDb.CategoryId = category.Id;

        await _unitOfWork.CategorySaveDirectoryRepository.UpdateAsync(saveDirectoryInDb);
        await _unitOfWork.SaveAsync();

        await LoadCategoriesAsync(loadHeaders: false);
    }
}