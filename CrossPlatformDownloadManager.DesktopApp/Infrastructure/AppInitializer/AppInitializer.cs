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
            Log.Information("Initializing application...");

            // Initialize Database
            Log.Debug("Creating database and applying migrations...");
            await _appService.UnitOfWork.CreateDatabaseAsync();

            // Initialize Categories
            Log.Debug("Creating categories...");
            await _appService.UnitOfWork.CreateCategoriesAsync();

            // Initialize SettingsService
            Log.Debug("Loading application settings...");
            await _appService.SettingsService.LoadSettingsAsync();

            // Initialize CategoryService
            Log.Debug("Loading categories...");
            await _appService.CategoryService.LoadCategoriesAsync(loadHeaders: true);

            // Initialize DownloadFileService
            Log.Debug("Loading download files...");
            await _appService.DownloadFileService.LoadDownloadFilesAsync();

            // Initialize DownloadQueueService
            Log.Debug("Loading download queues...");
            await _appService.DownloadQueueService.LoadDownloadQueuesAsync(addDefaultDownloadQueue: true);

            // Start listening for URLs
            Log.Debug("Starting browser extension listener...");
            _ = _browserExtension.StartListeningAsync();

            // Initialize AudioManager
            Log.Debug("Initializing audio manager...");
            AudioManager.Initialize();

            // Load theme data
            Log.Debug("Loading theme data...");
            _appService.AppThemeService.LoadThemeData();

            Log.Information("Application initialized successfully.");
        }
        catch (Exception ex)
        {
            // Log error and exit application
            Log.Error(ex, "An error occurred while initializing the application.");
            Environment.Exit(0);
        }
    }
}