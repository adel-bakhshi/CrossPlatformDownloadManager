using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Repository.Interfaces;
using SQLite;

namespace CrossPlatformDownloadManager.Data.Repository.Services;

public class QueueRepository : RepositoryBase<Queue>, IQueueRepository
{
    public QueueRepository(SQLiteConnection connection) : base(connection)
    {
    }
}