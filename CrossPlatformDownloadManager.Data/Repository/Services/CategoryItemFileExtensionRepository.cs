using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Repository.Interfaces;
using SQLite;

namespace CrossPlatformDownloadManager.Data.Repository.Services;

public class CategoryItemFileExtensionRepository : RepositoryBase<CategoryItemFileExtension>,
    ICategoryItemFileExtensionRepository
{
    public CategoryItemFileExtensionRepository(SQLiteConnection connection) : base(connection)
    {
    }
}