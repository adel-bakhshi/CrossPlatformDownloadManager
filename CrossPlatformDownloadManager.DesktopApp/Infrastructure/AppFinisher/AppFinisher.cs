using System;
using System.Linq;
using System.Threading.Tasks;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.BrowserExtension;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.AppFinisher;

public class AppFinisher : IAppFinisher
{
    #region Private Fields

    private readonly IAppService _appService;
    private readonly IBrowserExtension _browserExtension;

    #endregion

    public AppFinisher(IAppService appService, IBrowserExtension browserExtension)
    {
        _appService = appService;
        _browserExtension = browserExtension;
    }

    public async Task FinishAppAsync()
    {
        try
        {
            // Find all running download queues
            var runningDownloadQueues = _appService
                .DownloadQueueService
                .DownloadQueues
                .Where(dq => dq.IsRunning)
                .ToList();

            // Iterate over all running download queues and stop them
            foreach (var downloadQueue in runningDownloadQueues)
            {
                await _appService
                    .DownloadQueueService
                    .StopDownloadQueueAsync(downloadQueue, playSound: false);
            }

            // Find all download files that are still downloading
            var downloadFiles = _appService
                .DownloadFileService
                .DownloadFiles
                .Where(df => df.IsDownloading)
                .ToList();

            // Iterate over all download files and stop them
            foreach (var downloadFile in downloadFiles)
            {
                await _appService
                    .DownloadFileService
                    .StopDownloadFileAsync(downloadFile);
            }

            // Get settings and save them
            await _appService
                .SettingsService
                .SaveSettingsAsync(_appService.SettingsService.Settings, reloadData: false);

            // Stop listening for URLs
            _browserExtension.StopListening();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while finishing the application.");
        }
    }
}