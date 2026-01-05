using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;
using Mapster;

namespace CrossPlatformDownloadManager.Data.MapperConfigs;

public class CategoryFileExtensionConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CategoryFileExtension, CategoryFileExtensionViewModel>()
            .PreserveReference(true);

        config.NewConfig<CategoryFileExtensionViewModel, CategoryFileExtension>()
            .Ignore(nameof(CategoryFileExtension.Category))
            .PreserveReference(true);
    }
}