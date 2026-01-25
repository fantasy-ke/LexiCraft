namespace BuildingBlocks.Caching.DistributedLock;

/// <summary>
///     分布式锁接口
/// </summary>
public interface IDistributedLock : IAsyncDisposable
{
    /// <summary>
    ///     锁键
    /// </summary>
    string LockKey { get; }

    /// <summary>
    ///     锁是否有效
    /// </summary>
    bool IsValid { get; }

    /// <summary>
    ///     锁值（用于验证锁的所有权）
    /// </summary>
    string LockValue { get; }

    /// <summary>
    ///     锁的过期时间
    /// </summary>
    DateTime ExpiresAt { get; }

    /// <summary>
    ///     释放锁
    /// </summary>
    /// <returns>释放是否成功</returns>
    Task<bool> ReleaseAsync();

    /// <summary>
    ///     延长锁的过期时间
    /// </summary>
    /// <param name="extendBy">延长的时间</param>
    /// <returns>延长是否成功</returns>
    Task<bool> ExtendAsync(TimeSpan extendBy);
}