using System;
using System.Threading.Tasks;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Audio;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.BrowserExtension;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.AppInitializer;

public class AppInitializer : IAppInitializer
{
    #region Private Fields

    private readonly IAppService _appService;
    private readonly IBrowserExtension _browserExtension;

    #endregion

    public AppInitializer(IAppService appService, IBrowserExtension browserExtension)
    {
        _appService = appService;
        _browserExtension = browserExtension;
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Initialize UnitOfWork
            await _appService.UnitOfWork.CreateDatabaseAsync();
            await _appService.UnitOfWork.CreateCategoriesAsync();

            // Initialize SettingsService
            await _appService.SettingsService.LoadSettingsAsync();

            // Initialize CategoryService
            await _appService.CategoryService.LoadCategoriesAsync(loadHeaders: true);

            // Initialize DownloadFileService
            await _appService.DownloadFileService.LoadDownloadFilesAsync();

            // Initialize DownloadQueueService
            await _appService.DownloadQueueService.LoadDownloadQueuesAsync(addDefaultDownloadQueue: true);

            // Start listening for URLs
            _ = _browserExtension.StartListeningAsync();

            // Initialize AudioManager
            AudioManager.Initialize();
            
            // Load theme data
            _appService.AppThemeService.LoadThemeData();
        }
        catch (Exception ex)
        {
            // Log error and exit application
            Log.Error(ex, "An error occurred while initializing the application.");
            Environment.Exit(0);
        }
    }
}