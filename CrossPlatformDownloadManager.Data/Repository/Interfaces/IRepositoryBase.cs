using System.Linq.Expressions;

namespace CrossPlatformDownloadManager.Data.Repository.Interfaces;

public interface IRepositoryBase<T> where T : class, new()
{
    void Add(T? entity);

    void AddRange(IEnumerable<T>? entities);

    T? Get(Expression<Func<T, bool>>? where = null);

    T? Get<TU>(Expression<Func<T, bool>>? where = null, Expression<Func<T, TU>>? orderBy = null);

    TR? Get<TR>(Expression<Func<T, bool>>? where = null, Func<T, TR>? select = null);

    TR? Get<TU, TR>(Expression<Func<T, bool>>? where = null, Expression<Func<T, TU>>? orderBy = null,
        Func<T, TR>? select = null);

    List<T> GetAll(Expression<Func<T, bool>>? where = null);

    List<T> GetAll<TU>(Expression<Func<T, bool>>? where = null, Expression<Func<T, TU>>? orderBy = null);

    List<TR> GetAll<TR>(Expression<Func<T, bool>>? where = null, Func<T, TR>? select = null);

    List<TR> GetAll<TU, TR>(Expression<Func<T, bool>>? where = null, Expression<Func<T, TU>>? orderBy = null,
        Func<T, TR>? select = null);

    void Update(T? entity);

    void UpdateAll(IEnumerable<T>? entities, bool runInTransaction = false);

    void Delete(T? entity);

    void Delete(object? primaryKey);

    void DeleteAll(IEnumerable<T>? entities);

    void DeleteAll(IEnumerable<object>? primaryKeys);
}