using CrossPlatformDownloadManager.Data.ViewModels;

namespace CrossPlatformDownloadManager.Data.Services.SettingsService;

public interface ISettingsService
{
    #region Properties

    SettingsViewModel? Settings { get; }

    #endregion

    Task LoadSettingsAsync();

    Task SaveSettingsAsync(SettingsViewModel settings);
}