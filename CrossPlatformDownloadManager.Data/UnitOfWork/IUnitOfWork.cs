using CrossPlatformDownloadManager.Data.Repository.Interfaces;
using SQLite;

namespace CrossPlatformDownloadManager.Data.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    ICategoryHeaderRepository CategoryHeaderRepository { get; }
    ICategoryRepository CategoryRepository { get; }
    ICategoryFileExtensionRepository CategoryFileExtensionRepository { get; }
    IDownloadQueueRepository DownloadQueueRepository { get; }
    ICategorySaveDirectoryRepository CategorySaveDirectoryRepository { get; }
    IDownloadFileRepository DownloadFileRepository { get; }

    Task CreateCategoriesAsync();
}