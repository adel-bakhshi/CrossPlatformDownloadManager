using System;
using Avalonia;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.AppInitializer;
using Microsoft.Extensions.DependencyInjection;
using RolandK.AvaloniaExtensions.DependencyInjection;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure;

public static class AppBuilderExtensions
{
    public static AppBuilder InitializeApp(this AppBuilder appBuilder)
    {
        appBuilder.AfterSetup(builder =>
        {
            if (builder.Instance == null)
                throw new NullReferenceException(nameof(builder.Instance));

            var serviceProvider = builder.Instance.TryGetServiceProvider();
            if (serviceProvider == null)
                throw new NullReferenceException(nameof(serviceProvider));
            
            serviceProvider.GetRequiredService<IAppInitializer>().InitializeAsync().GetAwaiter();
        });
        
        return appBuilder;
    }
}