using MongoDB.Driver;

namespace BuildingBlocks.MongoDB.Abstractions;

/// <summary>定义 MongoDB 数据库、客户端和当前事务 session 的作用域上下文。</summary>
/// <remarks>同一上下文同时只能维护一个活动事务；事务内仓储操作必须使用 <see cref="Session"/>。</remarks>
public interface IMongoDbContext : IDisposable
{
    /// <summary>获取当前上下文使用的 MongoDB 数据库。</summary>
    IMongoDatabase Database { get; }

    /// <summary>获取用于创建 session 的共享 MongoDB 客户端。</summary>
    IMongoClient Client { get; }

    /// <summary>获取当前 session；未开始事务时为 <see langword="null"/>。</summary>
    IClientSessionHandle? Session { get; }

    /// <summary>创建 session 并开始事务。</summary>
    /// <param name="cancellationToken">用于取消 session 创建的令牌。</param>
    /// <returns>已开始事务的 session。</returns>
    /// <exception cref="InvalidOperationException">当前上下文已有活动事务时抛出。</exception>
    Task<IClientSessionHandle> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>提交当前事务；没有活动事务时不执行操作。</summary>
    /// <param name="cancellationToken">用于取消提交命令的令牌。</param>
    /// <returns>表示提交操作的任务。</returns>
    /// <remarks>仅在提交成功后释放 session；失败时保留 session，供调用方决定重试提交或回滚。</remarks>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>回滚当前事务；没有活动事务时不执行操作。</summary>
    /// <param name="cancellationToken">用于取消回滚命令的令牌。</param>
    /// <returns>表示回滚操作的任务。</returns>
    /// <remarks>仅在回滚成功后释放 session；失败时保留 session，避免丢失事务状态。</remarks>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
