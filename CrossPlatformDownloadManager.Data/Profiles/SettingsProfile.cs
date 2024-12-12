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
            .ForMember(dest => dest.ManagerPoint, opt => opt.MapFrom(src => MapPoint(src.ManagerPoint)))
            .ForMember(dest => dest.Proxies, opt => opt.MapFrom(src => src.Proxies.ToObservableCollection()));

        CreateMap<SettingsViewModel, Settings>()
            .ForMember(dest => dest.ManagerPoint, opt => opt.MapFrom(src => src.ManagerPoint.ConvertToJson()));
    }

    private static PointViewModel? MapPoint(string? point)
    {
        return point.IsNullOrEmpty() ? null : point.ConvertFromJson<PointViewModel?>();
    }
}