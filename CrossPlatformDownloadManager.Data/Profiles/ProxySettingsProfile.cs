using AutoMapper;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;

namespace CrossPlatformDownloadManager.Data.Profiles;

public class ProxySettingsProfile : Profile
{
    public ProxySettingsProfile()
    {
        CreateMap<ProxySettings, ProxySettingsViewModel>()
            .ReverseMap();
    }
}