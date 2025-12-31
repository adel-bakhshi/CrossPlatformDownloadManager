using System;
using System.Threading.Tasks;
using Avalonia;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.AppInitializer;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.ViewModels.Main;
using CrossPlatformDownloadManager.DesktopApp.Views.Main;
using Microsoft.Extensions.DependencyInjection;
using RolandK.AvaloniaExtensions.DependencyInjection;
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
    private MainWindow? _mainWindow;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupWindowViewModel"/> class.
    /// </summary>
    /// <param name="appService">Application service for accessing various services.</param>
    public StartupWindowViewModel(IAppService appService) : base(appService)
    {
    }

    /// <summary>
    /// Asynchronously initializes the application.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeApplicationAsync()
    {
        try
        {
            // Get app initializer from the service provider
            var serviceProvider = Application.Current?.TryGetServiceProvider();
            var appInitializer = serviceProvider?.GetService<IAppInitializer>();
            
            // Check if app initializer is null
            if (appInitializer == null)
            {
                Log.Debug("Service provider is null. Dependency injection is not properly configured.");

                await DialogBoxManager.ShowDangerDialogAsync(
                    dialogHeader: "Critical Error",
                    dialogMessage: "An error occurred while trying to initialize the application. Please restart the application.",
                    dialogButtons: DialogButtons.Ok);

                Log.Debug("Shutting down application...");

                if (App.Desktop?.TryShutdown() != true)
                    Environment.Exit(1);

                return;
            }

            Log.Debug("Initializing application...");
            
            // Initialize application
            await appInitializer.InitializeAsync();
            
            Log.Information("Application initialization completed successfully.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to initialize the application. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
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
        _mainWindow = new MainWindow { DataContext = viewModel };
    }
}