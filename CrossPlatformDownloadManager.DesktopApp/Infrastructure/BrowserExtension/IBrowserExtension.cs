using System.Threading.Tasks;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.BrowserExtension;

public interface IBrowserExtension
{
    Task StartListeningAsync();

    void StopListening();
}