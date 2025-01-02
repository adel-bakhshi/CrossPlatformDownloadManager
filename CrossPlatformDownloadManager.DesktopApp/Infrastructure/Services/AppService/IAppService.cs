using AutoMapper;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadQueueService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.SettingsService;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;

public interface IAppService
{
    IMapper Mapper { get; }
    IUnitOfWork UnitOfWork { get; }
    IDownloadFileService DownloadFileService { get; }
    IDownloadQueueService DownloadQueueService { get; }
    ISettingsService SettingsService { get; }
}