using CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;

namespace CrossPlatformDownloadManager.Data.Services.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    ICategoryHeaderRepository CategoryHeaderRepository { get; }
    ICategoryRepository CategoryRepository { get; }
    ICategoryFileExtensionRepository CategoryFileExtensionRepository { get; }
    IDownloadQueueRepository DownloadQueueRepository { get; }
    ICategorySaveDirectoryRepository CategorySaveDirectoryRepository { get; }
    IDownloadFileRepository DownloadFileRepository { get; }
    ISettingsRepository SettingsRepository { get; }
    IProxySettingsRepository ProxySettingsRepository { get; }

    Task SaveAsync();

    Task CreateDatabaseAsync();

    Task CreateCategoriesAsync();
}