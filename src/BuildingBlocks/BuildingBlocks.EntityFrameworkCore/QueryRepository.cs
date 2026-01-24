using System.Linq.Expressions;
using BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.EntityFrameworkCore;

/// <summary>
/// 只读仓储实现类
/// </summary>
/// <typeparam name="TDbContext"></typeparam>
/// <typeparam name="TEntity"></typeparam>
public class QueryRepository<TDbContext, TEntity>(TDbContext dbContext) : IQueryRepository<TEntity>
    where TEntity : class where TDbContext : DbContext
{
    protected TDbContext DbContext { get; } = dbContext;

    protected DbSet<TEntity> Entity => DbContext.Set<TEntity>();

    public IQueryable<TTemp> Select<TTemp>() where TTemp : class
    {
        return DbContext.Set<TTemp>();
    }

    public Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return Entity.Where(predicate).ToListAsync();
    }

    public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await Entity.FirstOrDefaultAsync(predicate).ConfigureAwait(false);
    }

    public Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return Entity.FirstAsync(predicate);
    }

    public Task<TEntity> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return Entity.SingleOrDefaultAsync(predicate)!;
    }

    public Task<TEntity> SingleAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return Entity.SingleAsync(predicate);
    }

    public Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return Entity.CountAsync(predicate);
    }

    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return Entity.AnyAsync(predicate);
    }

    public Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return Entity.FirstOrDefaultAsync(predicate);
    }

    public Task<List<TEntity>> GetListAsync()
    {
        return Entity.ToListAsync();
    }

    public IQueryable<TEntity> Query() { return DbContext.Set<TEntity>(); }

    public IQueryable<TEntity> QueryNoTracking()
    {
        return DbContext.Set<TEntity>().AsNoTracking();
    }

    public IQueryable<T> QueryNoTracking<T>() where T : class
    {
        return DbContext.Set<T>().AsNoTracking();
    }

    public async Task<(int total, IEnumerable<TEntity> result)> GetPageListAsync(
        Expression<Func<TEntity, bool>> predicate, int pageIndex,
        int pageSize, Expression<Func<TEntity, object>>? orderBy = null, bool isAsc = true)
    {
        var query = Entity.Where(predicate);

        var total = await query.CountAsync();

        if (orderBy != null)
        {
            query = isAsc ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);
        }

        var list = await query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToArrayAsync();

        return (total, list);
    }
}
