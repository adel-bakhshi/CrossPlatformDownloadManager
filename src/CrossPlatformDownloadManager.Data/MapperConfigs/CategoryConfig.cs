using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Utils;
using Mapster;

namespace CrossPlatformDownloadManager.Data.MapperConfigs;

public class CategoryConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Category, CategoryViewModel>()
            .Map(dest => dest.FileExtensions, src => src.FileExtensions.ToObservableCollection())
            .PreserveReference(true);

        config.NewConfig<CategoryViewModel, Category>()
            .Ignore(nameof(Category.FileExtensions), nameof(Category.CategorySaveDirectory))
            .PreserveReference(true);
    }
}