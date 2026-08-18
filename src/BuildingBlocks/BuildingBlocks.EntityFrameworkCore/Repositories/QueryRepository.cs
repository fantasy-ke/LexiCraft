using System.Linq.Expressions;
using BuildingBlocks.Persistence.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.EntityFrameworkCore.Repositories;

/// <summary>基于 EF Core 的通用实体查询仓储。</summary>
/// <typeparam name="TDbContext">查询所使用的数据库上下文类型。</typeparam>
/// <typeparam name="TEntity">实体类型。</typeparam>
/// <param name="dbContext">当前依赖注入作用域中的数据库上下文。</param>
/// <remarks>普通查询保留 EF Core 默认跟踪行为；显式调用 <see cref="QueryNoTracking"/> 可禁用跟踪。</remarks>
public class QueryRepository<TDbContext, TEntity>(TDbContext dbContext) : IQueryRepository<TEntity>
    where TEntity : class
    where TDbContext : DbContext
{
    /// <summary>获取仓储使用的数据库上下文。</summary>
    public TDbContext DbContext { get; } = dbContext;

    /// <summary>获取当前实体的 EF Core 集合。</summary>
    protected DbSet<TEntity> Entity => DbContext.Set<TEntity>();

    /// <summary>返回同一上下文中另一实体类型的无跟踪查询。</summary>
    /// <typeparam name="T">要查询的实体类型。</typeparam>
    /// <returns>另一实体集合的无跟踪查询。</returns>
    /// <remarks>这是 EF 专属的派生仓储扩展点，不属于跨提供程序接口。</remarks>
    protected IQueryable<T> QuerySetNoTracking<T>() where T : class
    {
        return DbContext.Set<T>().AsNoTracking();
    }

    /// <inheritdoc />
    public Task<List<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return Entity.Where(predicate).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return Entity.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TEntity> FirstAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return Entity.FirstAsync(predicate, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TEntity?> SingleOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return Entity.SingleOrDefaultAsync(predicate, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TEntity> SingleAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return Entity.SingleAsync(predicate, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return Entity.CountAsync(predicate, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return Entity.AnyAsync(predicate, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return Entity.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<TEntity>> GetListAsync(CancellationToken cancellationToken = default)
    {
        return Entity.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public IQueryable<TEntity> Query()
    {
        return Entity;
    }

    /// <inheritdoc />
    public IQueryable<TEntity> QueryNoTracking()
    {
        return Entity.AsNoTracking();
    }

    /// <inheritdoc />
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
