using System.Linq.Expressions;

namespace BuildingBlocks.Persistence.Abstractions.Repositories;

/// <summary>
///     Provider-neutral read repository contract.
/// </summary>
public interface IQueryRepository<TEntity>
    where TEntity : class
{
    Task<List<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<TEntity> FirstAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<TEntity?> SingleOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<TEntity> SingleAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns the provider queryable for advanced queries. Execution semantics remain provider-specific.
    /// </summary>
    IQueryable<TEntity> Query();

    /// <summary>
    ///     Returns a read-only queryable when the provider supports tracking; equivalent to Query for MongoDB.
    /// </summary>
    IQueryable<TEntity> QueryNoTracking();

    Task<(int total, IEnumerable<TEntity> result)> GetPageListAsync(
        Expression<Func<TEntity, bool>> predicate,
        int pageIndex,
        int pageSize,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool isAsc = true,
        CancellationToken cancellationToken = default);
}
