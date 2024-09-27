namespace CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;

public interface IUpdateRepositoryBase<T> where T : class
{
    Task UpdateAsync(T? entity);

    Task UpdateAllAsync(IEnumerable<T>? entities);
}