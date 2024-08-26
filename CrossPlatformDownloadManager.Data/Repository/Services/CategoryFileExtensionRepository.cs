using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Repository.Interfaces;
using SQLite;

namespace CrossPlatformDownloadManager.Data.Repository.Services;

public class CategoryFileExtensionRepository : RepositoryBase<CategoryFileExtension>,
    ICategoryFileExtensionRepository
{
    public CategoryFileExtensionRepository(SQLiteAsyncConnection connection) : base(connection)
    {
    }
}