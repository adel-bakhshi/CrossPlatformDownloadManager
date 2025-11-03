using System;
using Avalonia;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.AppInitializer;
using Microsoft.Extensions.DependencyInjection;
using RolandK.AvaloniaExtensions.DependencyInjection;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure;

/// <summary>
/// Provides extension methods for AppBuilder to initialize the application.
/// </summary>
public static class AppBuilderExtensions
{
    /// <summary>
    /// Initializes the application by setting up services and running the application initializer.
    /// </summary>
    /// <param name="appBuilder">The AppBuilder instance to extend.</param>
    /// <returns>The same AppBuilder instance for method chaining.</returns>
    /// <exception cref="NullReferenceException">Thrown when the application instance or service provider is null.</exception>
    public static AppBuilder InitializeApp(this AppBuilder appBuilder)
    {
        Log.Debug("Configuring application initialization...");

        appBuilder.AfterSetup(builder =>
        {
            Log.Debug("Starting application setup process...");

            // Validate application instance
            if (builder.Instance == null)
            {
                Log.Error("Application instance is null. Cannot proceed with initialization.");
                throw new NullReferenceException(nameof(builder.Instance));
            }

            Log.Debug("Application instance validated successfully.");

            // Get service provider
            var serviceProvider = builder.Instance.TryGetServiceProvider();
            if (serviceProvider == null)
            {
                Log.Error("Service provider is null. Dependency injection is not properly configured.");
                throw new NullReferenceException(nameof(serviceProvider));
            }

            Log.Debug("Service provider retrieved successfully.");

            try
            {
                // Initialize the application
                Log.Information("Starting application initialization via AppInitializer...");
                var appInitializer = serviceProvider.GetRequiredService<IAppInitializer>();
                appInitializer.InitializeAsync().Wait();

                Log.Information("Application initialization completed successfully.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred during application initialization.");
                throw;
            }
        });

        Log.Debug("Application initialization configuration completed.");
        return appBuilder;
    }
}