using AutoMapper;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;

namespace CrossPlatformDownloadManager.Data.Profiles;

public class DownloadFileProfile : Profile
{
    public DownloadFileProfile()
    {
        CreateMap<DownloadFile, DownloadFileViewModel>()
            .ForMember(dest => dest.DownloadQueueId, opt => opt.MapFrom(src => GetDownloadQueueId(src)))
            .ForMember(dest => dest.DownloadQueueName, opt => opt.MapFrom(src => GetDownloadQueueName(src)))
            .ForMember(dest => dest.Size, opt => opt.MapFrom(src => src.Size == 0 ? (double?)null : src.Size))
            .ForMember(dest => dest.DownloadProgress, opt => opt.MapFrom(src => src.DownloadProgress == 0 ? (double?)null : src.DownloadProgress))
            .ReverseMap();
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