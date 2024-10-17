using AutoMapper;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;

namespace CrossPlatformDownloadManager.Data.Profiles;

public class SettingsProfile : Profile
{
    public SettingsProfile()
    {
        CreateMap<Settings, SettingsViewModel>()
            .ReverseMap();
    }
}