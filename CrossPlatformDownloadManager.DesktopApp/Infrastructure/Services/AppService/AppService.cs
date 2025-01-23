using AutoMapper;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.CategoryService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadQueueService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.SettingsService;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;

public class AppService : IAppService
{
    #region Properties

    public IMapper Mapper { get; }
    public IUnitOfWork UnitOfWork { get; }
    public IDownloadFileService DownloadFileService { get; }
    public IDownloadQueueService DownloadQueueService { get; }
    public ISettingsService SettingsService { get; }
    public ICategoryService CategoryService { get; }

    #endregion

    public AppService(IUnitOfWork unitOfWork, IDownloadFileService downloadFileService, IMapper mapper,
        ISettingsService settingsService, IDownloadQueueService downloadQueueService, ICategoryService categoryService)
    {
        Mapper = mapper;
        UnitOfWork = unitOfWork;
        DownloadFileService = downloadFileService;
        DownloadQueueService = downloadQueueService;
        CategoryService = categoryService;
        SettingsService = settingsService;
    }
}