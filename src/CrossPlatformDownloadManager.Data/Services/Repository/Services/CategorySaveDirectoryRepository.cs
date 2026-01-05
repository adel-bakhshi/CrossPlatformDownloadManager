using CrossPlatformDownloadManager.Data.DbContext;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;

namespace CrossPlatformDownloadManager.Data.Services.Repository.Services;

public class CategorySaveDirectoryRepository : RepositoryBase<CategorySaveDirectory>,
    ICategorySaveDirectoryRepository
{
    public CategorySaveDirectoryRepository(DownloadManagerDbContext dbContext) : base(dbContext)
    {
    }
}