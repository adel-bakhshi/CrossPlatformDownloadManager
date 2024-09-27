using CrossPlatformDownloadManager.Data.DbContext;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;

namespace CrossPlatformDownloadManager.Data.Services.Repository.Services;

public class CategoryHeaderRepository : RepositoryBase<CategoryHeader>, ICategoryHeaderRepository
{
    public CategoryHeaderRepository(DownloadManagerDbContext dbContext) : base(dbContext)
    {
    }

    public async Task UpdateAsync(CategoryHeader? entity)
    {
        if (entity == null)
            return;

        var categoryHeaderInDb = await GetAsync(where: ch => ch.Id == entity.Id);
        categoryHeaderInDb?.UpdateData(entity);
    }

    public async Task UpdateAllAsync(IEnumerable<CategoryHeader>? entities)
    {
        var categoryHeaders = entities?.ToList();
        if (categoryHeaders == null || categoryHeaders.Count == 0)
            return;

        foreach (var entity in categoryHeaders)
            await UpdateAsync(entity);
    }
}