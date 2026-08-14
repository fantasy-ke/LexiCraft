using System.Linq.Expressions;
using BuildingBlocks.Persistence.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.EntityFrameworkCore.Repositories;

/// <summary>
///     Entity Framework Core query repository.
/// </summary>
public class QueryRepository<TDbContext, TEntity>(TDbContext dbContext) : IQueryRepository<TEntity>
    where TEntity : class
    where TDbContext : DbContext
{
    public TDbContext DbContext { get; } = dbContext;

    protected DbSet<TEntity> Entity => DbContext.Set<TEntity>();

    /// <summary>
    ///     EF-specific cross-set query helper. It is intentionally not part of the provider-neutral contract.
    /// </summary>
    protected IQueryable<T> QuerySetNoTracking<T>() where T : class
    {
        return DbContext.Set<T>().AsNoTracking();
    }

    public Task<List<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return Entity.Where(predicate).ToListAsync(cancellationToken);
    }

    public Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return Entity.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public Task<TEntity> FirstAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return Entity.FirstAsync(predicate, cancellationToken);
    }

    public Task<TEntity?> SingleOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return Entity.SingleOrDefaultAsync(predicate, cancellationToken);
    }

    public Task<TEntity> SingleAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return Entity.SingleAsync(predicate, cancellationToken);
    }

    public Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return Entity.CountAsync(predicate, cancellationToken);
    }

    public Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return Entity.AnyAsync(predicate, cancellationToken);
    }

    public Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return Entity.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public Task<List<TEntity>> GetListAsync(CancellationToken cancellationToken = default)
    {
        return Entity.ToListAsync(cancellationToken);
    }

    public IQueryable<TEntity> Query()
    {
        return Entity;
    }

    public IQueryable<TEntity> QueryNoTracking()
    {
        return Entity.AsNoTracking();
    }

    public async Task<(int total, IEnumerable<TEntity> result)> GetPageListAsync(
        Expression<Func<TEntity, bool>> predicate,
        int pageIndex,
        int pageSize,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool isAsc = true,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(pageIndex, pageSize);

        var query = Entity.Where(predicate);
        var total = await query.CountAsync(cancellationToken);

        if (orderBy is not null)
            query = isAsc ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);

        var skip = checked((pageIndex - 1) * pageSize);
        var list = await query.Skip(skip).Take(pageSize).ToArrayAsync(cancellationToken);

        return (total, list);
    }

    private static void ValidatePagination(int pageIndex, int pageSize)
    {
        if (pageIndex < 1)
            throw new ArgumentOutOfRangeException(nameof(pageIndex), pageIndex, "Page index must be greater than zero.");
        if (pageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Page size must be greater than zero.");
    }
}
