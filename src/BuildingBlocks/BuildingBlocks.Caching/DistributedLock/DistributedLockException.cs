using System;

namespace BuildingBlocks.Caching.DistributedLock;

/// <summary>
/// 分布式锁异常
/// </summary>
public class DistributedLockException : Exception
{
    /// <summary>
    /// 锁键
    /// </summary>
    public string? LockKey { get; }

    /// <summary>
    /// 初始化分布式锁异常
    /// </summary>
    /// <param name="message">异常消息</param>
    public DistributedLockException(string message) : base(message)
    {
    }

    /// <summary>
    /// 初始化分布式锁异常
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="lockKey">锁键</param>
    public DistributedLockException(string message, string lockKey) : base(message)
    {
        LockKey = lockKey;
    }

    /// <summary>
    /// 初始化分布式锁异常
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public DistributedLockException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// 初始化分布式锁异常
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="lockKey">锁键</param>
    /// <param name="innerException">内部异常</param>
    public DistributedLockException(string message, string lockKey, Exception innerException) : base(message, innerException)
    {
        LockKey = lockKey;
    }
}

/// <summary>
/// 锁获取超时异常
/// </summary>
public class LockAcquisitionTimeoutException : DistributedLockException
{
    /// <summary>
    /// 超时时间
    /// </summary>
    public TimeSpan Timeout { get; }

    /// <summary>
    /// 初始化锁获取超时异常
    /// </summary>
    /// <param name="lockKey">锁键</param>
    /// <param name="timeout">超时时间</param>
    public LockAcquisitionTimeoutException(string lockKey, TimeSpan timeout)
        : base($"Failed to acquire lock '{lockKey}' within {timeout.TotalMilliseconds}ms", lockKey)
    {
        Timeout = timeout;
    }
}

/// <summary>
/// 锁已被持有异常
/// </summary>
public class LockAlreadyHeldException : DistributedLockException
{
    /// <summary>
    /// 初始化锁已被持有异常
    /// </summary>
    /// <param name="lockKey">锁键</param>
    public LockAlreadyHeldException(string lockKey)
        : base($"Lock '{lockKey}' is already held by another process", lockKey)
    {
    }
}