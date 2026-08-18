using System.Linq.Expressions;
using BuildingBlocks.Domain.Internal;

namespace BuildingBlocks.Persistence.Abstractions.Repositories;

/// <summary>定义聚合根的读写仓储契约。</summary>
/// <typeparam name="TEntity">实现 <see cref="IAggregateRoot"/> 的聚合根类型。</typeparam>
/// <remarks>
///     EF Core 实现先更改跟踪状态，再由 <see cref="SaveChangesAsync"/> 提交；MongoDB 实现通常立即执行写命令。
///     因而调用方不能把该接口的返回时机统一理解为数据库事务已经提交。
/// </remarks>
public interface IRepository<TEntity> : IQueryRepository<TEntity>
    where TEntity : class, IAggregateRoot
{
    /// <summary>添加一个聚合根。</summary>
    /// <param name="entity">要添加的聚合根。</param>
    /// <param name="cancellationToken">用于取消底层操作的令牌。</param>
    /// <returns>已加入写入流程的聚合根。</returns>
    Task<TEntity> InsertAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>批量添加聚合根。</summary>
    /// <param name="entities">要添加的聚合根序列。</param>
    /// <param name="cancellationToken">用于取消底层操作的令牌。</param>
    /// <returns>表示批量添加操作的任务。</returns>
    /// <remarks>实现可以在写入前一次性枚举序列；调用方不应传入可重复产生副作用的迭代器。</remarks>
    Task InsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>更新一个聚合根。</summary>
    /// <param name="entity">包含新状态的聚合根。</param>
    /// <param name="cancellationToken">用于取消底层操作的令牌。</param>
    /// <returns>已加入写入流程或已被替换的聚合根。</returns>
    /// <exception cref="InvalidOperationException">提供程序要求实体必须存在但未找到匹配项时实现可抛出。</exception>
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>删除指定聚合根。</summary>
    /// <param name="entity">要删除的聚合根。</param>
    /// <param name="cancellationToken">用于取消底层操作的令牌。</param>
    /// <returns>表示删除操作的任务。</returns>
    /// <remarks>EF Core 可通过保存拦截器把支持软删除的实体转为软删除；MongoDB 内置实现执行物理删除。</remarks>
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>直接物理删除满足条件的聚合根。</summary>
    /// <param name="predicate">实体筛选表达式。</param>
    /// <param name="cancellationToken">用于取消底层删除命令的令牌。</param>
    /// <returns>表示删除命令的任务。</returns>
    /// <remarks>该重载绕过 EF Core 的 ChangeTracker、保存拦截器和软删除逻辑；MongoDB 也直接执行批量删除。</remarks>
    Task DeleteAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>提交延迟写提供程序中的待保存变更。</summary>
    /// <param name="cancellationToken">用于取消保存操作的令牌。</param>
    /// <returns>EF Core 返回写入数据库的状态条目数；即时写 MongoDB 提供程序固定返回 0。</returns>
    /// <remarks>返回 0 不代表 MongoDB 写操作失败，也不能用作受影响文档数。</remarks>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
