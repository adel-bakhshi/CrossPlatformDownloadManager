using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;
using Mapster;

namespace CrossPlatformDownloadManager.Data.MapperConfigs;

public class CategorySaveDirectoryConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CategorySaveDirectory, CategorySaveDirectoryViewModel>()
            .PreserveReference(true);

        config.NewConfig<CategorySaveDirectoryViewModel, CategorySaveDirectory>()
            .Map(dest => dest.CategoryId, src => GetCategoryId(src))
            .Ignore(nameof(CategorySaveDirectory.Category))
            .PreserveReference(true);
    }

    #region Helpers

    private static int? GetCategoryId(CategorySaveDirectoryViewModel viewModel)
    {
        return viewModel.Category?.Id;
    }

    #endregion
}