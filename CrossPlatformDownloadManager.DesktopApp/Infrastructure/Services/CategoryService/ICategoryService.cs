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

    Task<int> AddNewCategoryAsync(CategoryViewModel? viewModel);

    Task UpdateCategoryAsync(CategoryViewModel? viewModel);

    Task DeleteCategoryAsync(CategoryViewModel? viewModel);

    Task DeleteFileExtensionAsync(CategoryViewModel? viewModel, CategoryFileExtensionViewModel? fileExtension);
    
    Task DeleteAllFileExtensionsAsync(CategoryViewModel? viewModel);

    Task AddFileExtensionAsync(CategoryViewModel? viewModel, CategoryFileExtensionViewModel? fileExtension);
    
    Task AddFileExtensionsAsync(CategoryViewModel? viewModel, List<CategoryFileExtensionViewModel>? fileExtensions);

    Task UpdateFileExtensionAsync(CategoryViewModel? viewModel, CategoryFileExtensionViewModel? fileExtension);

    Task AddSaveDirectoryAsync(CategoryViewModel? viewModel, CategorySaveDirectoryViewModel? saveDirectory);

    Task UpdateSaveDirectoryAsync(CategoryViewModel? viewModel, CategorySaveDirectoryViewModel? saveDirectory);
}