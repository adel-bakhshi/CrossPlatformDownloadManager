using AutoMapper;
using CrossPlatformDownloadManager.Data.Services.DownloadFileService;
using CrossPlatformDownloadManager.Data.Services.DownloadQueueService;
using CrossPlatformDownloadManager.Data.Services.SettingsService;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;

namespace CrossPlatformDownloadManager.Data.Services.AppService;

public class AppService : IAppService
{
    #region Properties

    public IMapper Mapper { get; }
    public IUnitOfWork UnitOfWork { get; }
    public IDownloadFileService DownloadFileService { get; }
    public IDownloadQueueService DownloadQueueService { get; }
    public ISettingsService SettingsService { get; }

    #endregion

    public AppService(IUnitOfWork unitOfWork, IDownloadFileService downloadFileService, IMapper mapper,
        ISettingsService settingsService, IDownloadQueueService downloadQueueService)
    {
        Mapper = mapper;
        UnitOfWork = unitOfWork;
        DownloadFileService = downloadFileService;
        DownloadQueueService = downloadQueueService;
        SettingsService = settingsService;
    }
}