using System.Linq.Expressions;

namespace BuildingBlocks.Domain;

/// <summary>
///     只读仓储接口
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface IQueryRepository<TEntity>
{
    IQueryable<TTemp> Select<TTemp>() where TTemp : class;

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

    IQueryable<TEntity> Query();

    IQueryable<TEntity> QueryNoTracking();

    IQueryable<T> QueryNoTracking<T>() where T : class;

    Task<(int total, IEnumerable<TEntity> result)> GetPageListAsync(
        Expression<Func<TEntity, bool>> predicate,
        int pageIndex,
        int pageSize,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool isAsc = true,
        CancellationToken cancellationToken = default);
}
