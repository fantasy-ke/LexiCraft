namespace BuildingBlocks.Caching.Locking;

/// <summary>
///     在默认或命名 Redis 实例上获取、检查和强制删除有限租期的单实例分布式锁。
/// </summary>
/// <remarks>
///     锁适合保护短时缓存重建临界区，不提供多节点 Redlock 法定人数保证或自动续期。
///     普通释放必须通过 <see cref="IDistributedLock"/> 的所有权令牌校验。
/// </remarks>
public interface IDistributedLockProvider
{
    /// <summary>
    ///     在等待期限内尝试取得分布式锁。
    /// </summary>
    /// <param name="lockKey">业务锁键；实现会添加内部前缀。</param>
    /// <param name="lockTimeout">取得锁后的租期，必须大于零。</param>
    /// <param name="acquireTimeout">最长等待时间，不能为负；零仍执行一次立即尝试。</param>
    /// <param name="redisInstanceName">Redis 实例名称；空值或空白值表示默认实例。</param>
    /// <param name="cancellationToken">用于取消 Redis 操作及重试等待；取消会向上传播。</param>
    /// <returns>取得锁时返回锁句柄；锁被占用、等待超时或 Redis 操作失败时返回 <see langword="null"/>。</returns>
    /// <exception cref="ArgumentException">锁键为空、租期非正或等待时间为负时抛出。</exception>
    Task<IDistributedLock?> TryAcquireLockAsync(
        string lockKey,
        TimeSpan lockTimeout,
        TimeSpan acquireTimeout,
        string? redisInstanceName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     在等待期限内取得分布式锁，并将未取得锁的结果转换为超时异常。
    /// </summary>
    /// <param name="lockKey">业务锁键；实现会添加内部前缀。</param>
    /// <param name="lockTimeout">取得锁后的租期，必须大于零。</param>
    /// <param name="acquireTimeout">最长等待时间，不能为负；零仍执行一次立即尝试。</param>
    /// <param name="redisInstanceName">Redis 实例名称；空值或空白值表示默认实例。</param>
    /// <param name="cancellationToken">用于取消 Redis 操作及重试等待；取消会向上传播。</param>
    /// <returns>已取得且由调用方负责异步释放的锁句柄。</returns>
    /// <exception cref="ArgumentException">锁键为空、租期非正或等待时间为负时抛出。</exception>
    /// <exception cref="LockAcquisitionTimeoutException">
    ///     锁被占用直到等待超时，或底层尝试因 Redis 异常返回失败时抛出；异常包含原始业务锁键和等待时间。
    /// </exception>
    Task<IDistributedLock> AcquireLockAsync(
        string lockKey,
        TimeSpan lockTimeout,
        TimeSpan acquireTimeout,
        string? redisInstanceName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     检查 Redis 中是否存在对应锁键，不验证所有者。
    /// </summary>
    /// <param name="lockKey">业务锁键。</param>
    /// <param name="redisInstanceName">Redis 实例名称；空值或空白值表示默认实例。</param>
    /// <param name="cancellationToken">用于取消 Redis 查询；取消会向上传播。</param>
    /// <returns>锁键存在时为 <see langword="true"/>；不存在或 Redis 查询失败时为 <see langword="false"/>。</returns>
    /// <exception cref="ArgumentException">锁键为空时抛出。</exception>
    Task<bool> IsLockHeldAsync(
        string lockKey,
        string? redisInstanceName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     不检查所有权令牌而直接删除 Redis 锁键。
    /// </summary>
    /// <param name="lockKey">业务锁键。</param>
    /// <param name="redisInstanceName">Redis 实例名称；空值或空白值表示默认实例。</param>
    /// <param name="cancellationToken">用于取消 Redis 删除；取消会向上传播。</param>
    /// <returns>删除了现有键时为 <see langword="true"/>；键不存在或 Redis 操作失败时为 <see langword="false"/>。</returns>
    /// <remarks>此操作可能删除其他所有者的有效锁，只能在外部已确认锁可安全失效时使用。</remarks>
    /// <exception cref="ArgumentException">锁键为空时抛出。</exception>
    Task<bool> ForceReleaseLockAsync(
        string lockKey,
        string? redisInstanceName = null,
        CancellationToken cancellationToken = default);
}