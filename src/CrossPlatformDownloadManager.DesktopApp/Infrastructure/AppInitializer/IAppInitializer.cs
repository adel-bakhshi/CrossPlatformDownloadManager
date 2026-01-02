using System.Threading.Tasks;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.AppInitializer;

/// <summary>
/// Defines the contract for application initialization.
/// </summary>
public interface IAppInitializer
{
    /// <summary>
    /// Initializes the application components and services asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    Task InitializeAsync();
}