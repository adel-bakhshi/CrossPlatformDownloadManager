using CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace CrossPlatformDownloadManager.Data.Services.UnitOfWork;

/// <summary>
/// Represents the unit of work interface for accessing database.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    #region Properties

    /// <summary>
    /// Gets a value that indicates the permission to access the category repository. 
    /// </summary>
    ICategoryHeaderRepository CategoryHeaderRepository { get; }

    /// <summary>
    /// Gets a value that indicates the permission to access the category repository.
    /// </summary>
    ICategoryRepository CategoryRepository { get; }

    /// <summary>
    /// Gets a value that indicates the permission to access the category file extension repository.
    /// </summary>
    ICategoryFileExtensionRepository CategoryFileExtensionRepository { get; }

    /// <summary>
    /// Gets a value that indicates the permission to access the download queue repository.
    /// </summary>
    IDownloadQueueRepository DownloadQueueRepository { get; }

    /// <summary>
    /// Gets a value that indicates the permission to access the category save directory repository.
    /// </summary>
    ICategorySaveDirectoryRepository CategorySaveDirectoryRepository { get; }

    /// <summary>
    /// Gets a value that indicates the permission to access the download file repository.
    /// </summary>
    IDownloadFileRepository DownloadFileRepository { get; }

    /// <summary>
    /// Gets a value that indicates the permission to access the settings repository.
    /// </summary>
    ISettingsRepository SettingsRepository { get; }

    /// <summary>
    /// Gets a value that indicates the permission to access the proxy settings repository.
    /// </summary>
    IProxySettingsRepository ProxySettingsRepository { get; }

    #endregion

    /// <summary>
    /// Saves all changes made in this unit of work asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    Task SaveAsync();

    /// <summary>
    /// Creates the database and apply migrations asynchronously.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    Task CreateDatabaseAsync();

    /// <summary>
    /// Creates the categories table in the database asynchronously.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    Task CreateCategoriesAsync();

    /// <summary>
    /// Begins a new transaction asynchronously.
    /// </summary>
    /// <returns>A Task that represents the asynchronous operation, returning the transaction.</returns>
    Task<IDbContextTransaction?> BeginTransactionAsync();

    /// <summary>
    /// Commits the transaction asynchronously.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    Task CommitTransactionAsync();

    /// <summary>
    /// Rolls back the transaction asynchronously.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    Task RollbackTransactionAsync();
}