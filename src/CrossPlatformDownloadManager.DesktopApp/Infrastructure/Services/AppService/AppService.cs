using CrossPlatformDownloadManager.Data.Services.UnitOfWork;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.CategoryService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadQueueService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.ExportImportService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.SettingsService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.TrayMenuService;
using MapsterMapper;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;

/// <summary>
/// Main class for the application services.
/// </summary>
public class AppService : IAppService
{
    #region Properties

    public IMapper Mapper { get; }
    public IUnitOfWork UnitOfWork { get; }
    public IDownloadFileService DownloadFileService { get; }
    public IDownloadQueueService DownloadQueueService { get; }
    public ISettingsService SettingsService { get; }
    public ICategoryService CategoryService { get; }
    public IAppThemeService AppThemeService { get; }
    public IExportImportService ExportImportService { get; }
    public ITrayMenuService TrayMenuService { get; }

    #endregion

    public AppService(
        IUnitOfWork unitOfWork,
        IDownloadFileService downloadFileService,
        IMapper mapper,
        ISettingsService settingsService,
        IDownloadQueueService downloadQueueService,
        ICategoryService categoryService,
        IAppThemeService appThemeService,
        IExportImportService exportImportService,
        ITrayMenuService trayMenuService)
    {
        Mapper = mapper;
        UnitOfWork = unitOfWork;
        DownloadFileService = downloadFileService;
        DownloadQueueService = downloadQueueService;
        CategoryService = categoryService;
        SettingsService = settingsService;
        AppThemeService = appThemeService;
        ExportImportService = exportImportService;
        TrayMenuService = trayMenuService;
    }
}