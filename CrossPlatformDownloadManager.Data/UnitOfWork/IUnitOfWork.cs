using CrossPlatformDownloadManager.Data.Repository.Interfaces;
using SQLite;

namespace CrossPlatformDownloadManager.Data.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    ICategoryRepository CategoryRepository { get; }
    ICategoryItemRepository CategoryItemRepository { get; }
    ICategoryItemFileExtensionRepository CategoryItemFileExtensionRepository { get; }
    IQueueRepository QueueRepository { get; }
    ICategoryItemSaveDirectoryRepository CategoryItemSaveDirectoryRepository { get; }

    void CreateCategories();
}