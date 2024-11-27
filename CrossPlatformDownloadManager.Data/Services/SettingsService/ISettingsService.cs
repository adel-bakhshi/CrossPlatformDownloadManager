using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;

namespace CrossPlatformDownloadManager.Data.Services.SettingsService;

public interface ISettingsService
{
    #region Properties

    SettingsViewModel Settings { get; }

    #endregion

    #region Events

    event EventHandler? DataChanged;
    event EventHandler? ActiveProxyChanged;

    #endregion

    Task LoadSettingsAsync();

    Task SaveSettingsAsync(SettingsViewModel viewModel, bool reloadData = true);

    Task<int> AddProxySettingsAsync(ProxySettings? proxySettings);

    Task<int> AddProxySettingsAsync(ProxySettingsViewModel? viewModel);

    Task UpdateProxySettingsAsync(ProxySettings? proxySettings);

    Task UpdateProxySettingsAsync(ProxySettingsViewModel? viewModel);

    Task DeleteProxySettingsAsync(ProxySettings? proxySettings);
    
    Task DeleteProxySettingsAsync(ProxySettingsViewModel? viewModel);

    Task ActiveProxyAsync(ProxySettingsViewModel? viewModel);

    Task DisableProxyAsync();

    Task UseSystemProxySettingsAsync();
}