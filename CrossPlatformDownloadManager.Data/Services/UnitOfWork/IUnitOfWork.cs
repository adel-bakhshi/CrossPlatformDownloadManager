using CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace CrossPlatformDownloadManager.Data.Services.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    #region Properties

    ICategoryHeaderRepository CategoryHeaderRepository { get; }
    ICategoryRepository CategoryRepository { get; }
    ICategoryFileExtensionRepository CategoryFileExtensionRepository { get; }
    IDownloadQueueRepository DownloadQueueRepository { get; }
    ICategorySaveDirectoryRepository CategorySaveDirectoryRepository { get; }
    IDownloadFileRepository DownloadFileRepository { get; }
    ISettingsRepository SettingsRepository { get; }
    IProxySettingsRepository ProxySettingsRepository { get; }

    #endregion

    Task SaveAsync();

    Task CreateDatabaseAsync();

    Task CreateCategoriesAsync();

    Task<IDbContextTransaction?> BeginTransactionAsync();
    
    Task CommitTransactionAsync();
    
    Task RollbackTransactionAsync();
}