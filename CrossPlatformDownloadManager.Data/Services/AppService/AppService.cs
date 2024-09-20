using AutoMapper;
using CrossPlatformDownloadManager.Data.Services.DownloadFileService;
using CrossPlatformDownloadManager.Data.Services.SettingsService;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;

namespace CrossPlatformDownloadManager.Data.Services.AppService;

public class AppService : IAppService
{
    #region Properties

    public IUnitOfWork UnitOfWork { get; }
    public IDownloadFileService DownloadFileService { get; }
    public IMapper Mapper { get; }
    public ISettingsService SettingsService { get; }

    #endregion

    public AppService(IUnitOfWork unitOfWork, IDownloadFileService downloadFileService, IMapper mapper,
        ISettingsService settingsService)
    {
        UnitOfWork = unitOfWork;
        DownloadFileService = downloadFileService;
        Mapper = mapper;
        SettingsService = settingsService;
    }
}