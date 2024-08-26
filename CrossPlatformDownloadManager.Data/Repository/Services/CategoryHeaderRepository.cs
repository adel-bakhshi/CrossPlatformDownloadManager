using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Repository.Interfaces;
using SQLite;

namespace CrossPlatformDownloadManager.Data.Repository.Services;

public class CategoryHeaderRepository : RepositoryBase<CategoryHeader>, ICategoryHeaderRepository
{
    public CategoryHeaderRepository(SQLiteAsyncConnection connection) : base(connection)
    {
    }
}