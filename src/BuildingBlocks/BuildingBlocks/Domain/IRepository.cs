using System.Linq.Expressions;
using BuildingBlocks.Domain.Internal;

namespace BuildingBlocks.Domain;

/// <summary>
///     通用仓储接口
/// </summary>
/// <typeparam name="TDbContext">数据库上下文类型</typeparam>
/// <typeparam name="TEntity">实体类型</typeparam>
public interface IRepository<TDbContext, TEntity> : IRepository<TEntity>
    where TEntity : class, IAggregateRoot
{
    TDbContext DbContext { get; }
}

/// <summary>
///     通用仓储接口
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
public interface IRepository<TEntity> : IQueryRepository<TEntity>
    where TEntity : class, IAggregateRoot
{
    Task<TEntity> InsertAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task InsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Directly deletes matching rows. This bypasses change tracking, SaveChanges and interceptors.
    /// </summary>
    Task DeleteAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
