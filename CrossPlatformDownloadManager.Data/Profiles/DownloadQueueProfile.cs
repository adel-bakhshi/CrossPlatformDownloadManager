using AutoMapper;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Utils;

namespace CrossPlatformDownloadManager.Data.Profiles;

public class DownloadQueueProfile : Profile
{
    public DownloadQueueProfile()
    {
        CreateMap<DownloadQueue, DownloadQueueViewModel>()
            .ForMember(dest => dest.DownloadFiles, opt => opt.MapFrom(src => src.DownloadFiles.ToObservableCollection()));

        CreateMap<DownloadQueueViewModel, DownloadQueue>();
    }
}