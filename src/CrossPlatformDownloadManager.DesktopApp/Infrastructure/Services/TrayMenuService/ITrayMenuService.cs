using System.Threading.Tasks;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.TrayMenuService;

/// <summary>
/// Represents the tray menu service.
/// </summary>
public interface ITrayMenuService
{
    /// <summary>
    /// Opens the main window.
    /// </summary>
    void OpenMainWindow();

    /// <summary>
    /// Starts or stops the download queue.
    /// </summary>
    /// <param name="downloadQueue">The download queue.</param>
    /// <returns>A task that completes when the operation is complete.</returns>
    Task StartStopDownloadQueueAsync(DownloadQueueViewModel? downloadQueue);

    /// <summary>
    /// Adds a new download link.
    /// </summary>
    /// <param name="appService">The app service.</param>
    void AddNewDownloadLink(IAppService appService);

    /// <summary>
    /// Adds a new download queue.
    /// </summary>
    /// <param name="appService">The app service.</param>
    void AddNewDownloadQueue(IAppService appService);

    /// <summary>
    /// Opens the settings window.
    /// </summary>
    /// <param name="appService">The app service.</param>
    void OpenSettingsWindow(IAppService appService);

    /// <summary>
    /// Opens the CDM web page.
    /// </summary>
    void OpenCdmWebPage();

    /// <summary>
    /// Opens the about us window.
    /// </summary>
    /// <param name="appService">The app service.</param>
    void OpenAboutUsWindow(IAppService appService);

    /// <summary>
    /// Exits the program.
    /// </summary>
    /// <returns>A task that completes when the operation is complete.</returns>
    Task ExitProgramAsync();
}