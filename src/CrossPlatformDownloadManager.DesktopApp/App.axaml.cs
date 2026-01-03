using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.AppFinisher;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Views;
using Microsoft.Extensions.DependencyInjection;
using RolandK.AvaloniaExtensions.DependencyInjection;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp;

public partial class App : Application
{
    #region Properties

    /// <summary>
    /// Gets the <see cref="IClassicDesktopStyleApplicationLifetime"/> instance.
    /// </summary>
    public static IClassicDesktopStyleApplicationLifetime? Desktop { get; private set; }

    #endregion

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }


    /// <summary>
    /// Called when the framework initialization is completed.
    /// This method initializes the application's data context, sets up the main window,
    /// and registers exception handlers.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            // Log the initialization start
            Log.Debug("Initializing the data context of the app...");

            // Get the service provider and retrieve the AppViewModel
            var serviceProvider = this.TryGetServiceProvider();
            var appViewModel = serviceProvider?.GetService<AppViewModel>();
            // Set the data context or throw exception if null
            DataContext = appViewModel ?? throw new NullReferenceException(nameof(appViewModel));

            // Get the startup window and validate it's not null
            var startupWindow = serviceProvider?.GetService<StartupWindow>();
            if (startupWindow == null)
                throw new NullReferenceException(nameof(startupWindow));

            // Check if the application is running in desktop mode
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Set desktop-specific properties and event handlers
                Desktop = desktop;
                Desktop.MainWindow = startupWindow;
                Desktop.Exit += ApplicationOnExit;
            }

            // Register exception handlers for UI thread and unobserved tasks
            Dispatcher.UIThread.UnhandledException += UIThreadOnUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
            // Call base method and log successful initialization
            base.OnFrameworkInitializationCompleted();
            Log.Information("Application started.");
        }
        catch (Exception ex)
        {
            // Log any exceptions that occurred during initialization
            Log.Error(ex, "An error occurred during initialization. Error message: {ErrorMessage}", ex.Message);
            // Attempt to shut down the application gracefully
            if (Desktop != null)
                Desktop.Shutdown();
            else
                Environment.Exit(0);
        }
    }

    #region Helpers

    /// <summary>
    /// Handles the application exit event, ensuring proper cleanup and shutdown procedures.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A ControlledApplicationLifetimeExitEventArgs that contains the event data.</param>
    private async void ApplicationOnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        try
        {
            // Attempt to get the service provider from the current application instance
            var serviceProvider = this.TryGetServiceProvider();
            // Get the application finisher service from the service provider
            var appFinisher = serviceProvider?.GetService<IAppFinisher>();
            // If no app finisher is found, exit the method early
            if (appFinisher == null)
                return;

            // Execute the application finishing logic asynchronously
            await appFinisher.FinishAppAsync();
        }
        catch (Exception ex)
        {
            // Log any exception that occurs during the application finishing process
            Log.Error(ex, "An error occurred while finishing the application. Error message: {ErrorMessage}", ex.Message);
            // Force exit the application if an error occurs during shutdown
            Environment.Exit(0);
        }
    }

    /// <summary>
    /// Handles unhandled exceptions that occur in the UI thread.
    /// This method is marked as async to allow for asynchronous operations within the exception handling.
    /// It is static as it doesn't require instance-specific data.
    /// </summary>
    /// <param name="sender">The source of the event (typically the Dispatcher).</param>
    /// <param name="e">Provides data for the DispatcherUnhandledException event.</param>
    private static async void UIThreadOnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            // Handle the exception
            e.Handled = true;

            // Log the error
            Log.Error(e.Exception, "An unhandled error occurred in Dispatcher. Error message: {ErrorMessage}", e.Exception.Message);
            await DialogBoxManager.ShowErrorDialogAsync(e.Exception);
        }
        catch
        {
            // Ignore exceptions
        }
    }

    /// <summary>
    /// Handles unobserved task exceptions in the TaskScheduler.
    /// This method is called when an exception occurs in a task that hasn't been observed or awaited.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments containing the exception information.</param>
    private static async void TaskSchedulerOnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        try
        {
            // Handle the exception
            e.SetObserved();

            // Log the error
            Log.Error(e.Exception, "An unhandled error occurred in TaskScheduler. Error message: {ErrorMessage}", e.Exception.Message);
            await DialogBoxManager.ShowErrorDialogAsync(e.Exception);
        }
        catch
        {
            // Ignore exceptions
        }
    }

    #endregion
}