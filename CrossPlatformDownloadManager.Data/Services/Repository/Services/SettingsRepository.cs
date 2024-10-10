using CrossPlatformDownloadManager.Data.DbContext;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;

namespace CrossPlatformDownloadManager.Data.Services.Repository.Services;

public class SettingsRepository : RepositoryBase<Settings>, ISettingsRepository
{
    public SettingsRepository(DownloadManagerDbContext dbContext) : base(dbContext)
    {
    }
}