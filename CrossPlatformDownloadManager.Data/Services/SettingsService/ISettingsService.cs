using CrossPlatformDownloadManager.Data.ViewModels;

namespace CrossPlatformDownloadManager.Data.Services.SettingsService;

public interface ISettingsService
{
    Task LoadSettingsAsync();
    
    Task SaveSettingsAsync(SettingsViewModel settings);
}