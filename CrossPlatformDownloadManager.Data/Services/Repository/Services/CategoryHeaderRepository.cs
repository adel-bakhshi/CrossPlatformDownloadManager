using CrossPlatformDownloadManager.Data.DbContext;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;

namespace CrossPlatformDownloadManager.Data.Services.Repository.Services;

public class CategoryHeaderRepository : RepositoryBase<CategoryHeader>, ICategoryHeaderRepository
{
    public CategoryHeaderRepository(DownloadManagerDbContext dbContext) : base(dbContext)
    {
    }
}