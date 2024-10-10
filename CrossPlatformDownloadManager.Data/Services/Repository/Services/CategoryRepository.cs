using CrossPlatformDownloadManager.Data.DbContext;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;

namespace CrossPlatformDownloadManager.Data.Services.Repository.Services;

public class CategoryRepository : RepositoryBase<Category>, ICategoryRepository
{
    public CategoryRepository(DownloadManagerDbContext dbContext) : base(dbContext)
    {
    }
}