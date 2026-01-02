using System.Linq.Expressions;
using CrossPlatformDownloadManager.Data.DbContext;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CrossPlatformDownloadManager.Data.Services.Repository.Services;

public class RepositoryBase<T> : IRepositoryBase<T> where T : DbModelBase
{
    #region Private Fields

    private readonly DbSet<T> _table;

    #endregion

    protected RepositoryBase(DownloadManagerDbContext dbContext)
    {
        _table = dbContext.Set<T>() ?? throw new InvalidOperationException("Entity not found.");
    }

    public async Task AddAsync(T? entity)
    {
        if (entity == null)
            return;

        await _table.AddAsync(entity);
    }

    public async Task AddRangeAsync(IEnumerable<T>? entities)
    {
        var objects = entities?.ToList() ?? [];
        if (objects.Count == 0)
            return;

        await _table.AddRangeAsync(objects);
    }

    public async Task<T?> GetAsync(Expression<Func<T, bool>>? where = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        params string[] includeProperties)
    {
        var query = _table.AsQueryable();

        if (includeProperties.Length != 0)
        {
            foreach (var includeProperty in includeProperties)
                query = query.Include(includeProperty);
        }

        if (where != null)
            query = query.Where(where);

        if (orderBy != null)
            query = orderBy(query);

        return await query.FirstOrDefaultAsync();
    }

    public async Task<TR?> GetAsync<TR>(Expression<Func<T, bool>>? where = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<T, TR>? select = null,
        params string[] includeProperties)
    {
        if (select == null)
            return default;

        var query = _table.AsQueryable();

        if (includeProperties.Length != 0)
        {
            foreach (var includeProperty in includeProperties)
                query = query.Include(includeProperty);
        }

        if (where != null)
            query = query.Where(where);

        if (orderBy != null)
            query = orderBy(query);

        var data = await query.ToListAsync();
        return data.Select(select).FirstOrDefault();
    }

    public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? where = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        bool distinct = false,
        params string[] includeProperties)
    {
        var query = _table.AsQueryable();

        if (includeProperties.Length != 0)
        {
            foreach (var includeProperty in includeProperties)
                query = query.Include(includeProperty);
        }

        if (where != null)
            query = query.Where(where);

        if (orderBy != null)
            query = orderBy(query);

        if (distinct)
            query = query.Distinct();

        return await query.ToListAsync();
    }

    public async Task<List<TR>> GetAllAsync<TR>(Expression<Func<T, bool>>? where = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<T, TR>? select = null,
        bool distinct = false,
        params string[] includeProperties)
    {
        if (select == null)
            return [];

        var query = _table.AsQueryable();

        if (includeProperties.Length != 0)
        {
            foreach (var includeProperty in includeProperties)
                query = query.Include(includeProperty);
        }

        if (where != null)
            query = query.Where(where);

        if (orderBy != null)
            query = orderBy(query);

        if (distinct)
            query = query.Distinct();

        var data = await query.ToListAsync();
        return data.Select(select).ToList();
    }

    public async Task DeleteAsync(T? entity)
    {
        if (entity == null)
            return;
        
        var entityInDb = await GetAsync(where: e => e.Id == entity.Id);
        if (entityInDb == null)
            return;

        _table.Remove(entityInDb);
    }

    public async Task DeleteAllAsync(IEnumerable<T>? entities)
    {
        var objects = entities?.ToList() ?? [];
        if (objects.Count == 0)
            return;
        
        var primaryKeys = objects.ConvertAll(o => o.Id);
        var entitiesInDb = await GetAllAsync(where: e => primaryKeys.Contains(e.Id));
        if (entitiesInDb.Count == 0)
            return;
        
        _table.RemoveRange(entitiesInDb);
    }

    public async Task UpdateAsync(T? entity)
    {
        if (entity == null)
            return;

        var entityInDb = await GetAsync(where: e => e.Id == entity.Id);
        if (entityInDb == null)
            return;
        
        entityInDb.UpdateDbModel(entity);
        _table.Update(entityInDb);
    }

    public async Task UpdateAllAsync(IEnumerable<T>? entities)
    {
        var objects = entities?.ToList() ?? [];
        if (objects.Count == 0)
            return;

        var primaryKeys = objects.ConvertAll(o => o.Id);
        var entitiesInDb = await GetAllAsync(where: e => primaryKeys.Contains(e.Id));
        if (entitiesInDb.Count == 0)
            return;

        foreach (var dbModel in entitiesInDb)
        {
            var updatedEntity = objects.Find(o => o.Id == dbModel.Id);
            dbModel.UpdateDbModel(updatedEntity);
        }

        _table.UpdateRange(entitiesInDb);
    }

    public async Task<int> GetCountAsync(Expression<Func<T, bool>>? where = null, bool distinct = false,
        params string[] includeProperties)
    {
        var query = _table.AsQueryable();

        if (where != null)
            query = query.Where(where);

        if (distinct)
            query = query.Distinct();

        return await query.CountAsync();
    }

    public async Task<TResult> GetMaxAsync<TResult>(Expression<Func<T, TResult>> selector,
        Expression<Func<T, bool>>? where = null,
        bool distinct = false,
        params string[] includeProperties)
    {
        var query = _table.AsQueryable();

        if (includeProperties.Length != 0)
        {
            foreach (var includeProperty in includeProperties)
                query = query.Include(includeProperty);
        }

        if (where != null)
            query = query.Where(where);

        if (distinct)
            query = query.Distinct();

        return await query.MaxAsync(selector);
    }
}