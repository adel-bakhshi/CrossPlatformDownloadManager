namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.StartupManager;

public interface IStartupManager
{
    bool IsRegistered();

    void Register();

    void Delete();
}