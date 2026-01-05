using CrossPlatformDownloadManager.Data.DbContext;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;

namespace CrossPlatformDownloadManager.Data.Services.Repository.Services;

public class DownloadQueueRepository : RepositoryBase<DownloadQueue>, IDownloadQueueRepository
{
    public DownloadQueueRepository(DownloadManagerDbContext dbContext) : base(dbContext)
    {
    }
}