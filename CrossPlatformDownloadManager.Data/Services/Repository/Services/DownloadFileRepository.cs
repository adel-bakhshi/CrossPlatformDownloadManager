using CrossPlatformDownloadManager.Data.DbContext;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;

namespace CrossPlatformDownloadManager.Data.Services.Repository.Services;

public class DownloadFileRepository : RepositoryBase<DownloadFile>, IDownloadFileRepository
{
    public DownloadFileRepository(DownloadManagerDbContext dbContext) : base(dbContext)
    {
    }

    public async Task UpdateAsync(DownloadFile? entity)
    {
        if (entity == null)
            return;

        var downloadFileInDb = await GetAsync(where: df => df.Id == entity.Id);
        downloadFileInDb?.UpdateData(entity);
    }

    public async Task UpdateAllAsync(IEnumerable<DownloadFile>? entities)
    {
        var downloadFiles = entities?.ToList();
        if (downloadFiles == null || downloadFiles.Count == 0)
            return;

        foreach (var downloadFile in downloadFiles)
            await UpdateAsync(downloadFile);
    }
}