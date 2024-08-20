using System.Linq.Expressions;
using CrossPlatformDownloadManager.Data.Repository.Interfaces;
using SQLite;

namespace CrossPlatformDownloadManager.Data.Repository.Services;

public class RepositoryBase<T> : IRepositoryBase<T> where T : class, new()
{
    #region Private Fields

    private readonly SQLiteConnection _connection;

    #endregion

    public RepositoryBase(SQLiteConnection connection)
    {
        _connection = connection;
        _connection.CreateTable<T>();
    }

    public void Add(T? entity)
    {
        if (entity == null)
            return;

        _connection.Insert(entity);
    }

    public void AddRange(IEnumerable<T>? entities)
    {
        var objects = entities?.ToList() ?? [];
        if (!objects.Any())
            return;

        _connection.InsertAll(objects);
    }

    public T? Get(Expression<Func<T, bool>>? where = null)
    {
        return GetEntities<T>(where).FirstOrDefault();
    }

    public T? Get<TU>(Expression<Func<T, bool>>? where = null, Expression<Func<T, TU>>? orderBy = null)
    {
        return GetEntities(where, orderBy).FirstOrDefault();
    }

    public TR? Get<TR>(Expression<Func<T, bool>>? where = null, Func<T, TR>? select = null)
    {
        if (select == null)
            return default;

        return GetEntities<T>(where)
            .Select(select)
            .FirstOrDefault();
    }

    public TR? Get<TU, TR>(Expression<Func<T, bool>>? where = null, Expression<Func<T, TU>>? orderBy = null,
        Func<T, TR>? select = null)
    {
        if (select == null)
            return default;

        return GetEntities(where, orderBy)
            .Select(select)
            .FirstOrDefault();
    }

    public List<T> GetAll(Expression<Func<T, bool>>? where = null)
    {
        return GetEntities<T>(where);
    }

    public List<T> GetAll<TU>(Expression<Func<T, bool>>? where = null, Expression<Func<T, TU>>? orderBy = null)
    {
        return GetEntities(where, orderBy);
    }

    public List<TR> GetAll<TR>(Expression<Func<T, bool>>? where = null, Func<T, TR>? select = null)
    {
        if (select == null)
            return [];

        return GetEntities<T>(where)
            .Select(select)
            .ToList();
    }

    public List<TR> GetAll<TU, TR>(Expression<Func<T, bool>>? where = null, Expression<Func<T, TU>>? orderBy = null,
        Func<T, TR>? select = null)
    {
        if (select == null)
            return [];

        return GetEntities(where, orderBy)
            .Select(select)
            .ToList();
    }

    public void Update(T? entity)
    {
        if (entity == null)
            return;

        _connection.Update(entity);
    }

    public void UpdateAll(IEnumerable<T>? entities, bool runInTransaction = false)
    {
        var objects = entities?.ToList() ?? [];
        if (!objects.Any())
            return;

        _connection.UpdateAll(objects, runInTransaction);
    }

    public void Delete(T? entity)
    {
        if (entity == null)
            return;

        _connection.Delete(entity);
    }

    public void Delete(object? primaryKey)
    {
        if (primaryKey == null)
            return;

        _connection.Delete<T>(primaryKey);
    }

    public void DeleteAll(IEnumerable<T>? entities)
    {
        var objects = entities?.ToList() ?? [];
        if (!objects.Any())
            return;

        foreach (var obj in objects)
            Delete(obj);
    }

    public void DeleteAll(IEnumerable<object>? primaryKeys)
    {
        var objects = primaryKeys?.ToList() ?? [];
        if (!objects.Any())
            return;

        foreach (var obj in objects)
            Delete(obj);
    }

    #region Helpers

    private List<T> GetEntities<TU>(Expression<Func<T, bool>>? where = null, Expression<Func<T, TU>>? orderBy = null)
    {
        var table = _connection.Table<T>();

        if (where != null)
            table = table.Where(where);

        if (orderBy != null)
            table = table.OrderBy(orderBy);

        return table.ToList();
    }

    #endregion
}