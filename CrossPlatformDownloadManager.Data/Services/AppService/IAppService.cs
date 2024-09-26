using AutoMapper;
using CrossPlatformDownloadManager.Data.Services.DownloadFileService;
using CrossPlatformDownloadManager.Data.Services.DownloadQueueService;
using CrossPlatformDownloadManager.Data.Services.SettingsService;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;

namespace CrossPlatformDownloadManager.Data.Services.AppService;

public interface IAppService
{
    IMapper Mapper { get; }
    IUnitOfWork UnitOfWork { get; }
    IDownloadFileService DownloadFileService { get; }
    IDownloadQueueService DownloadQueueService { get; }
    ISettingsService SettingsService { get; }
}