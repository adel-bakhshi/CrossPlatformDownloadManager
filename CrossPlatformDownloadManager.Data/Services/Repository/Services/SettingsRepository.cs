using CrossPlatformDownloadManager.Data.DbContext;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;

namespace CrossPlatformDownloadManager.Data.Services.Repository.Services;

public class SettingsRepository : RepositoryBase<Settings>, ISettingsRepository
{
    public SettingsRepository(DownloadManagerDbContext dbContext) : base(dbContext)
    {
    }

    public async Task UpdateAsync(Settings? entity)
    {
        if (entity == null)
            return;

        var settingsInDb = await GetAsync(where: s => s.Id == entity.Id);
        settingsInDb?.UpdateData(entity);
    }

    public async Task UpdateAllAsync(IEnumerable<Settings>? entities)
    {
        var settings = entities?.ToList();
        if (settings == null || settings.Count == 0)
            return;

        foreach (var setting in settings)
            await UpdateAsync(setting);
    }
}