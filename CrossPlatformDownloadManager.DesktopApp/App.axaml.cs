using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.AppFinisher;
using CrossPlatformDownloadManager.DesktopApp.Views;
using Microsoft.Extensions.DependencyInjection;
using RolandK.AvaloniaExtensions.DependencyInjection;

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
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
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
            Console.WriteLine(ex);
        }
    }

    #endregion
}