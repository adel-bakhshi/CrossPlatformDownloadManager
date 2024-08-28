using CrossPlatformDownloadManager.Data.DbContext;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;

namespace CrossPlatformDownloadManager.Data.Services.Repository.Services;

public class CategoryFileExtensionRepository : RepositoryBase<CategoryFileExtension>,
    ICategoryFileExtensionRepository
{
    public CategoryFileExtensionRepository(DownloadManagerDbContext dbContext) : base(dbContext)
    {
    }
}