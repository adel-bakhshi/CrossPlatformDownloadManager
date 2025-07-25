using System.Threading.Tasks;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.ExportImportService;

public interface IExportImportService
{
    Task ExportDataAsync(bool exportAsCdmFile);
    
    Task ImportDataAsync();

    Task ExportSettingsAsync();
    
    Task ImportSettingsAsync();
}