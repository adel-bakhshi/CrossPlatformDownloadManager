using CrossPlatformDownloadManager.Data.DbContext;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;

namespace CrossPlatformDownloadManager.Data.Services.Repository.Services;

public class DownloadQueueRepository : RepositoryBase<DownloadQueue>, IDownloadQueueRepository
{
    public DownloadQueueRepository(DownloadManagerDbContext dbContext) : base(dbContext)
    {
    }

    public async Task UpdateAsync(DownloadQueue downloadQueue)
    {
        var downloadQueueInDb = await GetAsync(where: dq => dq.Id == downloadQueue.Id);
        if (downloadQueueInDb == null)
            return;

        downloadQueueInDb.Title = downloadQueue.Title;
        downloadQueueInDb.StartOnApplicationStartup = downloadQueue.StartOnApplicationStartup;
        downloadQueueInDb.StartDownloadSchedule = downloadQueue.StartDownloadSchedule;
        downloadQueueInDb.StopDownloadSchedule = downloadQueue.StopDownloadSchedule;
        downloadQueueInDb.IsDaily = downloadQueue.IsDaily;
        downloadQueueInDb.JustForDate = downloadQueue.JustForDate;
        downloadQueueInDb.DaysOfWeek = downloadQueue.DaysOfWeek;
        downloadQueueInDb.RetryOnDownloadingFailed = downloadQueue.RetryOnDownloadingFailed;
        downloadQueueInDb.RetryCount = downloadQueue.RetryCount;
        downloadQueueInDb.ShowAlarmWhenDone = downloadQueue.ShowAlarmWhenDone;
        downloadQueueInDb.ExitProgramWhenDone = downloadQueue.ExitProgramWhenDone;
        downloadQueueInDb.TurnOffComputerWhenDone = downloadQueue.TurnOffComputerWhenDone;
        downloadQueueInDb.TurnOffComputerMode = downloadQueue.TurnOffComputerMode;
        downloadQueueInDb.IsDefault = downloadQueue.IsDefault;
    }

    public async Task UpdateAllAsync(List<DownloadQueue> downloadQueues)
    {
        if (!downloadQueues.Any())
            return;

        foreach (var downloadQueue in downloadQueues)
            await UpdateAsync(downloadQueue);
    }
}