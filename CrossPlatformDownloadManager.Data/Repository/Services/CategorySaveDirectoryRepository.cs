using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Repository.Interfaces;
using SQLite;

namespace CrossPlatformDownloadManager.Data.Repository.Services;

public class CategorySaveDirectoryRepository : RepositoryBase<CategorySaveDirectory>,
    ICategorySaveDirectoryRepository
{
    public CategorySaveDirectoryRepository(SQLiteAsyncConnection connection) : base(connection)
    {
    }
}