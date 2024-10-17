using AutoMapper;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;

namespace CrossPlatformDownloadManager.Data.Profiles;

public class DownloadQueueProfile : Profile
{
    public DownloadQueueProfile()
    {
        CreateMap<DownloadQueue, DownloadQueueViewModel>()
            .ReverseMap();
    }
}