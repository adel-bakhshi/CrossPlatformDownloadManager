using AutoMapper;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Utils;

namespace CrossPlatformDownloadManager.Data.Profiles;

public class SettingsProfile : Profile
{
    public SettingsProfile()
    {
        CreateMap<Settings, SettingsViewModel>()
            .ForMember(dest => dest.Proxies, opt => opt.MapFrom(src => src.Proxies.ToObservableCollection()));

        CreateMap<SettingsViewModel, Settings>();
    }
}