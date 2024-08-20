using Avalonia;
using Avalonia.ReactiveUI;
using System;
using CrossPlatformDownloadManager.Data.UnitOfWork;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Views;
using Microsoft.Extensions.DependencyInjection;
using RolandK.AvaloniaExtensions.DependencyInjection;

namespace CrossPlatformDownloadManager.DesktopApp;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UseDependencyInjection(services =>
            {
                // Add UnitOfWork to services
                services.AddTransient<IUnitOfWork, UnitOfWork>();
                
                // Add ViewModels to services
                services.AddSingleton<MainWindowViewModel>();
                
                // Add Windows to services
                services.AddSingleton<MainWindow>();
            })
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}