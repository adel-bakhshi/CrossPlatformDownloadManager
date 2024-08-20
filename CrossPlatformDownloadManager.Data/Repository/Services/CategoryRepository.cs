using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Repository.Interfaces;
using SQLite;

namespace CrossPlatformDownloadManager.Data.Repository.Services;

public class CategoryRepository : RepositoryBase<Category>, ICategoryRepository
{
    public CategoryRepository(SQLiteConnection connection) : base(connection)
    {
    }
}