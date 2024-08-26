using System.Linq.Expressions;

namespace CrossPlatformDownloadManager.Data.Repository.Interfaces;

public interface IRepositoryBase<T> where T : class, new()
{
    Task AddAsync(T? entity);

    Task AddRangeAsync(IEnumerable<T>? entities);

    Task<T?> GetAsync(Expression<Func<T, bool>>? where = null);

    Task<T?> GetAsync<TU>(Expression<Func<T, bool>>? where = null, Expression<Func<T, TU>>? orderBy = null);

    Task<TR?> GetAsync<TR>(Expression<Func<T, bool>>? where = null, Func<T, TR>? select = null);

    Task<TR?> GetAsync<TU, TR>(Expression<Func<T, bool>>? where = null, Expression<Func<T, TU>>? orderBy = null,
        Func<T, TR>? select = null);

    Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? where = null);

    Task<List<T>> GetAllAsync<TU>(Expression<Func<T, bool>>? where = null, Expression<Func<T, TU>>? orderBy = null);

    Task<List<TR>> GetAllAsync<TR>(Expression<Func<T, bool>>? where = null, Func<T, TR>? select = null);

    Task<List<TR>> GetAllAsync<TU, TR>(Expression<Func<T, bool>>? where = null, Expression<Func<T, TU>>? orderBy = null,
        Func<T, TR>? select = null);

    Task UpdateAsync(T? entity);

    Task UpdateAllAsync(IEnumerable<T>? entities, bool runInTransaction = false);

    Task DeleteAsync(T? entity);

    Task DeleteAsync(object? primaryKey);

    Task DeleteAllAsync(IEnumerable<T>? entities);

    Task DeleteAllAsync(IEnumerable<object>? primaryKeys);
}