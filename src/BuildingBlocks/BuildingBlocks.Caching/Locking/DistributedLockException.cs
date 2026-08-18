namespace BuildingBlocks.Caching.Locking;

/// <summary>
///     表示分布式锁获取或管理失败，并可携带相关业务锁键。
/// </summary>
public class DistributedLockException : Exception
{
    /// <summary>
    ///     使用错误消息初始化不关联锁键的分布式锁异常。
    /// </summary>
    /// <param name="message">描述失败原因的消息。</param>
    public DistributedLockException(string message) : base(message)
    {
    }

    /// <summary>
    ///     使用错误消息和业务锁键初始化分布式锁异常，供派生异常使用。
    /// </summary>
    /// <param name="message">描述失败原因的消息。</param>
    /// <param name="lockKey">未添加内部 <c>lock:</c> 前缀的业务锁键。</param>
    protected DistributedLockException(string message, string lockKey) : base(message)
    {
        LockKey = lockKey;
    }

    /// <summary>
    ///     使用错误消息和导致失败的内部异常初始化不关联锁键的分布式锁异常。
    /// </summary>
    /// <param name="message">描述失败原因的消息。</param>
    /// <param name="innerException">导致锁操作失败的异常。</param>
    public DistributedLockException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    ///     使用错误消息、业务锁键和导致失败的内部异常初始化分布式锁异常。
    /// </summary>
    /// <param name="message">描述失败原因的消息。</param>
    /// <param name="lockKey">相关业务锁键。</param>
    /// <param name="innerException">导致锁操作失败的异常。</param>
    public DistributedLockException(string message, string lockKey, Exception innerException) : base(message,
        innerException)
    {
        LockKey = lockKey;
    }

    /// <summary>
    ///     获取相关业务锁键；异常未关联具体键时为 <see langword="null"/>。
    /// </summary>
    public string? LockKey { get; }
}

/// <summary>
///     表示在指定等待时间内未能取得分布式锁。
/// </summary>
/// <remarks>
///     当前 <see cref="IDistributedLockProvider.AcquireLockAsync"/> 在尝试 API 返回空结果时抛出此异常；
///     空结果既可能表示锁竞争超时，也可能表示尝试过程中发生了已记录的 Redis 错误。
/// </remarks>
public class LockAcquisitionTimeoutException : DistributedLockException
{
    /// <summary>
    ///     使用业务锁键和实际配置的最长等待时间初始化异常。
    /// </summary>
    /// <param name="lockKey">未添加内部前缀的业务锁键。</param>
    /// <param name="timeout">锁获取的最长等待时间。</param>
    public LockAcquisitionTimeoutException(string lockKey, TimeSpan timeout)
        : base($"Failed to acquire lock '{lockKey}' within {timeout.TotalMilliseconds}ms", lockKey)
    {
        Timeout = timeout;
    }

    /// <summary>
    ///     获取本次锁获取操作配置的最长等待时间。
    /// </summary>
    public TimeSpan Timeout { get; }
}

/// <summary>
///     表示指定分布式锁已由其他所有者持有。
/// </summary>
/// <remarks>当前默认 Redis 锁提供者不会主动抛出此异常；该类型供调用方或替代实现表达明确的占用冲突。</remarks>
public class LockAlreadyHeldException : DistributedLockException
{
    /// <summary>
    ///     初始化锁已被持有异常
    /// </summary>
    /// <param name="lockKey">锁键</param>
    public LockAlreadyHeldException(string lockKey)
        : base($"Lock '{lockKey}' is already held by another process", lockKey)
    {
    }
}