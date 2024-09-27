using CrossPlatformDownloadManager.Data.DbContext;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;

namespace CrossPlatformDownloadManager.Data.Services.Repository.Services;

public class DownloadQueueRepository : RepositoryBase<DownloadQueue>, IDownloadQueueRepository
{
    public DownloadQueueRepository(DownloadManagerDbContext dbContext) : base(dbContext)
    {
    }

    public async Task UpdateAsync(DownloadQueue? entity)
    {
        if (entity == null)
            return;

        var downloadQueueInDb = await GetAsync(where: dq => dq.Id == entity.Id);
        downloadQueueInDb?.UpdateData(entity);
    }

    public async Task UpdateAllAsync(IEnumerable<DownloadQueue>? entities)
    {
        var downloadQueues = entities?.ToList();
        if (downloadQueues == null || downloadQueues.Count == 0)
            return;

        foreach (var downloadQueue in downloadQueues)
            await UpdateAsync(downloadQueue);
    }
}