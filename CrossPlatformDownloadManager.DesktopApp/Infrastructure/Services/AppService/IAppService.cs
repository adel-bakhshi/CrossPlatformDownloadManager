using CrossPlatformDownloadManager.Data.Services.UnitOfWork;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.CategoryService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadQueueService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.ExportImportService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.SettingsService;
using MapsterMapper;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;

/// <summary>
/// Interface for the application services.
/// </summary>
public interface IAppService
{
    #region Properties

    /// <summary>
    /// Gets a value that indicates the Mapper service.
    /// </summary>
    IMapper Mapper { get; }

    /// <summary>
    /// Gets a value that indicates the UnitOfWork service.
    /// </summary>
    IUnitOfWork UnitOfWork { get; }

    /// <summary>
    /// Gets a value that indicates the DownloadFile service.
    /// </summary>
    IDownloadFileService DownloadFileService { get; }

    /// <summary>
    /// Gets a value that indicates the DownloadQueue service.
    /// </summary>
    IDownloadQueueService DownloadQueueService { get; }

    /// <summary>
    /// Gets a value that indicates the Settings service.
    /// </summary>
    ISettingsService SettingsService { get; }

    /// <summary>
    /// Gets a value that indicates the Category service.
    /// </summary>
    ICategoryService CategoryService { get; }

    /// <summary>
    /// Gets a value that indicates the AppTheme service.
    /// </summary>
    IAppThemeService AppThemeService { get; }

    /// <summary>
    /// Gets a value that indicates the ExportImport service.
    /// </summary>
    IExportImportService ExportImportService { get; }

    #endregion
}