using CrossPlatformDownloadManager.Data.DbContext;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;

namespace CrossPlatformDownloadManager.Data.Services.Repository.Services;

public class CategoryRepository : RepositoryBase<Category>, ICategoryRepository
{
    public CategoryRepository(DownloadManagerDbContext dbContext) : base(dbContext)
    {
    }

    public async Task UpdateAsync(Category category)
    {
        var categoryInDb = await GetAsync(where: c => c.Id == category.Id);
        if (categoryInDb == null)
            return;

        categoryInDb.Title = category.Title;
        categoryInDb.Icon = category.Icon;
        categoryInDb.IsDefault = category.IsDefault;
        categoryInDb.AutoAddLinkFromSites = category.AutoAddLinkFromSites;
        categoryInDb.CategorySaveDirectoryId = category.CategorySaveDirectoryId;
    }
}