namespace BuildingBlocks.Caching.Locking;

/// <summary>
///     表示一个由唯一所有权令牌保护、具有有限租期的 Redis 分布式锁句柄。
/// </summary>
/// <remarks>
///     锁基于单个 Redis 实例，不是 Redlock。释放和续期均通过 Lua 原子比较所有权令牌后执行，
///     因而过期后被其他调用方重新取得的锁不会被旧句柄误删或误续期。组件不提供自动续期。
/// </remarks>
public interface IDistributedLock : IAsyncDisposable
{
    /// <summary>
    ///     获取实际存储在 Redis 中的锁键；当前实现包含 <c>lock:</c> 前缀。
    /// </summary>
    string LockKey { get; }

    /// <summary>
    ///     获取本地句柄是否尚未释放、销毁且本地记录的到期时间仍在未来。
    /// </summary>
    /// <remarks>该值不发起 Redis 查询，不能证明服务端锁仍由当前句柄持有。</remarks>
    bool IsValid { get; }

    /// <summary>
    ///     获取当前锁的唯一所有权令牌；释放和续期时必须与 Redis 中的值匹配。
    /// </summary>
    string LockValue { get; }

    /// <summary>
    ///     获取当前句柄记录的 UTC 到期时刻；成功续期后会更新。
    /// </summary>
    DateTime ExpiresAt { get; }

    /// <summary>
    ///     仅在所有权令牌仍匹配时原子删除 Redis 锁键。
    /// </summary>
    /// <param name="cancellationToken">用于取消 Redis 操作；取消会向上传播。</param>
    /// <returns>成功删除锁时为 <see langword="true"/>；已释放、已过期、所有权改变或 Redis 错误时为 <see langword="false"/>。</returns>
    Task<bool> ReleaseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     仅在所有权令牌仍匹配时，将 Redis 锁的剩余租期重置为指定时长。
    /// </summary>
    /// <param name="extendBy">从续期成功时刻起计算的新剩余租期，必须大于零。</param>
    /// <param name="cancellationToken">用于取消 Redis 操作；取消会向上传播。</param>
    /// <returns>成功重置租期时为 <see langword="true"/>；锁无效、所有权改变或 Redis 错误时为 <see langword="false"/>。</returns>
    /// <exception cref="ArgumentException"><paramref name="extendBy"/> 小于或等于零时抛出。</exception>
    Task<bool> ExtendAsync(TimeSpan extendBy, CancellationToken cancellationToken = default);
}