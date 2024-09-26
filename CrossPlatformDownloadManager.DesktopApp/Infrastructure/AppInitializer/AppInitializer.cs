using System;
using System.Threading.Tasks;
using CrossPlatformDownloadManager.Data.Services.AppService;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.AppInitializer;

public class AppInitializer : IAppInitializer
{
    #region Private Fields

    private readonly IAppService _appService;

    #endregion

    public AppInitializer(IAppService appService)
    {
        _appService = appService;
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
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}