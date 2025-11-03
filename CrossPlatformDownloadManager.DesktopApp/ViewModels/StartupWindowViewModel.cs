using System;
using System.Threading.Tasks;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

/// <summary>
/// ViewModel for the startup window that handles application initialization and loading.
/// </summary>
public class StartupWindowViewModel : ViewModelBase
{
    #region Private fields

    /// <summary>
    /// Reference to the main window instance.
    /// </summary>
    private Views.MainWindow? _mainWindow;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupWindowViewModel"/> class.
    /// </summary>
    /// <param name="appService">Application service for accessing various services.</param>
    public StartupWindowViewModel(IAppService appService) : base(appService)
    {
    }

    /// <summary>
    /// Asynchronously loads and initializes the application components.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LoadAppAsync()
    {
        try
        {
            // Create main window if it doesn't exist
            CreateMainWindow();

            // Get settings service and show manager if needed
            if (AppService.SettingsService.Settings.UseManager)
                AppService.SettingsService.ShowManager();

            // Start download queues manager timer to manage queues
            AppService.DownloadQueueService.StartScheduleManagerTimer();

            // Check if the application has been run yet, if not, show the main window
            if (!AppService.SettingsService.Settings.HasApplicationBeenRunYet)
            {
                AppService.SettingsService.Settings.HasApplicationBeenRunYet = true;
                await AppService.SettingsService.SaveSettingsAsync(AppService.SettingsService.Settings, reloadData: true);

                // Show main window
                _mainWindow!.Show();
            }

            // Check for updates
            if (_mainWindow!.DataContext is MainWindowViewModel viewModel)
                await viewModel.CheckForUpdatesAsync(null);
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred while trying to load the application. Error message: {ErrorMessage}", ex.Message);
        }
    }

    /// <summary>
    /// Shows the main window.
    /// </summary>
    public void ShowMainWindow()
    {
        // Create main window if it doesn't exist
        CreateMainWindow();
        // Show main window
        _mainWindow!.Show();
    }

    /// <summary>
    /// Creates the main window instance if it doesn't exist.
    /// </summary>
    private void CreateMainWindow()
    {
        if (_mainWindow != null)
            return;

        var viewModel = new MainWindowViewModel(AppService);
        _mainWindow = new Views.MainWindow { DataContext = viewModel };
    }
}