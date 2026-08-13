using System.Linq.Expressions;
using BuildingBlocks.Domain;
using BuildingBlocks.Domain.Internal;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.EntityFrameworkCore;

/// <summary>
///     仓储基类
/// </summary>
public class Repository<TDbContext, TEntity>(TDbContext dbContext)
    : QueryRepository<TDbContext, TEntity>(dbContext), IRepository<TDbContext, TEntity>
    where TEntity : class, IAggregateRoot
    where TDbContext : DbContext
{
    public async Task<TEntity> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        return (await Entity.AddAsync(entity, cancellationToken)).Entity;
    }

    public Task InsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        return Entity.AddRangeAsync(entities, cancellationToken);
    }

    public Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Entity.Update(entity);
        return Task.FromResult(entity);
    }

    public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Entity.Remove(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return Entity.Where(predicate).ExecuteDeleteAsync(cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return DbContext.SaveChangesAsync(cancellationToken);
    }
}
