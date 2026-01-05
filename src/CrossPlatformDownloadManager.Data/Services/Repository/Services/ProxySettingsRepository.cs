using CrossPlatformDownloadManager.Data.DbContext;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;

namespace CrossPlatformDownloadManager.Data.Services.Repository.Services;

public class ProxySettingsRepository : RepositoryBase<ProxySettings>, IProxySettingsRepository
{
    public ProxySettingsRepository(DownloadManagerDbContext dbContext) : base(dbContext)
    {
    }
}