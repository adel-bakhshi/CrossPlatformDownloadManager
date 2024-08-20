using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Repository.Interfaces;
using SQLite;

namespace CrossPlatformDownloadManager.Data.Repository.Services;

public class CategoryItemRepository : RepositoryBase<CategoryItem>, ICategoryItemRepository
{
    public CategoryItemRepository(SQLiteConnection connection) : base(connection)
    {
    }
}