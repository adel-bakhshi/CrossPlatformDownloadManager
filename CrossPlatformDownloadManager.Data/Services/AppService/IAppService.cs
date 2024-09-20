using AutoMapper;
using CrossPlatformDownloadManager.Data.Services.DownloadFileService;
using CrossPlatformDownloadManager.Data.Services.SettingsService;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;

namespace CrossPlatformDownloadManager.Data.Services.AppService;

public interface IAppService
{
    IUnitOfWork UnitOfWork { get; }
    IDownloadFileService DownloadFileService { get; }
    IMapper Mapper { get; }
    ISettingsService SettingsService { get; }
}