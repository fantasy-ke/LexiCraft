using System.Linq.Expressions;
using BuildingBlocks.Domain.Internal;

namespace BuildingBlocks.Persistence.Abstractions.Repositories;

/// <summary>
///     Provider-neutral aggregate repository contract.
/// </summary>
public interface IRepository<TEntity> : IQueryRepository<TEntity>
    where TEntity : class, IAggregateRoot
{
    Task<TEntity> InsertAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task InsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Directly deletes matching entities. Provider-specific tracking and interceptor behavior is bypassed.
    /// </summary>
    Task DeleteAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Persists pending changes when the provider is deferred-write; immediate-write providers return zero.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
