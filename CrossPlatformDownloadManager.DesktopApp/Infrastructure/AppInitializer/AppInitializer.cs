using System;
using System.Threading.Tasks;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.BrowserExtension;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;

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
        // TODO: Show message box
        try
        {
            // Initialize UnitOfWork
            await _appService.UnitOfWork.CreateDatabaseAsync();
            await _appService.UnitOfWork.CreateCategoriesAsync();

            // Initialize SettingsService
            await _appService.SettingsService.LoadSettingsAsync();

            // Initialize DownloadFileService
            await _appService.DownloadFileService.LoadDownloadFilesAsync();
            
            // Initialize DownloadQueueService
            await _appService.DownloadQueueService.LoadDownloadQueuesAsync(addDefaultDownloadQueue: true);
            
            // Start listening for URLs
            await _browserExtension.StartListeningAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}