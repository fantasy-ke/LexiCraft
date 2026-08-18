using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BuildingBlocks.Caching.Locking;

/// <summary>
///     基于 Redis 的分布式锁实现
/// </summary>
/// <remarks>
///     锁状态就是单个 Redis 实例上的一个带 TTL 的键，键值是唯一 owner token（见
///     <see cref="LockValue"/>）。释放与续期都通过 Lua 脚本先 <c>GET</c> 比较 owner token 再执行
///     <c>DEL</c> 或 <c>PEXPIRE</c>，两步在 Redis 中原子完成，因此锁过期后被其他调用方重新取得时，
///     旧句柄不会误删或误续期。这不是 Redlock，不提供多节点法定人数保证，也不会自动续期：
///     一旦临界区耗时超过租期，锁会静默失效，之后的 <see cref="ReleaseAsync"/> 返回
///     <see langword="false"/> 而不抛异常。除取消以外的 Redis 异常都被记录并转换为
///     <see langword="false"/>，属于降级而非抛出。
/// </remarks>
internal sealed class RedisDistributedLock : IDistributedLock
{
    /// <summary>
    ///     Lua 脚本：释放锁（仅当锁值匹配时）
    /// </summary>
    private const string ReleaseLockScript = @"
        if redis.call('GET', KEYS[1]) == ARGV[1] then
            return redis.call('DEL', KEYS[1])
        else
            return 0
        end";

    /// <summary>
    ///     Lua 脚本：延长锁的过期时间（仅当锁值匹配时）
    /// </summary>
    private const string ExtendLockScript = @"
        if redis.call('GET', KEYS[1]) == ARGV[1] then
            return redis.call('PEXPIRE', KEYS[1], ARGV[2])
        else
            return 0
        end";

    private readonly IDatabase _database;
    private readonly ILogger<RedisDistributedLock> _logger;
    private bool _disposed;
    private bool _released;

    /// <summary>
    ///     初始化 Redis 分布式锁
    /// </summary>
    /// <param name="database">已取得锁的 Redis 数据库句柄；必须与获取锁时使用的实例一致。</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="lockKey">含 <c>lock:</c> 前缀的完整 Redis 锁键</param>
    /// <param name="lockValue">唯一 owner token，用于释放和续期时的原子校验</param>
    /// <param name="expiresAt">本地记录的 UTC 到期时刻</param>
    /// <exception cref="ArgumentNullException">任一引用参数为 <see langword="null"/> 时抛出。</exception>
    /// <remarks>构造函数不执行任何 Redis 操作，调用方必须已经成功取得锁。</remarks>
    public RedisDistributedLock(
        IDatabase database,
        ILogger<RedisDistributedLock> logger,
        string lockKey,
        string lockValue,
        DateTime expiresAt)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        LockKey = lockKey ?? throw new ArgumentNullException(nameof(lockKey));
        LockValue = lockValue ?? throw new ArgumentNullException(nameof(lockValue));
        ExpiresAt = expiresAt;

        _logger.LogDebug("分布式锁已创建: {LockKey}, 值: {LockValue}, 过期时间: {ExpiresAt}",
            LockKey, LockValue, ExpiresAt);
    }

    public string LockKey { get; }

    public bool IsValid => !_disposed && !_released && DateTime.UtcNow < ExpiresAt;
    public string LockValue { get; }

    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    ///     释放锁
    /// </summary>
    /// <returns>释放是否成功</returns>
    public async Task<bool> ReleaseAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed || _released)
        {
            _logger.LogWarning("尝试释放已释放或已销毁的锁: {LockKey}", LockKey);
            return false;
        }

        try
        {
            var result = await _database.ScriptEvaluateAsync(
                ReleaseLockScript,
                [LockKey],
                [LockValue]).WaitAsync(cancellationToken);

            var success = result.ToString() == "1";
            if (success)
            {
                _released = true;
                _logger.LogDebug("分布式锁已释放: {LockKey}", LockKey);
            }
            else
            {
                _logger.LogWarning("释放分布式锁失败，锁可能已过期或被其他进程持有: {LockKey}", LockKey);
            }

            return success;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "释放分布式锁时发生异常: {LockKey}", LockKey);
            return false;
        }
    }

    /// <summary>
    ///     延长锁的过期时间
    /// </summary>
    /// <param name="extendBy">延长的时间</param>
    /// <param name="cancellationToken"></param>
    /// <returns>延长是否成功</returns>
    public async Task<bool> ExtendAsync(TimeSpan extendBy, CancellationToken cancellationToken = default)
    {
        if (_disposed || _released)
        {
            _logger.LogWarning("尝试延长已释放或已销毁的锁: {LockKey}", LockKey);
            return false;
        }

        if (extendBy <= TimeSpan.Zero) throw new ArgumentException("延长时间必须大于零", nameof(extendBy));

        try
        {
            var extendMilliseconds = (long)extendBy.TotalMilliseconds;
            var result = await _database.ScriptEvaluateAsync(
                ExtendLockScript,
                [LockKey],
                [LockValue, extendMilliseconds]).WaitAsync(cancellationToken);

            var success = result.ToString() == "1";
            if (success)
            {
                ExpiresAt = DateTime.UtcNow.Add(extendBy);
                _logger.LogDebug("分布式锁过期时间已延长: {LockKey}, 延长时间: {ExtendBy}", LockKey, extendBy);
            }
            else
            {
                _logger.LogWarning("延长分布式锁过期时间失败，锁可能已过期或被其他进程持有: {LockKey}", LockKey);
            }

            return success;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "延长分布式锁过期时间时发生异常: {LockKey}", LockKey);
            return false;
        }
    }

    /// <summary>
    ///     异步释放资源
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            if (!_released) await ReleaseAsync();
            _disposed = true;
        }
    }
}