using CrossPlatformDownloadManager.Data.DbContext;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;

namespace CrossPlatformDownloadManager.Data.Services.Repository.Services;

public class CategoryRepository : RepositoryBase<Category>, ICategoryRepository
{
    public CategoryRepository(DownloadManagerDbContext dbContext) : base(dbContext)
    {
    }

    public async Task UpdateAsync(Category? entity)
    {
        if (entity == null)
            return;

        var categoryInDb = await GetAsync(where: c => c.Id == entity.Id);
        categoryInDb?.UpdateData(entity);
    }

    public async Task UpdateAllAsync(IEnumerable<Category>? entities)
    {
        var categories = entities?.ToList();
        if (categories == null || categories.Count == 0)
            return;

        foreach (var entity in categories)
            await UpdateAsync(entity);
    }
}