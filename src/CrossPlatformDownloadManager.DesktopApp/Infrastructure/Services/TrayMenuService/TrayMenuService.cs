using System.Diagnostics;
using System.Threading.Tasks;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadQueueService;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.ViewModels.AddEditQueue;
using CrossPlatformDownloadManager.DesktopApp.ViewModels.Settings;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.DesktopApp.Views.AddEditQueue;
using CrossPlatformDownloadManager.DesktopApp.Views.Settings;
using CrossPlatformDownloadManager.Utils;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.TrayMenuService;

/// <summary>
/// Represents the tray menu service.
/// </summary>
public class TrayMenuService : ITrayMenuService
{
    #region Private Fields

    /// <summary>
    /// The download queue service.
    /// </summary>
    private readonly IDownloadQueueService _downloadQueueService;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="TrayMenuService"/> class.
    /// </summary>
    /// <param name="downloadQueueService">The download queue service.</param>
    public TrayMenuService(IDownloadQueueService downloadQueueService)
    {
        _downloadQueueService = downloadQueueService;
    }

    public void OpenMainWindow()
    {
        // Check if the startup window exists and view model is not null,
        // If so, show the main window
        if (App.Desktop?.MainWindow?.DataContext is not StartupWindowViewModel viewModel)
            return;

        viewModel.ShowMainWindow();
    }

    public async Task StartStopDownloadQueueAsync(DownloadQueueViewModel? downloadQueue)
    {
        // Check if the download queue is null
        if (downloadQueue == null)
            return;

        // Check if the download queue is running
        if (!downloadQueue.IsRunning)
        {
            await _downloadQueueService.StartDownloadQueueAsync(downloadQueue);
        }
        else
        {
            await _downloadQueueService.StopDownloadQueueAsync(downloadQueue);
        }
    }

    public void AddNewDownloadLink(IAppService appService)
    {
        var vm = new CaptureUrlWindowViewModel(appService);
        var window = new CaptureUrlWindow { DataContext = vm };
        window.Show();
    }

    public void AddNewDownloadQueue(IAppService appService)
    {
        var vm = new AddEditQueueWindowViewModel(appService, null);
        var window = new AddEditQueueWindow { DataContext = vm };
        window.Show();
    }

    public void OpenSettingsWindow(IAppService appService)
    {
        var vm = new SettingsWindowViewModel(appService);
        var window = new SettingsWindow { DataContext = vm };
        window.Show();
    }

    public void OpenCdmWebPage()
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = Constants.CdmWebsiteUrl,
            UseShellExecute = true
        };

        Process.Start(processStartInfo);
    }

    public void OpenAboutUsWindow(IAppService appService)
    {
        var vm = new AboutUsWindowViewModel(appService);
        var window = new AboutUsWindow { DataContext = vm };
        window.Show();
    }

    public async Task ExitProgramAsync()
    {
        // Check if the desktop is null
        if (App.Desktop == null)
            return;

        // Show the exit dialog
        var result = await DialogBoxManager.ShowInfoDialogAsync("Exit", "Are you sure you want to exit the app?", DialogButtons.YesNo);
        if (result != DialogResult.Yes)
            return;

        // Exit the program
        App.Desktop.Shutdown();
    }
}