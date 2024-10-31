using System.Threading.Tasks;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.AppFinisher;

public interface IAppFinisher
{
    Task FinishAppAsync();
}