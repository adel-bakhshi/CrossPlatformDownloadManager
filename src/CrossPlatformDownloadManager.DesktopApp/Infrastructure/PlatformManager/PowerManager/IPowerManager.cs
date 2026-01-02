namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager.PowerManager;

public interface IPowerManager
{
    void Shutdown();

    void Sleep();
    
    void Hibernate();
    
    bool IsHibernateEnabled();
}