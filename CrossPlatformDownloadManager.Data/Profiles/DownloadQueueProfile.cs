using AutoMapper;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.DbViewModels;

namespace CrossPlatformDownloadManager.Data.Profiles;

public class DownloadQueueProfile : Profile
{
    public DownloadQueueProfile()
    {
        CreateMap<DownloadQueue, DownloadQueueViewModel>()
            .ReverseMap();
    }
}