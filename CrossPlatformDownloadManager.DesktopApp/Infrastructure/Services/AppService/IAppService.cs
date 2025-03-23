using AutoMapper;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.CategoryService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadQueueService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.SettingsService;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;

public interface IAppService
{
    #region Properties

    IMapper Mapper { get; }
    IUnitOfWork UnitOfWork { get; }
    IDownloadFileService DownloadFileService { get; }
    IDownloadQueueService DownloadQueueService { get; }
    ISettingsService SettingsService { get; }
    ICategoryService CategoryService { get; }
    IAppThemeService AppThemeService { get; }

    #endregion
}