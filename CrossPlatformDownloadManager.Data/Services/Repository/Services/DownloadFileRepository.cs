using CrossPlatformDownloadManager.Data.DbContext;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;

namespace CrossPlatformDownloadManager.Data.Services.Repository.Services;

public class DownloadFileRepository : RepositoryBase<DownloadFile>, IDownloadFileRepository
{
    public DownloadFileRepository(DownloadManagerDbContext dbContext) : base(dbContext)
    {
    }

    public async Task UpdateAsync(DownloadFile downloadFile)
    {
        var downloadFileInDb = await GetAsync(where: df => df.Id == downloadFile.Id);
        if (downloadFileInDb == null)
            return;

        downloadFileInDb.Url = downloadFile.Url;
        downloadFileInDb.FileName = downloadFile.FileName;
        downloadFileInDb.DownloadQueueId = downloadFile.DownloadQueueId;
        downloadFileInDb.Size = downloadFile.Size;
        downloadFileInDb.Description = downloadFile.Description;
        downloadFileInDb.Status = downloadFile.Status;
        downloadFileInDb.LastTryDate = downloadFile.LastTryDate;
        downloadFileInDb.DateAdded = downloadFile.DateAdded;
        downloadFileInDb.DownloadQueuePriority = downloadFile.DownloadQueuePriority;
        downloadFileInDb.CategoryId = downloadFile.CategoryId;
        downloadFileInDb.DownloadProgress = downloadFile.DownloadProgress;
        downloadFileInDb.ElapsedTime = downloadFile.ElapsedTime;
        downloadFileInDb.TimeLeft = downloadFile.TimeLeft;
        downloadFileInDb.TransferRate = downloadFile.TransferRate;
        downloadFileInDb.SaveLocation = downloadFile.SaveLocation;
        downloadFileInDb.DownloadPackage = downloadFile.DownloadPackage;
    }

    public async Task UpdateAllAsync(List<DownloadFile> downloadFiles)
    {
        if (!downloadFiles.Any())
            return;

        foreach (var downloadFile in downloadFiles)
            await UpdateAsync(downloadFile);
    }
}