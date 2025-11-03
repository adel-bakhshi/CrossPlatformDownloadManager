using System;
using System.Threading.Tasks;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Audio;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.BrowserExtension;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.AppInitializer;

/// <summary>
/// Handles the initialization of the application components and services.
/// </summary>
public class AppInitializer : IAppInitializer
{
    #region Private Fields

    /// <summary>
    /// The app service instance containing application services.
    /// </summary>
    private readonly IAppService _appService;

    /// <summary>
    /// The browser extension service for handling request that received from browser.
    /// </summary>
    private readonly IBrowserExtension _browserExtension;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="AppInitializer"/> class.
    /// </summary>
    /// <param name="appService">The application service instance.</param>
    /// <param name="browserExtension">The browser extension service instance.</param>
    public AppInitializer(IAppService appService, IBrowserExtension browserExtension)
    {
        _appService = appService;
        _browserExtension = browserExtension;

        Log.Debug("AppInitializer initialized successfully.");
    }

    public async Task InitializeAsync()
    {
        try
        {
            Log.Information("Starting application initialization...");

            // Initialize Database
            Log.Debug("Creating database and applying migrations...");
            await _appService.UnitOfWork.CreateDatabaseAsync();
            Log.Debug("Database initialization completed successfully.");

            // Initialize Categories
            Log.Debug("Creating default categories...");
            await _appService.UnitOfWork.CreateCategoriesAsync();
            Log.Debug("Categories initialization completed successfully.");

            // Initialize SettingsService
            Log.Debug("Loading application settings...");
            await _appService.SettingsService.LoadSettingsAsync();
            Log.Debug("Settings service initialization completed successfully.");

            // Initialize CategoryService
            Log.Debug("Loading categories and category headers...");
            await _appService.CategoryService.LoadCategoriesAsync(loadHeaders: true);
            Log.Debug("Category service initialization completed successfully.");

            // Initialize DownloadFileService
            Log.Debug("Loading download files from database...");
            await _appService.DownloadFileService.LoadDownloadFilesAsync();
            Log.Debug("Download file service initialization completed successfully.");

            // Initialize DownloadQueueService
            Log.Debug("Loading download queues and adding default queue if needed...");
            await _appService.DownloadQueueService.LoadDownloadQueuesAsync(addDefaultDownloadQueue: true);
            Log.Debug("Download queue service initialization completed successfully.");

            // Start listening for URLs from browser extension
            Log.Debug("Starting browser extension listener for URL capture...");
            _ = _browserExtension.StartListeningAsync();
            Log.Debug("Browser extension listener started successfully.");

            // Initialize AudioManager for notification sounds
            Log.Debug("Initializing audio manager for notification sounds...");
            AudioManager.Initialize();
            Log.Debug("Audio manager initialization completed successfully.");

            // Load theme data for application styling
            Log.Debug("Loading application theme data...");
            _appService.AppThemeService.LoadThemeData();
            Log.Debug("Theme data loading completed successfully.");

            Log.Information("Application initialization completed successfully. All services are ready.");
        }
        catch (Exception ex)
        {
            // Log error and exit application
            Log.Error(ex, "A critical error occurred while initializing the application. Application will exit.");
            Environment.Exit(0);
        }
    }
}