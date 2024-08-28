using System.Linq.Expressions;

namespace CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;

public interface IRepositoryBase<T> where T : class, new()
{
    Task AddAsync(T? entity);

    Task AddRangeAsync(IEnumerable<T>? entities);

    Task<T?> GetAsync(Expression<Func<T, bool>>? where = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        params string[] includeProperties);

    Task<TR?> GetAsync<TR>(Expression<Func<T, bool>>? where = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Expression<Func<T, TR>>? select = null,
        params string[] includeProperties);

    Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? where = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        params string[] includeProperties);

    Task<List<TR>> GetAllAsync<TR>(Expression<Func<T, bool>>? where = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Expression<Func<T, TR>>? select = null,
        params string[] includeProperties);

    void Delete(T? entity);

    void DeleteAll(IEnumerable<T>? entities);
}