using System.Linq.Expressions;
using BuildingBlocks.Domain.Internal;
using BuildingBlocks.Persistence.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.EntityFrameworkCore.Repositories;

/// <summary>基于 EF Core ChangeTracker 的通用聚合根仓储。</summary>
/// <typeparam name="TDbContext">仓储使用的数据库上下文类型。</typeparam>
/// <typeparam name="TEntity">要读写的聚合根类型。</typeparam>
/// <param name="dbContext">当前依赖注入作用域中的数据库上下文。</param>
/// <remarks>
///     单实体添加、更新和删除只改变跟踪状态，调用方必须随后调用 <see cref="SaveChangesAsync"/> 或工作单元保存。
///     按谓词删除使用 EF Core 的 <c>ExecuteDeleteAsync</c> 立即物理删除，并绕过保存拦截器与软删除语义。
/// </remarks>
public class Repository<TDbContext, TEntity>(TDbContext dbContext)
    : QueryRepository<TDbContext, TEntity>(dbContext), IRepository<TEntity>
    where TEntity : class, IAggregateRoot
    where TDbContext : DbContext
{
    /// <inheritdoc />
    public async Task<TEntity> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        return (await Entity.AddAsync(entity, cancellationToken)).Entity;
    }

    /// <inheritdoc />
    public Task InsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        return Entity.AddRangeAsync(entities, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Entity.Update(entity);
        return Task.FromResult(entity);
    }

    /// <inheritdoc />
    public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Entity.Remove(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return Entity.Where(predicate).ExecuteDeleteAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return DbContext.SaveChangesAsync(cancellationToken);
    }
}
