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
            .ForMember(dest => dest.Proxies, opt => opt.MapFrom(src => src.Proxies.ToObservableCollection()))
            .ForMember(dest => dest.DataGridColumnSettings, opt => opt.MapFrom(src => MapDataGridColumnsSettings(src.DataGridColumnsSettings)));

        CreateMap<SettingsViewModel, Settings>()
            .ForMember(dest => dest.ManagerPoint, opt => opt.MapFrom(src => src.ManagerPoint.ConvertToJson(null)))
            .ForMember(dest => dest.DataGridColumnsSettings, opt => opt.MapFrom(src => src.DataGridColumnSettings.ConvertToJson(null)));
    }

    private static PointViewModel? MapPoint(string? point)
    {
        return point.IsStringNullOrEmpty() ? null : point.ConvertFromJson<PointViewModel?>();
    }

    private static MainGridColumnSettings MapDataGridColumnsSettings(string? columnsSettings)
    {
        if (columnsSettings.IsStringNullOrEmpty())
            return new MainGridColumnSettings();

        var settings = columnsSettings!.ConvertFromJson<MainGridColumnSettings?>();
        return settings ?? new MainGridColumnSettings();
    }
}