namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager.StartupManager;

public interface IStartupManager
{
    bool IsRegistered();

    void Register();

    void Delete();
}