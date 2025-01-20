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
            .ForMember(dest => dest.DataGridColumnsSettings, opt => opt.MapFrom(src => MapDataGridColumnsSettings(src.DataGridColumnsSettings)));

        CreateMap<SettingsViewModel, Settings>()
            .ForMember(dest => dest.ManagerPoint, opt => opt.MapFrom(src => src.ManagerPoint.ConvertToJson()))
            .ForMember(dest => dest.DataGridColumnsSettings, opt => opt.MapFrom(src => src.DataGridColumnsSettings.ConvertToJson()));
    }

    private static PointViewModel? MapPoint(string? point)
    {
        return point.IsNullOrEmpty() ? null : point.ConvertFromJson<PointViewModel?>();
    }

    private MainDownloadFilesDataGridColumnsSettings MapDataGridColumnsSettings(string? columnsSettings)
    {
        if (columnsSettings.IsNullOrEmpty())
            return new MainDownloadFilesDataGridColumnsSettings();

        var settings = columnsSettings!.ConvertFromJson<MainDownloadFilesDataGridColumnsSettings?>();
        return settings ?? new MainDownloadFilesDataGridColumnsSettings();
    }
}