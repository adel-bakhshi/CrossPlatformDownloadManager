using CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;

namespace CrossPlatformDownloadManager.Data.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    ICategoryHeaderRepository CategoryHeaderRepository { get; }
    ICategoryRepository CategoryRepository { get; }
    ICategoryFileExtensionRepository CategoryFileExtensionRepository { get; }
    IDownloadQueueRepository DownloadQueueRepository { get; }
    ICategorySaveDirectoryRepository CategorySaveDirectoryRepository { get; }
    IDownloadFileRepository DownloadFileRepository { get; }

    Task SaveAsync();

    Task CreateCategoriesAsync();
}