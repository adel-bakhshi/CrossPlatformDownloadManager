using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.AppFinisher;
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

    #endregion
}