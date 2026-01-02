using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;
using Mapster;

namespace CrossPlatformDownloadManager.Data.MapperConfigs;

public class DownloadFileConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<DownloadFile, DownloadFileViewModel>()
            .Map(dest => dest.DownloadQueueId, src => GetDownloadQueueId(src))
            .Map(dest => dest.DownloadQueueName, src => GetDownloadQueueName(src))
            .Map(dest => dest.Size, src => src.Size == 0 ? (double?)null : src.Size)
            .Map(dest => dest.DownloadProgress, src => src.DownloadProgress == 0 ? (double?)null : src.DownloadProgress)
            .PreserveReference(true);

        config.NewConfig<DownloadFileViewModel, DownloadFile>()
            .Map(dest => dest.Size, src => src.Size ?? 0)
            .Map(dest => dest.DownloadProgress, src => src.DownloadProgress ?? 0)
            .Ignore(nameof(DownloadFile.DownloadQueue), nameof(DownloadFile.Category))
            .PreserveReference(true);
    }

    #region Helpers

    private static int? GetDownloadQueueId(DownloadFile downloadFile)
    {
        return downloadFile.DownloadQueue?.Id;
    }

    private static string GetDownloadQueueName(DownloadFile downloadFile)
    {
        return downloadFile.DownloadQueue?.Title ?? string.Empty;
    }

    #endregion
}