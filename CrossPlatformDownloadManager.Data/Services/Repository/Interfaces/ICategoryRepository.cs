using CrossPlatformDownloadManager.Data.Models;

namespace CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;

public interface ICategoryRepository : IRepositoryBase<Category>
{
    Task UpdateAsync(Category category);
}