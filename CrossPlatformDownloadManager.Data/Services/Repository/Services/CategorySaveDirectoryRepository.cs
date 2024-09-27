using CrossPlatformDownloadManager.Data.DbContext;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;

namespace CrossPlatformDownloadManager.Data.Services.Repository.Services;

public class CategorySaveDirectoryRepository : RepositoryBase<CategorySaveDirectory>,
    ICategorySaveDirectoryRepository
{
    public CategorySaveDirectoryRepository(DownloadManagerDbContext dbContext) : base(dbContext)
    {
    }

    public async Task UpdateAsync(CategorySaveDirectory? entity)
    {
        if (entity == null)
            return;

        var categorySaveDirectoryInDb = await GetAsync(where: csd => csd.Id == entity.Id);
        categorySaveDirectoryInDb?.UpdateData(entity);
    }

    public async Task UpdateAllAsync(IEnumerable<CategorySaveDirectory>? entities)
    {
        var categorySaveDirectories = entities?.ToList();
        if (categorySaveDirectories == null || categorySaveDirectories.Count == 0)
            return;

        foreach (var entity in categorySaveDirectories)
            await UpdateAsync(entity);
    }
}