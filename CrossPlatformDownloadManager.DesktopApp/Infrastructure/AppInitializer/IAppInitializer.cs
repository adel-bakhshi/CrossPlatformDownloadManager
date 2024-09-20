using System.Threading.Tasks;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.AppInitializer;

public interface IAppInitializer
{
    Task InitializeAsync();
}