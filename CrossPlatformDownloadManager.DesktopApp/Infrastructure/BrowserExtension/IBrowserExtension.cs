using System.Threading.Tasks;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.BrowserExtension;

public interface IBrowserExtension
{
    /// <summary>
    /// Starts listening for download events from the browser.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task StartListeningAsync();

    /// <summary>
    /// Stops listening for download events from the browser.
    /// </summary>
    void StopListening();
}