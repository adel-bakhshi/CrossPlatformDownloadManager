using System.Linq.Expressions;
using CrossPlatformDownloadManager.Data.DbContext;
using CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CrossPlatformDownloadManager.Data.Services.Repository.Services;

public class RepositoryBase<T> : IRepositoryBase<T> where T : class, new()
{
    #region Private Fields

    private readonly DownloadManagerDbContext _dbContext;

    #endregion

    #region Properties

    protected DbSet<T> Table { get; }

    #endregion

    public RepositoryBase(DownloadManagerDbContext dbContext)
    {
        _dbContext = dbContext;
        Table = _dbContext.Set<T>();
    }

    public async Task AddAsync(T? entity)
    {
        if (entity == null)
            return;

        await Table.AddAsync(entity);
    }

    public async Task AddRangeAsync(IEnumerable<T>? entities)
    {
        var objects = entities?.ToList() ?? [];
        if (!objects.Any())
            return;

        await Table.AddRangeAsync(objects);
    }

    public async Task<T?> GetAsync(Expression<Func<T, bool>>? where = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        params string[] includeProperties)
    {
        var query = Table.AsQueryable();

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

        var query = Table.AsQueryable();

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
        var query = Table.AsQueryable();

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

        var query = Table.AsQueryable();

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

    public void Delete(T? entity)
    {
        if (entity == null)
            return;

        Table.Remove(entity);
    }

    public void DeleteAll(IEnumerable<T>? entities)
    {
        var objects = entities?.ToList() ?? [];
        if (!objects.Any())
            return;

        Table.RemoveRange(objects);
    }

    public async Task<int> GetCountAsync(Expression<Func<T, bool>>? where = null, bool distinct = false,
        params string[] includeProperties)
    {
        var query = Table.AsQueryable();

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
        var query = Table.AsQueryable();
        
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