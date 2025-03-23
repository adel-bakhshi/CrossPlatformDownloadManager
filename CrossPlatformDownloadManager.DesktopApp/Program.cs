using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.IO;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.AppFinisher;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.AppInitializer;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.BrowserExtension;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.CategoryService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadQueueService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.SettingsService;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Views;
using Microsoft.Extensions.DependencyInjection;
using RolandK.AvaloniaExtensions.DependencyInjection;
using Serilog;
using CrossPlatformDownloadManager.Utils;

namespace CrossPlatformDownloadManager.DesktopApp;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while starting the application. Error message: {ErrorMessage}", ex.Message);
            try
            {
                _ = DialogBoxManager.ShowErrorDialogAsync(ex);
            }
            catch
            {
                // Ignore exceptions
            }
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        const string fileName = "logs.txt";
        var logFilePath = Path.Combine(Constants.ApplicationDataDirectory, fileName);

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(logFilePath)
            .CreateLogger();

        var appBuilder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI()
            .UseDependencyInjection(services =>
            {
                // Add AutoMapper to services
                services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

                // Add UnitOfWork to services
                services.AddTransient<IUnitOfWork, UnitOfWork>();

                // Add SettingsService to services
                services.AddSingleton<ISettingsService, SettingsService>();

                // Add AppThemeService to services
                services.AddSingleton<IAppThemeService, AppThemeService>();

                // Add CategoryService to services
                services.AddSingleton<ICategoryService, CategoryService>();

                // Add DownloadFileService to services
                services.AddSingleton<IDownloadFileService, DownloadFileService>();

                // Add DownloadQueueService to services
                services.AddSingleton<IDownloadQueueService, DownloadQueueService>();

                // Add AppService to services
                services.AddSingleton<IAppService, AppService>();

                // Add ViewModels to services
                services.AddSingleton<AppViewModel>();
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<TrayMenuWindowViewModel>();

                // Add Windows to services
                services.AddSingleton<MainWindow>();
                services.AddSingleton<TrayMenuWindow>();

                // Add BrowserExtension to services
                services.AddSingleton<IBrowserExtension, BrowserExtension>();

                // Add AppInitializer to services
                services.AddSingleton<IAppInitializer, AppInitializer>();

                // Add AppFinisher to services
                services.AddSingleton<IAppFinisher, AppFinisher>();
            })
            .InitializeApp();

        return appBuilder;
    }
}