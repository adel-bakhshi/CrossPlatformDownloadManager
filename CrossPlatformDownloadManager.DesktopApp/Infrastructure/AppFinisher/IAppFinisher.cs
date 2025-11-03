using System.Threading.Tasks;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.AppFinisher;

/// <summary>
/// Defines the contract for application shutdown and cleanup operations.
/// </summary>
public interface IAppFinisher
{
    /// <summary>
    /// Performs application cleanup and shutdown procedures.
    /// This includes stopping all active downloads, saving settings, and releasing resources.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task FinishAppAsync();
}