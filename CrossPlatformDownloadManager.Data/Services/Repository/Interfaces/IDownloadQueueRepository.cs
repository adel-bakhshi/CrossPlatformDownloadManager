using CrossPlatformDownloadManager.Data.Models;

namespace CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;

public interface IDownloadQueueRepository : IRepositoryBase<DownloadQueue>
{
    Task UpdateAsync(DownloadQueue downloadQueue);

    Task UpdateAllAsync(List<DownloadQueue> downloadQueues);
}