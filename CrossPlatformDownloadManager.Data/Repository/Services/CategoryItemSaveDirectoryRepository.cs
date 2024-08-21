using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Repository.Interfaces;
using SQLite;

namespace CrossPlatformDownloadManager.Data.Repository.Services;

public class CategoryItemSaveDirectoryRepository : RepositoryBase<CategoryItemSaveDirectory>,
    ICategoryItemSaveDirectoryRepository
{
    public CategoryItemSaveDirectoryRepository(SQLiteConnection connection) : base(connection)
    {
    }
}