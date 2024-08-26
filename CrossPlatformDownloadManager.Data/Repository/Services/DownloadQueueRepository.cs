using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Repository.Interfaces;
using SQLite;

namespace CrossPlatformDownloadManager.Data.Repository.Services;

public class DownloadQueueRepository : RepositoryBase<DownloadQueue>, IDownloadQueueRepository
{
    public DownloadQueueRepository(SQLiteAsyncConnection connection) : base(connection)
    {
    }
}