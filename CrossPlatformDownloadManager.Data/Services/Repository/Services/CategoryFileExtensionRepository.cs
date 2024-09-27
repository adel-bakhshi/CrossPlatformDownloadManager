using CrossPlatformDownloadManager.Data.DbContext;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;

namespace CrossPlatformDownloadManager.Data.Services.Repository.Services;

public class CategoryFileExtensionRepository : RepositoryBase<CategoryFileExtension>,
    ICategoryFileExtensionRepository
{
    public CategoryFileExtensionRepository(DownloadManagerDbContext dbContext) : base(dbContext)
    {
    }

    public async Task UpdateAsync(CategoryFileExtension? entity)
    {
        if (entity == null)
            return;
        
        var categoryFileExtensionInDb = await GetAsync(where: fe => fe.Id == entity.Id);
        categoryFileExtensionInDb?.UpdateData(entity);
    }

    public async Task UpdateAllAsync(IEnumerable<CategoryFileExtension>? entities)
    {
        var categoryFileExtensions = entities?.ToList();
        if (categoryFileExtensions == null || categoryFileExtensions.Count == 0)
            return;

        foreach (var entity in categoryFileExtensions)
            await UpdateAsync(entity);
    }
}