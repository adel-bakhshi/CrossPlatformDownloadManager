using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Utils;
using Mapster;

namespace CrossPlatformDownloadManager.Data.MapperConfigs;

public class SettingsConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Settings, SettingsViewModel>()
            .Map(dest => dest.ManagerPoint, src => MapPoint(src.ManagerPoint))
            .Map(dest => dest.Proxies, src => src.Proxies.ToObservableCollection())
            .Map(dest => dest.DataGridColumnSettings, src => MapDataGridColumnsSettings(src.DataGridColumnsSettings))
            .PreserveReference(true);

        config.NewConfig<SettingsViewModel, Settings>()
            .Map(dest => dest.ManagerPoint, src => src.ManagerPoint.ConvertToJson(null))
            .Map(dest => dest.DataGridColumnsSettings, src => src.DataGridColumnSettings.ConvertToJson(null))
            .PreserveReference(true);
    }

    #region Helpers

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

    #endregion
}