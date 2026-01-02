using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.PropertyChanged;
using MapsterMapper;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.CategoryService;

/// <summary>
/// Service for managing categories.
/// </summary>
public class CategoryService : PropertyChangedBase, ICategoryService
{
    #region Private Fields

    /// <summary>
    /// Unit of work service for accessing the database.
    /// </summary>
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Mapper service to map data.
    /// </summary>
    private readonly IMapper _mapper;

    // Backing fields for properties
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

    /// <summary>
    /// Initializes a new instance of CategoryService
    /// </summary>
    /// <param name="unitOfWork">The unit of work for database operations</param>
    /// <param name="mapper">The mapper for object mapping</param>
    public CategoryService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;

        Log.Debug("CategoryService initialized successfully.");
    }

    public async Task LoadCategoriesAsync(bool loadHeaders = true)
    {
        Log.Information("Loading categories...");

        // Get all categories from database with related entities
        var categories = await _unitOfWork
            .CategoryRepository
            .GetAllAsync(includeProperties: ["CategorySaveDirectory", "FileExtensions"]);

        Log.Debug("Retrieved {CategoryCount} categories from database", categories.Count);

        // Find deleted categories and remove them from local collection
        var deletedCategories = Categories
            .Where(vm => !categories.Exists(c => c.Id == vm.Id))
            .ToList();

        Log.Debug("Found {DeletedCount} categories to remove from service", deletedCategories.Count);

        foreach (var deletedCategory in deletedCategories)
        {
            Categories.Remove(deletedCategory);
            Log.Debug("Removed category with ID {CategoryId} from service", deletedCategory.Id);
        }

        // Find new categories and add them to local collection
        var addedCategories = categories
            .Where(c => Categories.All(vm => vm.Id != c.Id))
            .Select(c => _mapper.Map<CategoryViewModel>(c))
            .ToList();

        Log.Debug("Found {AddedCount} new categories to add to service", addedCategories.Count);

        foreach (var addedCategory in addedCategories)
        {
            Categories.Add(addedCategory);
            Log.Debug("Added new category with ID {CategoryId} to service", addedCategory.Id);
        }

        Log.Debug("Updating required data of the existing categories...");

        // Update existing categories with latest data from database
        var existingCategories = Categories
            .Where(vm => !addedCategories.Exists(c => c.Id == vm.Id))
            .ToList();

        foreach (var vm in existingCategories)
        {
            var category = categories.Find(c => c.Id == vm.Id);
            if (category == null)
            {
                Log.Debug("Category with ID {CategoryId} not found in database", vm.Id);
                return;
            }

            Log.Debug("Updating category with ID {CategoryId}", vm.Id);

            var viewModel = _mapper.Map<CategoryViewModel>(category);
            vm.FileExtensions = viewModel.FileExtensions;
            vm.CategorySaveDirectory = viewModel.CategorySaveDirectory;
        }

        // Load category headers if requested
        if (loadHeaders)
        {
            Log.Debug("Loading category headers as requested");
            await LoadCategoryHeadersAsync();
        }

        // Notify subscribers about categories change
        CategoriesChanged?.Invoke(this, EventArgs.Empty);
        Log.Information("Categories loaded successfully. Total categories: {CategoryCount}", Categories.Count);
    }

    public async Task LoadCategoryHeadersAsync()
    {
        Log.Information("Loading category headers...");

        // Get all category headers from database
        var categoryHeaders = await _unitOfWork
            .CategoryHeaderRepository
            .GetAllAsync();

        Log.Debug("Retrieved {HeaderCount} category headers from database", categoryHeaders.Count);

        // Remove deleted category headers from local collection
        var deletedCategoryHeaders = CategoryHeaders
            .Where(vm => !categoryHeaders.Exists(ch => ch.Id == vm.Id))
            .ToList();

        Log.Debug("Found {DeletedHeaderCount} category headers to remove", deletedCategoryHeaders.Count);

        foreach (var deletedCategoryHeader in deletedCategoryHeaders)
        {
            CategoryHeaders.Remove(deletedCategoryHeader);
            Log.Debug("Removed category header with ID {HeaderId}", deletedCategoryHeader.Id);
        }

        // Add new category headers to local collection
        var addedCategoryHeaders = categoryHeaders
            .Where(ch => CategoryHeaders.All(vm => vm.Id != ch.Id))
            .Select(ch => _mapper.Map<CategoryHeaderViewModel>(ch))
            .ToList();

        Log.Debug("Found {AddedHeaderCount} new category headers to add", addedCategoryHeaders.Count);

        foreach (var addedCategoryHeader in addedCategoryHeaders)
        {
            CategoryHeaders.Add(addedCategoryHeader);
            Log.Debug("Added new category header with ID {HeaderId}", addedCategoryHeader.Id);
        }

        // Update categories reference in all category headers
        foreach (var categoryHeader in CategoryHeaders)
        {
            categoryHeader.Categories = Categories;
            Log.Debug("Updated categories reference for category header with ID {HeaderId}", categoryHeader.Id);
        }

        // Notify subscribers about category headers change
        CategoryHeadersChanged?.Invoke(this, EventArgs.Empty);
        Log.Information("Category headers loaded successfully. Total headers: {HeaderCount}", CategoryHeaders.Count);
    }

    public async Task<int> AddNewCategoryAsync(CategoryViewModel? viewModel, bool reloadData = true)
    {
        if (viewModel == null || viewModel.Id > 0)
        {
            Log.Warning("Attempted to add invalid category. ViewModel is null or has existing ID");
            return 0;
        }

        Log.Information("Adding new category: {CategoryTitle}", viewModel.Title);

        var category = _mapper.Map<Category>(viewModel);
        await _unitOfWork.CategoryRepository.AddAsync(category);
        await _unitOfWork.SaveAsync();

        Log.Debug("New category added to database with ID: {CategoryId}", category.Id);

        if (reloadData)
        {
            Log.Debug("Reloading categories after adding new category");
            await LoadCategoriesAsync(loadHeaders: false);
        }

        Log.Information("Category added successfully with ID: {CategoryId}", category.Id);
        return category.Id;
    }

    public async Task UpdateCategoryAsync(CategoryViewModel? viewModel, bool reloadData = true)
    {
        if (viewModel == null)
        {
            Log.Warning("Attempted to update null category");
            return;
        }

        var category = Categories.FirstOrDefault(c => c.Id == viewModel.Id);
        if (category == null)
        {
            Log.Warning("Category with ID {CategoryId} not found for update", viewModel.Id);
            return;
        }

        Log.Information("Updating category with ID: {CategoryId}", viewModel.Id);

        var categoryInDb = _mapper.Map<Category>(category);
        await _unitOfWork.CategoryRepository.UpdateAsync(categoryInDb);
        await _unitOfWork.SaveAsync();

        Log.Debug("Category with ID {CategoryId} updated in database", viewModel.Id);

        if (reloadData)
        {
            Log.Debug("Reloading categories after updating category");
            await LoadCategoriesAsync(loadHeaders: false);
        }

        Log.Information("Category with ID {CategoryId} updated successfully", viewModel.Id);
    }

    public async Task DeleteCategoryAsync(CategoryViewModel? viewModel, bool reloadData = true)
    {
        if (viewModel == null)
        {
            Log.Warning("Attempted to delete null category");
            return;
        }

        var category = Categories.FirstOrDefault(c => c.Id == viewModel.Id);
        if (category == null)
        {
            Log.Warning("Category with ID {CategoryId} not found for deletion", viewModel.Id);
            return;
        }

        Log.Information("Deleting category with ID: {CategoryId}, Title: {CategoryTitle}", viewModel.Id, viewModel.Title);

        var categoryInDb = await _unitOfWork
            .CategoryRepository
            .GetAsync(where: c => c.Id == category.Id, includeProperties: ["CategorySaveDirectory", "FileExtensions", "DownloadFiles"]);

        if (categoryInDb == null)
        {
            Log.Warning("Category with ID {CategoryId} not found in database", category.Id);
            return;
        }

        Log.Debug("Deleting related save directories for category ID {CategoryId}", category.Id);
        await _unitOfWork.CategorySaveDirectoryRepository.DeleteAsync(categoryInDb.CategorySaveDirectory);

        Log.Debug("Deleting related file extensions for category ID {CategoryId}", category.Id);
        await _unitOfWork.CategoryFileExtensionRepository.DeleteAllAsync(categoryInDb.FileExtensions);

        // Handle download files related to this category
        if (categoryInDb.DownloadFiles.Count > 0)
        {
            Log.Information("Category has {DownloadFileCount} download files. Asking user for action.", categoryInDb.DownloadFiles.Count);

            var result = await DialogBoxManager.ShowWarningDialogAsync("Delete category",
                "Would you also like to delete the downloaded files in this category?",
                DialogButtons.YesNo);

            if (result == DialogResult.Yes)
            {
                Log.Debug("User chose to delete download files for category ID {CategoryId}", category.Id);
                await _unitOfWork.DownloadFileRepository.DeleteAllAsync(categoryInDb.DownloadFiles);
            }
            else
            {
                Log.Debug("User chose to keep download files. Moving to general category.");
                var generalCategory = Categories.FirstOrDefault(c => c.Title.Equals(Constants.GeneralCategoryTitle, StringComparison.OrdinalIgnoreCase));

                if (generalCategory == null)
                {
                    Log.Error("General category not found. Download files will be deleted.");
                    await DialogBoxManager.ShowDangerDialogAsync("Delete category",
                        $"We encountered an error and the '{categoryInDb.Title}' category could not be found. As a result, your downloads have been deleted. We sincerely apologize for this inconvenience.",
                        DialogButtons.Ok);

                    await _unitOfWork.DownloadFileRepository.DeleteAllAsync(categoryInDb.DownloadFiles);
                }
                else
                {
                    Log.Debug("Moving {DownloadFileCount} download files to general category ID {GeneralCategoryId}", categoryInDb.DownloadFiles.Count, generalCategory.Id);

                    foreach (var downloadFile in categoryInDb.DownloadFiles)
                        downloadFile.CategoryId = generalCategory.Id;
                }
            }
        }

        Log.Debug("Deleting category with ID {CategoryId} from database", category.Id);
        await _unitOfWork.CategoryRepository.DeleteAsync(categoryInDb);
        await _unitOfWork.SaveAsync();

        if (reloadData)
        {
            Log.Debug("Reloading categories after deletion");
            await LoadCategoriesAsync(loadHeaders: false);
        }

        Log.Information("Category with ID {CategoryId} deleted successfully", category.Id);
    }

    public async Task DeleteFileExtensionAsync(CategoryViewModel? viewModel, CategoryFileExtensionViewModel? fileExtension, bool reloadData = true)
    {
        if (viewModel == null || fileExtension == null)
        {
            Log.Warning("Attempted to delete file extension with null category or file extension");
            return;
        }

        var category = Categories.FirstOrDefault(c => c.Id == viewModel.Id);
        if (category == null)
        {
            Log.Warning("Category with ID {CategoryId} not found for file extension deletion", viewModel.Id);
            return;
        }

        Log.Information("Deleting file extension with ID {FileExtensionId} from category ID {CategoryId}", fileExtension.Id, category.Id);

        var categoryInDb = await _unitOfWork
            .CategoryRepository
            .GetAsync(where: c => c.Id == category.Id);

        if (categoryInDb == null)
        {
            Log.Warning("Category with ID {CategoryId} not found in database", category.Id);
            return;
        }

        var fileExtensionInDb = await _unitOfWork
            .CategoryFileExtensionRepository
            .GetAsync(where: fe => fe.Id == fileExtension.Id && fe.CategoryId == categoryInDb.Id);

        if (fileExtensionInDb == null)
        {
            Log.Warning("File extension with ID {FileExtensionId} not found in category ID {CategoryId}", fileExtension.Id, category.Id);
            return;
        }

        fileExtensionInDb = _mapper.Map<CategoryFileExtension>(fileExtension);

        await _unitOfWork.CategoryFileExtensionRepository.DeleteAsync(fileExtensionInDb);
        await _unitOfWork.SaveAsync();

        Log.Debug("File extension with ID {FileExtensionId} deleted successfully", fileExtension.Id);

        if (reloadData)
        {
            Log.Debug("Reloading categories after file extension deletion");
            await LoadCategoriesAsync(loadHeaders: false);
        }

        Log.Information("File extension deleted successfully from category ID {CategoryId}", category.Id);
    }

    public async Task DeleteAllFileExtensionsAsync(CategoryViewModel? viewModel, bool reloadData = true)
    {
        if (viewModel == null)
        {
            Log.Warning("Attempted to delete all file extensions from null category");
            return;
        }

        var category = Categories.FirstOrDefault(c => c.Id == viewModel.Id);
        if (category == null)
        {
            Log.Warning("Category with ID {CategoryId} not found for deleting all file extensions", viewModel.Id);
            return;
        }

        Log.Information("Deleting all file extensions from category ID {CategoryId}", category.Id);

        var fileExtensions = _mapper.Map<List<CategoryFileExtension>>(category.FileExtensions);
        await _unitOfWork.CategoryFileExtensionRepository.DeleteAllAsync(fileExtensions);
        await _unitOfWork.SaveAsync();

        Log.Debug("Deleted {FileExtensionCount} file extensions from category ID {CategoryId}", fileExtensions.Count, category.Id);

        if (reloadData)
        {
            Log.Debug("Reloading categories after deleting all file extensions");
            await LoadCategoriesAsync(loadHeaders: false);
        }

        Log.Information("All file extensions deleted successfully from category ID {CategoryId}", category.Id);
    }

    public async Task AddFileExtensionAsync(CategoryViewModel? viewModel, CategoryFileExtensionViewModel? fileExtension, bool reloadData = true)
    {
        if (viewModel == null || fileExtension == null || fileExtension.Id > 0)
        {
            Log.Warning("Attempted to add invalid file extension to category");
            return;
        }

        var category = Categories.FirstOrDefault(c => c.Id == viewModel.Id);
        if (category == null)
        {
            Log.Warning("Category with ID {CategoryId} not found for adding file extension", viewModel.Id);
            return;
        }

        Log.Information("Adding file extension to category ID {CategoryId}", category.Id);

        var categoryInDb = await _unitOfWork
            .CategoryRepository
            .GetAsync(where: c => c.Id == category.Id);

        if (categoryInDb == null)
        {
            Log.Warning("Category with ID {CategoryId} not found in database", category.Id);
            return;
        }

        var categoryFileExtension = _mapper.Map<CategoryFileExtension>(fileExtension);
        categoryFileExtension.CategoryId = categoryInDb.Id;

        await _unitOfWork.CategoryFileExtensionRepository.AddAsync(categoryFileExtension);
        await _unitOfWork.SaveAsync();

        Log.Debug("File extension added successfully to category ID {CategoryId} with new ID {FileExtensionId}", category.Id, categoryFileExtension.Id);

        if (reloadData)
        {
            Log.Debug("Reloading categories after adding file extension");
            await LoadCategoriesAsync(loadHeaders: false);
        }

        Log.Information("File extension added successfully to category ID {CategoryId}", category.Id);
    }

    public async Task AddFileExtensionsAsync(CategoryViewModel? viewModel, List<CategoryFileExtensionViewModel>? fileExtensions, bool reloadData = true)
    {
        if (viewModel == null || fileExtensions == null || fileExtensions.Count == 0)
        {
            Log.Warning("Attempted to add null or empty file extensions list to category");
            return;
        }

        var category = Categories.FirstOrDefault(c => c.Id == viewModel.Id);
        if (category == null)
        {
            Log.Warning("Category with ID {CategoryId} not found for adding file extensions", viewModel.Id);
            return;
        }

        Log.Information("Adding {FileExtensionCount} file extensions to category ID {CategoryId}",
            fileExtensions.Count, category.Id);

        // Filter out file extensions that already have IDs (already exist)
        fileExtensions = fileExtensions
            .Where(fe => fe.Id == 0)
            .ToList();

        if (fileExtensions.Count == 0)
        {
            Log.Warning("No valid file extensions to add (all have existing IDs)");
            return;
        }

        Log.Debug("Filtered to {ValidFileExtensionCount} new file extensions to add", fileExtensions.Count);

        var categoryInDb = await _unitOfWork
            .CategoryRepository
            .GetAsync(where: c => c.Id == category.Id);

        if (categoryInDb == null)
        {
            Log.Warning("Category with ID {CategoryId} not found in database", category.Id);
            return;
        }

        var categoryFileExtensions = _mapper.Map<List<CategoryFileExtension>>(fileExtensions);
        categoryFileExtensions = categoryFileExtensions
            .ConvertAll(fe =>
            {
                fe.CategoryId = category.Id;
                return fe;
            });

        await _unitOfWork.CategoryFileExtensionRepository.AddRangeAsync(categoryFileExtensions);
        await _unitOfWork.SaveAsync();

        Log.Debug("Successfully added {AddedCount} file extensions to category ID {CategoryId}", categoryFileExtensions.Count, category.Id);

        if (reloadData)
        {
            Log.Debug("Reloading categories after adding multiple file extensions");
            await LoadCategoriesAsync(loadHeaders: false);
        }

        Log.Information("Successfully added {AddedCount} file extensions to category ID {CategoryId}", categoryFileExtensions.Count, category.Id);
    }

    public async Task UpdateFileExtensionAsync(CategoryViewModel? viewModel, CategoryFileExtensionViewModel? fileExtension, bool reloadData = true)
    {
        if (viewModel == null || fileExtension is not { Id: > 0 })
        {
            Log.Warning("Attempted to update invalid file extension");
            return;
        }

        var category = Categories.FirstOrDefault(c => c.Id == viewModel.Id);
        if (category == null)
        {
            Log.Warning("Category with ID {CategoryId} not found for file extension update", viewModel.Id);
            return;
        }

        Log.Information("Updating file extension with ID {FileExtensionId} in category ID {CategoryId}", fileExtension.Id, category.Id);

        var categoryInDb = await _unitOfWork
            .CategoryRepository
            .GetAsync(where: c => c.Id == category.Id);

        if (categoryInDb == null)
        {
            Log.Warning("Category with ID {CategoryId} not found in database", category.Id);
            return;
        }

        var fileExtensionInDb = await _unitOfWork
            .CategoryFileExtensionRepository
            .GetAsync(where: fe => fe.Id == fileExtension.Id);

        if (fileExtensionInDb == null)
        {
            Log.Warning("File extension with ID {FileExtensionId} not found in database", fileExtension.Id);
            return;
        }

        fileExtensionInDb = _mapper.Map<CategoryFileExtension>(fileExtension);
        fileExtensionInDb.CategoryId = categoryInDb.Id;

        await _unitOfWork.CategoryFileExtensionRepository.UpdateAsync(fileExtensionInDb);
        await _unitOfWork.SaveAsync();

        Log.Debug("File extension with ID {FileExtensionId} updated successfully", fileExtension.Id);

        if (reloadData)
        {
            Log.Debug("Reloading categories after file extension update");
            await LoadCategoriesAsync(loadHeaders: false);
        }

        Log.Information("File extension with ID {FileExtensionId} updated successfully in category ID {CategoryId}", fileExtension.Id, category.Id);
    }

    public async Task AddSaveDirectoryAsync(CategoryViewModel? viewModel, CategorySaveDirectoryViewModel? saveDirectory, bool reloadData = true)
    {
        if (viewModel == null || saveDirectory == null || saveDirectory.Id > 0)
        {
            Log.Warning("Attempted to add invalid save directory to category");
            return;
        }

        var category = Categories.FirstOrDefault(c => c.Id == viewModel.Id);
        if (category == null)
        {
            Log.Warning("Category with ID {CategoryId} not found for adding save directory", viewModel.Id);
            return;
        }

        Log.Information("Adding save directory to category ID {CategoryId}", category.Id);

        var categoryInDb = await _unitOfWork
            .CategoryRepository
            .GetAsync(where: c => c.Id == category.Id);

        if (categoryInDb == null)
        {
            Log.Warning("Category with ID {CategoryId} not found in database", category.Id);
            return;
        }

        var saveDirectoryInDb = _mapper.Map<CategorySaveDirectory>(saveDirectory);
        saveDirectoryInDb.CategoryId = categoryInDb.Id;

        await _unitOfWork.CategorySaveDirectoryRepository.AddAsync(saveDirectoryInDb);
        await _unitOfWork.SaveAsync();

        Log.Debug("Save directory added successfully to category ID {CategoryId} with new ID {SaveDirectoryId}", category.Id, saveDirectoryInDb.Id);

        if (reloadData)
        {
            Log.Debug("Reloading categories after adding save directory");
            await LoadCategoriesAsync(loadHeaders: false);
        }

        Log.Information("Save directory added successfully to category ID {CategoryId}", category.Id);
    }

    public async Task UpdateSaveDirectoryAsync(CategoryViewModel? viewModel, CategorySaveDirectoryViewModel? saveDirectory, bool reloadData = true)
    {
        if (viewModel == null || saveDirectory == null)
        {
            Log.Warning("Attempted to update save directory with null category or save directory");
            return;
        }

        var category = Categories.FirstOrDefault(c => c.Id == viewModel.Id);
        if (category == null)
        {
            Log.Warning("Category with ID {CategoryId} not found for save directory update", viewModel.Id);
            return;
        }

        Log.Information("Updating save directory with ID {SaveDirectoryId} for category ID {CategoryId}", saveDirectory.Id, category.Id);

        var categoryInDb = await _unitOfWork
            .CategoryRepository
            .GetAsync(where: c => c.Id == category.Id);

        if (categoryInDb == null)
        {
            Log.Warning("Category with ID {CategoryId} not found in database", category.Id);
            return;
        }

        var saveDirectoryInDb = await _unitOfWork
            .CategorySaveDirectoryRepository
            .GetAsync(where: sd => sd.Id == saveDirectory.Id && sd.CategoryId == categoryInDb.Id);

        if (saveDirectoryInDb == null)
        {
            Log.Warning("Save directory with ID {SaveDirectoryId} not found for category ID {CategoryId}", saveDirectory.Id, category.Id);
            return;
        }

        saveDirectoryInDb = _mapper.Map<CategorySaveDirectory>(saveDirectory);
        saveDirectoryInDb.CategoryId = category.Id;

        await _unitOfWork.CategorySaveDirectoryRepository.UpdateAsync(saveDirectoryInDb);
        await _unitOfWork.SaveAsync();

        Log.Debug("Save directory with ID {SaveDirectoryId} updated successfully", saveDirectory.Id);

        if (reloadData)
        {
            Log.Debug("Reloading categories after save directory update");
            await LoadCategoriesAsync(loadHeaders: false);
        }

        Log.Information("Save directory with ID {SaveDirectoryId} updated successfully for category ID {CategoryId}", saveDirectory.Id, category.Id);
    }
}