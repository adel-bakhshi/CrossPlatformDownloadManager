using Avalonia;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.AppFinisher;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.AppInitializer;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.BrowserExtension;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.CategoryService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadQueueService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.ExportImportService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.SettingsService;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Views;
using Microsoft.Extensions.DependencyInjection;
using RolandK.AvaloniaExtensions.DependencyInjection;
using Serilog;
using CrossPlatformDownloadManager.Utils;
using Mapster;
using ReactiveUI.Avalonia;

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
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        // Set the default culture for the application to en-US
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

        const string fileName = "logs.txt";
        var logFilePath = Path.Combine(Constants.ApplicationDataDirectory, fileName);

        // Initialize logger
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(logFilePath)
            .CreateLogger();

        // Check if the application is already running
        CheckApplicationProcess();

        Log.Debug("Building Avalonia app...");

        var appBuilder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI()
            .UseDependencyInjection(services =>
            {
                // Add mapper to services
                TypeAdapterConfig.GlobalSettings.Scan(AppDomain.CurrentDomain.GetAssemblies());
                services.AddMapster();

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

                // Add ExportImportService to services
                services.AddSingleton<IExportImportService, ExportImportService>();

                // Add AppService to services
                services.AddSingleton<IAppService, AppService>();

                // Add ViewModels to services
                services.AddSingleton<AppViewModel>();
                services.AddSingleton<StartupWindowViewModel>();
                services.AddSingleton<TrayMenuWindowViewModel>();

                // Add Windows to services
                services.AddSingleton<StartupWindow>();
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

    /// <summary>
    /// Checks the application process and exits if another instance is running.
    /// </summary>
    private static void CheckApplicationProcess()
    {
        try
        {
            // Check if the application is already running
            var runningInstanceExists = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1;
            if (!runningInstanceExists)
                return;

            // Exit the application
            Log.Information("Application is already running. Exiting...");
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            // Log any exception that occurs during the application finishing process
            Log.Error(ex, "An error occurred while starting the application. Error message: {ErrorMessage}", ex.Message);
            // Force exit the application if an error occurs during startup
            Environment.Exit(0);
        }
    }
}