using CrossPlatformDownloadManager.Data.DbContext;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;

namespace CrossPlatformDownloadManager.Data.Services.Repository.Services;

public class DownloadFileRepository : RepositoryBase<DownloadFile>, IDownloadFileRepository
{
    public DownloadFileRepository(DownloadManagerDbContext dbContext) : base(dbContext)
    {
    }
}