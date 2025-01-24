using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CrossPlatformDownloadManager.Data.ViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.CategoryService;

public interface ICategoryService
{
    #region Properties

    ObservableCollection<CategoryViewModel> Categories { get; }
    ObservableCollection<CategoryHeaderViewModel> CategoryHeaders { get; }

    #endregion

    #region Events

    event EventHandler? CategoriesChanged;
    event EventHandler? CategoryHeadersChanged;

    #endregion

    Task LoadCategoriesAsync(bool loadHeaders = true);

    Task LoadCategoryHeadersAsync();

    Task<int> AddNewCategoryAsync(CategoryViewModel? viewModel, bool reloadData = true);

    Task UpdateCategoryAsync(CategoryViewModel? viewModel, bool reloadData = true);

    Task DeleteCategoryAsync(CategoryViewModel? viewModel, bool reloadData = true);

    Task DeleteFileExtensionAsync(CategoryViewModel? viewModel, CategoryFileExtensionViewModel? fileExtension, bool reloadData = true);
    
    Task DeleteAllFileExtensionsAsync(CategoryViewModel? viewModel, bool reloadData = true);

    Task AddFileExtensionAsync(CategoryViewModel? viewModel, CategoryFileExtensionViewModel? fileExtension, bool reloadData = true);
    
    Task AddFileExtensionsAsync(CategoryViewModel? viewModel, List<CategoryFileExtensionViewModel>? fileExtensions, bool reloadData = true);

    Task UpdateFileExtensionAsync(CategoryViewModel? viewModel, CategoryFileExtensionViewModel? fileExtension, bool reloadData = true);

    Task AddSaveDirectoryAsync(CategoryViewModel? viewModel, CategorySaveDirectoryViewModel? saveDirectory, bool reloadData = true);

    Task UpdateSaveDirectoryAsync(CategoryViewModel? viewModel, CategorySaveDirectoryViewModel? saveDirectory, bool reloadData = true);
}