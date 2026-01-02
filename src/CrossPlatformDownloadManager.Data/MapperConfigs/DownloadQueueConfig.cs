using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Utils;
using Mapster;

namespace CrossPlatformDownloadManager.Data.MapperConfigs;

public class DownloadQueueConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<DownloadQueue, DownloadQueueViewModel>()
            .Map(dest => dest.DownloadFiles, src => src.DownloadFiles.ToObservableCollection())
            .PreserveReference(true);

        config.NewConfig<DownloadQueueViewModel, DownloadQueue>()
            .Ignore(nameof(DownloadQueue.DownloadFiles))
            .PreserveReference(true);
    }
}