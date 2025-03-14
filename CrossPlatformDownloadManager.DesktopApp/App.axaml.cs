using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
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

    public static IClassicDesktopStyleApplicationLifetime? Desktop { get; private set; }

    #endregion

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            var serviceProvider = this.TryGetServiceProvider();
            var appViewModel = serviceProvider?.GetService<AppViewModel>();
            DataContext = appViewModel ?? throw new NullReferenceException(nameof(appViewModel));
            
            var mainWindow = serviceProvider?.GetService<MainWindow>();
            if (mainWindow == null)
                throw new NullReferenceException(nameof(mainWindow));

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                Desktop = desktop;
                Desktop.MainWindow = mainWindow;
                Desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                Desktop.Exit += ApplicationOnExit;
            }

            Dispatcher.UIThread.UnhandledException += UIThreadOnUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
            base.OnFrameworkInitializationCompleted();
            Log.Information("Application started");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred during initialization. Error message: {ErrorMessage}", ex.Message);
            if (Desktop != null)
                Desktop.Shutdown();
            else
                Environment.Exit(0);
        }
    }

    #region Helpers

    private async void TrayIconOnClicked(object? sender, EventArgs e)
    {
        try
        {
            Desktop?.MainWindow?.Show();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to show main window. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async void ApplicationOnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        try
        {
            var serviceProvider = this.TryGetServiceProvider();
            var appFinisher = serviceProvider?.GetService<IAppFinisher>();
            if (appFinisher == null)
                return;

            await appFinisher.FinishAppAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while finishing the application. Error message: {ErrorMessage}", ex.Message);
            Environment.Exit(0);
        }
    }

    private static async void UIThreadOnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            Log.Error(e.Exception, "An unhandled error occurred in Dispatcher. Error message: {ErrorMessage}", e.Exception.Message);
            await DialogBoxManager.ShowErrorDialogAsync(e.Exception);
        }
        catch
        {
            // Ignore exceptions
        }
    }

    private static async void TaskSchedulerOnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        try
        {
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