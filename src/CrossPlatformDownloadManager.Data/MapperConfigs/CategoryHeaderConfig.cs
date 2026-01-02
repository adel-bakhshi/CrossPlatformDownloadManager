using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Utils;
using Mapster;

namespace CrossPlatformDownloadManager.Data.MapperConfigs;

public class CategoryHeaderConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CategoryHeader, CategoryHeaderViewModel>()
            .Map(dest => dest.Categories, src => src.Categories.ToObservableCollection())
            .PreserveReference(true);

        config.NewConfig<CategoryHeaderViewModel, CategoryHeader>()
            .PreserveReference(true);
    }
}