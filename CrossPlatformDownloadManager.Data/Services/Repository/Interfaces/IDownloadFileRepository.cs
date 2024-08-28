using CrossPlatformDownloadManager.Data.Models;

namespace CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;

public interface IDownloadFileRepository : IRepositoryBase<DownloadFile>
{
    Task UpdateAsync(DownloadFile downloadFile);

    Task UpdateAllAsync(List<DownloadFile> downloadFiles);
}