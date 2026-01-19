namespace BuildingBlocks.Caching.DistributedLock;

/// <summary>
/// 分布式锁提供者接口
/// </summary>
public interface IDistributedLockProvider
{
    /// <summary>
    /// 尝试获取分布式锁
    /// </summary>
    /// <param name="lockKey">锁键</param>
    /// <param name="lockTimeout">锁超时时间</param>
    /// <param name="acquireTimeout">获取锁的超时时间</param>
    /// <param name="redisInstanceName">Redis 实例名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>获取成功返回锁对象，失败返回 null</returns>
    Task<IDistributedLock?> TryAcquireLockAsync(
        string lockKey,
        TimeSpan lockTimeout,
        TimeSpan acquireTimeout,
        string? redisInstanceName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取分布式锁（如果获取失败会抛出异常）
    /// </summary>
    /// <param name="lockKey">锁键</param>
    /// <param name="lockTimeout">锁超时时间</param>
    /// <param name="acquireTimeout">获取锁的超时时间</param>
    /// <param name="redisInstanceName">Redis 实例名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>锁对象</returns>
    /// <exception cref="TimeoutException">获取锁超时</exception>
    /// <exception cref="InvalidOperationException">获取锁失败</exception>
    Task<IDistributedLock> AcquireLockAsync(
        string lockKey,
        TimeSpan lockTimeout,
        TimeSpan acquireTimeout,
        string? redisInstanceName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查锁是否存在
    /// </summary>
    /// <param name="lockKey">锁键</param>
    /// <param name="redisInstanceName">Redis 实例名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>锁是否存在</returns>
    Task<bool> IsLockHeldAsync(
        string lockKey,
        string? redisInstanceName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 强制释放锁（危险操作，仅在确认锁已失效时使用）
    /// </summary>
    /// <param name="lockKey">锁键</param>
    /// <param name="redisInstanceName">Redis 实例名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>释放是否成功</returns>
    Task<bool> ForceReleaseLockAsync(
        string lockKey,
        string? redisInstanceName = null,
        CancellationToken cancellationToken = default);
}