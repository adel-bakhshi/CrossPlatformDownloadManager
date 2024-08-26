using System.Linq.Expressions;
using CrossPlatformDownloadManager.Data.Repository.Interfaces;
using SQLite;

namespace CrossPlatformDownloadManager.Data.Repository.Services;

public class RepositoryBase<T> : IRepositoryBase<T> where T : class, new()
{
    #region Private Fields

    private readonly SQLiteAsyncConnection _connection;

    #endregion

    public RepositoryBase(SQLiteAsyncConnection connection)
    {
        _connection = connection;
        _connection.CreateTableAsync<T>().GetAwaiter().GetResult();
    }

    public async Task AddAsync(T? entity)
    {
        if (entity == null)
            return;

        await _connection.InsertAsync(entity);
    }

    public async Task AddRangeAsync(IEnumerable<T>? entities)
    {
        var objects = entities?.ToList() ?? [];
        if (!objects.Any())
            return;

        await _connection.InsertAllAsync(objects);
    }

    public async Task<T?> GetAsync(Expression<Func<T, bool>>? where = null)
    {
        return (await GetEntitiesAsync<T>(where)).FirstOrDefault();
    }

    public async Task<T?> GetAsync<TU>(Expression<Func<T, bool>>? where = null, Expression<Func<T, TU>>? orderBy = null)
    {
        return (await GetEntitiesAsync(where, orderBy)).FirstOrDefault();
    }

    public async Task<TR?> GetAsync<TR>(Expression<Func<T, bool>>? where = null, Func<T, TR>? select = null)
    {
        if (select == null)
            return default;

        return (await GetEntitiesAsync<T>(where))
            .Select(select)
            .FirstOrDefault();
    }

    public async Task<TR?> GetAsync<TU, TR>(Expression<Func<T, bool>>? where = null,
        Expression<Func<T, TU>>? orderBy = null,
        Func<T, TR>? select = null)
    {
        if (select == null)
            return default;

        return (await GetEntitiesAsync(where, orderBy))
            .Select(select)
            .FirstOrDefault();
    }

    public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? where = null)
    {
        return await GetEntitiesAsync<T>(where);
    }

    public async Task<List<T>> GetAllAsync<TU>(Expression<Func<T, bool>>? where = null,
        Expression<Func<T, TU>>? orderBy = null)
    {
        return await GetEntitiesAsync(where, orderBy);
    }

    public async Task<List<TR>> GetAllAsync<TR>(Expression<Func<T, bool>>? where = null, Func<T, TR>? select = null)
    {
        if (select == null)
            return [];

        return (await GetEntitiesAsync<T>(where))
            .Select(select)
            .ToList();
    }

    public async Task<List<TR>> GetAllAsync<TU, TR>(Expression<Func<T, bool>>? where = null,
        Expression<Func<T, TU>>? orderBy = null,
        Func<T, TR>? select = null)
    {
        if (select == null)
            return [];

        return (await GetEntitiesAsync(where, orderBy))
            .Select(select)
            .ToList();
    }

    public async Task UpdateAsync(T? entity)
    {
        if (entity == null)
            return;

        await _connection.UpdateAsync(entity);
    }

    public async Task UpdateAllAsync(IEnumerable<T>? entities, bool runInTransaction = false)
    {
        var objects = entities?.ToList() ?? [];
        if (!objects.Any())
            return;

        await _connection.UpdateAllAsync(objects, runInTransaction);
    }

    public async Task DeleteAsync(T? entity)
    {
        if (entity == null)
            return;

        await _connection.DeleteAsync(entity);
    }

    public async Task DeleteAsync(object? primaryKey)
    {
        if (primaryKey == null)
            return;

        await _connection.DeleteAsync<T>(primaryKey);
    }

    public async Task DeleteAllAsync(IEnumerable<T>? entities)
    {
        var objects = entities?.ToList() ?? [];
        if (!objects.Any())
            return;

        foreach (var obj in objects)
            await DeleteAsync(obj);
    }

    public async Task DeleteAllAsync(IEnumerable<object>? primaryKeys)
    {
        var objects = primaryKeys?.ToList() ?? [];
        if (!objects.Any())
            return;

        foreach (var obj in objects)
            await DeleteAsync(obj);
    }

    #region Helpers

    private async Task<List<T>> GetEntitiesAsync<TU>(Expression<Func<T, bool>>? where = null,
        Expression<Func<T, TU>>? orderBy = null)
    {
        var table = _connection.Table<T>();

        if (where != null)
            table = table.Where(where);

        if (orderBy != null)
            table = table.OrderBy(orderBy);

        return await table.ToListAsync();
    }

    #endregion
}