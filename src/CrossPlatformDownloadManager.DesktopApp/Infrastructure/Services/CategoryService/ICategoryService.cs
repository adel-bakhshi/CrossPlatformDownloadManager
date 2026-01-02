using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CrossPlatformDownloadManager.Data.ViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.CategoryService;

/// <summary>
/// Category service interface.
/// </summary>
public interface ICategoryService
{
    #region Properties

    /// <summary>
    /// Gets a value that indicates the categories of the application.
    /// </summary>
    ObservableCollection<CategoryViewModel> Categories { get; }

    /// <summary>
    /// Gets a value that indicates the category headers of the application.
    /// </summary>
    ObservableCollection<CategoryHeaderViewModel> CategoryHeaders { get; }

    #endregion

    #region Events

    /// <summary>
    /// Event handler that is invoked when the categories are changed.
    /// </summary>
    event EventHandler? CategoriesChanged;

    /// <summary>
    /// Event handler that is invoked when the category headers are changed.
    /// </summary>
    event EventHandler? CategoryHeadersChanged;

    #endregion

    /// <summary>
    /// Asynchronously loads all categories from the data source
    /// </summary>
    /// <param name="loadHeaders">If true, category headers will also be loaded</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task LoadCategoriesAsync(bool loadHeaders = true);

    /// <summary>
    /// Asynchronously loads only category headers from the data source
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task LoadCategoryHeadersAsync();

    /// <summary>
    /// Asynchronously adds a new category to the system
    /// </summary>
    /// <param name="viewModel">The category view model containing category data</param>
    /// <param name="reloadData">If true, data will be reloaded after adding</param>
    /// <returns>A task that returns the ID of the newly created category</returns>
    Task<int> AddNewCategoryAsync(CategoryViewModel? viewModel, bool reloadData = true);

    /// <summary>
    /// Asynchronously updates an existing category
    /// </summary>
    /// <param name="viewModel">The category view model with updated data</param>
    /// <param name="reloadData">If true, data will be reloaded after updating</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task UpdateCategoryAsync(CategoryViewModel? viewModel, bool reloadData = true);

    /// <summary>
    /// Asynchronously deletes a category from the system
    /// </summary>
    /// <param name="viewModel">The category view model to delete</param>
    /// <param name="reloadData">If true, data will be reloaded after deletion</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task DeleteCategoryAsync(CategoryViewModel? viewModel, bool reloadData = true);

    /// <summary>
    /// Asynchronously deletes a file extension from a category
    /// </summary>
    /// <param name="viewModel">The category view model</param>
    /// <param name="fileExtension">The file extension view model to delete</param>
    /// <param name="reloadData">If true, data will be reloaded after deletion</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task DeleteFileExtensionAsync(CategoryViewModel? viewModel, CategoryFileExtensionViewModel? fileExtension, bool reloadData = true);

    /// <summary>
    /// Asynchronously deletes all file extensions from a category
    /// </summary>
    /// <param name="viewModel">The category view model</param>
    /// <param name="reloadData">If true, data will be reloaded after deletion</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task DeleteAllFileExtensionsAsync(CategoryViewModel? viewModel, bool reloadData = true);

    /// <summary>
    /// Asynchronously adds a file extension to a category
    /// </summary>
    /// <param name="viewModel">The category view model</param>
    /// <param name="fileExtension">The file extension view model to add</param>
    /// <param name="reloadData">If true, data will be reloaded after adding</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task AddFileExtensionAsync(CategoryViewModel? viewModel, CategoryFileExtensionViewModel? fileExtension, bool reloadData = true);

    /// <summary>
    /// Asynchronously adds multiple file extensions to a category
    /// </summary>
    /// <param name="viewModel">The category view model</param>
    /// <param name="fileExtensions">The list of file extension view models to add</param>
    /// <param name="reloadData">If true, data will be reloaded after adding</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task AddFileExtensionsAsync(CategoryViewModel? viewModel, List<CategoryFileExtensionViewModel>? fileExtensions, bool reloadData = true);

    /// <summary>
    /// Asynchronously updates a file extension in a category
    /// </summary>
    /// <param name="viewModel">The category view model</param>
    /// <param name="fileExtension">The file extension view model with updated data</param>
    /// <param name="reloadData">If true, data will be reloaded after updating</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task UpdateFileExtensionAsync(CategoryViewModel? viewModel, CategoryFileExtensionViewModel? fileExtension, bool reloadData = true);

    /// <summary>
    /// Asynchronously adds a save directory to a category
    /// </summary>
    /// <param name="viewModel">The category view model</param>
    /// <param name="saveDirectory">The save directory view model to add</param>
    /// <param name="reloadData">If true, data will be reloaded after adding</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task AddSaveDirectoryAsync(CategoryViewModel? viewModel, CategorySaveDirectoryViewModel? saveDirectory, bool reloadData = true);

    /// <summary>
    /// Asynchronously updates a save directory in a category
    /// </summary>
    /// <param name="viewModel">The category view model</param>
    /// <param name="saveDirectory">The save directory view model with updated data</param>
    /// <param name="reloadData">If true, data will be reloaded after updating</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task UpdateSaveDirectoryAsync(CategoryViewModel? viewModel, CategorySaveDirectoryViewModel? saveDirectory, bool reloadData = true);
}