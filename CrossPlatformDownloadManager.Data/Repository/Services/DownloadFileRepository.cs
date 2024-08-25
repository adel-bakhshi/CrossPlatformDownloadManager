using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Repository.Interfaces;
using SQLite;

namespace CrossPlatformDownloadManager.Data.Repository.Services;

public class DownloadFileRepository : RepositoryBase<DownloadFile>, IDownloadFileRepository
{
    public DownloadFileRepository(SQLiteConnection connection) : base(connection)
    {
    }
}