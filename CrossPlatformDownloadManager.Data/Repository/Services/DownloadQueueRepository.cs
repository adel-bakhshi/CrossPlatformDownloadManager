using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Repository.Interfaces;
using SQLite;

namespace CrossPlatformDownloadManager.Data.Repository.Services;

public class DownloadQueueRepository : RepositoryBase<DownloadQueue>, IDownloadQueueRepository
{
    public DownloadQueueRepository(SQLiteConnection connection) : base(connection)
    {
    }
}