using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;
using Mapster;

namespace CrossPlatformDownloadManager.Data.MapperConfigs;

public class ProxySettingsConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ProxySettings, ProxySettingsViewModel>()
            .TwoWays()
            .PreserveReference(true);
    }
}