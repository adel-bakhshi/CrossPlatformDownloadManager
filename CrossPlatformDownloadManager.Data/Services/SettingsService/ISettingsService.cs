using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.DbViewModels;

namespace CrossPlatformDownloadManager.Data.Services.SettingsService;

public interface ISettingsService
{
    Task LoadSettingsAsync();
    
    Task SaveSettingsAsync(SettingsViewModel settings);
}