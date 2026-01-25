using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BuildingBlocks.Caching.DistributedLock;

/// <summary>
///     基于 Redis 的分布式锁实现
/// </summary>
public class RedisDistributedLock : IDistributedLock
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
    /// <param name="database">Redis 数据库</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="lockKey">锁键</param>
    /// <param name="lockValue">锁值</param>
    /// <param name="expiresAt">过期时间</param>
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

    public DateTime ExpiresAt { get; }

    /// <summary>
    ///     释放锁
    /// </summary>
    /// <returns>释放是否成功</returns>
    public async Task<bool> ReleaseAsync()
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
                new RedisKey[] { LockKey },
                new RedisValue[] { LockValue });

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
    /// <returns>延长是否成功</returns>
    public async Task<bool> ExtendAsync(TimeSpan extendBy)
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
                new RedisKey[] { LockKey },
                new RedisValue[] { LockValue, extendMilliseconds });

            var success = result.ToString() == "1";
            if (success)
                _logger.LogDebug("分布式锁过期时间已延长: {LockKey}, 延长时间: {ExtendBy}", LockKey, extendBy);
            else
                _logger.LogWarning("延长分布式锁过期时间失败，锁可能已过期或被其他进程持有: {LockKey}", LockKey);

            return success;
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