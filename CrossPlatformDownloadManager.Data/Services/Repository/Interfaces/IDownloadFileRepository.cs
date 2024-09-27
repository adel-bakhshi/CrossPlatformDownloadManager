using CrossPlatformDownloadManager.Data.Models;

namespace CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;

public interface IDownloadFileRepository : IRepositoryBase<DownloadFile>, IUpdateRepositoryBase<DownloadFile>
{
}