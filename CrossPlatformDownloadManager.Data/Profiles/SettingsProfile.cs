using AutoMapper;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.DbViewModels;

namespace CrossPlatformDownloadManager.Data.Profiles;

public class SettingsProfile : Profile
{
    public SettingsProfile()
    {
        CreateMap<Settings, SettingsViewModel>()
            .ReverseMap();
    }
}