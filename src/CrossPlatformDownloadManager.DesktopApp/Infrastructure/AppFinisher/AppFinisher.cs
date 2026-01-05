using System;
using System.Linq;
using System.Threading.Tasks;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.BrowserExtension;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.AppFinisher;

/// <summary>
/// Handles application shutdown procedures and cleanup operations.
/// </summary>
public class AppFinisher : IAppFinisher
{
    #region Private Fields

    /// <summary>
    /// The app service containing application services.
    /// </summary>
    private readonly IAppService _appService;

    /// <summary>
    /// The browser extension service for handling requests that received from browser.
    /// </summary>
    private readonly IBrowserExtension _browserExtension;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="AppFinisher"/> class.
    /// </summary>
    /// <param name="appService">The application service for accessing various services.</param>
    /// <param name="browserExtension">The browser extension service for URL listening.</param>
    public AppFinisher(IAppService appService, IBrowserExtension browserExtension)
    {
        _appService = appService;
        _browserExtension = browserExtension;

        Log.Debug("AppFinisher initialized successfully");
    }

    /// <summary>
    /// Performs application cleanup and shutdown procedures.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task FinishAppAsync()
    {
        Log.Information("Starting application shutdown procedures...");

        try
        {
            // Find all running download queues
            var runningDownloadQueues = _appService
                .DownloadQueueService
                .DownloadQueues
                .Where(dq => dq.IsRunning)
                .ToList();

            Log.Debug("Found {RunningQueueCount} running download queues to stop", runningDownloadQueues.Count);

            // Iterate over all running download queues and stop them
            foreach (var downloadQueue in runningDownloadQueues)
            {
                Log.Debug("Stopping download queue: {QueueTitle}", downloadQueue.Title);

                await _appService
                    .DownloadQueueService
                    .StopDownloadQueueAsync(downloadQueue, playSound: false);

                Log.Debug("Download queue stopped: {QueueTitle}", downloadQueue.Title);
            }

            Log.Information("Stopped all running download queues");

            // Find all download files that are still downloading
            var downloadFiles = _appService
                .DownloadFileService
                .DownloadFiles
                .Where(df => df.IsDownloading)
                .ToList();

            Log.Debug("Found {DownloadingFileCount} downloading files to stop", downloadFiles.Count);

            // Iterate over all download files and stop them
            foreach (var downloadFile in downloadFiles)
            {
                Log.Debug("Stopping download file: {FileName}", downloadFile.FileName);

                await _appService
                    .DownloadFileService
                    .StopDownloadFileAsync(downloadFile);

                Log.Debug("Download file stopped: {FileName}", downloadFile.FileName);
            }

            Log.Information("Stopped all downloading files");

            // Get settings and save them
            Log.Debug("Saving application settings before shutdown...");

            await _appService
                .SettingsService
                .SaveSettingsAsync(_appService.SettingsService.Settings, reloadData: false);

            Log.Debug("Application settings saved successfully");

            // Stop listening for URLs
            Log.Debug("Stopping browser extension URL listening...");
            _browserExtension.StopListening();
            Log.Debug("Browser extension URL listening stopped");

            Log.Information("Application shutdown procedures completed successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while finishing the application. Error message: {ErrorMessage}", ex.Message);
            throw;
        }
    }
}