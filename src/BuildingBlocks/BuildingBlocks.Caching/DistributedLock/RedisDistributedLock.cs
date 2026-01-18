using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace BuildingBlocks.Caching.DistributedLock;

/// <summary>
/// 基于 Redis 的分布式锁实现
/// </summary>
public class RedisDistributedLock : IDistributedLock
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisDistributedLock> _logger;
    private readonly string _lockKey;
    private readonly string _lockValue;
    private readonly DateTime _expiresAt;
    private bool _disposed;
    private bool _released;

    /// <summary>
    /// Lua 脚本：释放锁（仅当锁值匹配时）
    /// </summary>
    private const string ReleaseLockScript = @"
        if redis.call('GET', KEYS[1]) == ARGV[1] then
            return redis.call('DEL', KEYS[1])
        else
            return 0
        end";

    /// <summary>
    /// Lua 脚本：延长锁的过期时间（仅当锁值匹配时）
    /// </summary>
    private const string ExtendLockScript = @"
        if redis.call('GET', KEYS[1]) == ARGV[1] then
            return redis.call('PEXPIRE', KEYS[1], ARGV[2])
        else
            return 0
        end";

    public string LockKey => _lockKey;
    public bool IsValid => !_disposed && !_released && DateTime.UtcNow < _expiresAt;
    public string LockValue => _lockValue;
    public DateTime ExpiresAt => _expiresAt;

    /// <summary>
    /// 初始化 Redis 分布式锁
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
        _lockKey = lockKey ?? throw new ArgumentNullException(nameof(lockKey));
        _lockValue = lockValue ?? throw new ArgumentNullException(nameof(lockValue));
        _expiresAt = expiresAt;

        _logger.LogDebug("分布式锁已创建: {LockKey}, 值: {LockValue}, 过期时间: {ExpiresAt}",
            _lockKey, _lockValue, _expiresAt);
    }

    /// <summary>
    /// 释放锁
    /// </summary>
    /// <returns>释放是否成功</returns>
    public async Task<bool> ReleaseAsync()
    {
        if (_disposed || _released)
        {
            _logger.LogWarning("尝试释放已释放或已销毁的锁: {LockKey}", _lockKey);
            return false;
        }

        try
        {
            var result = await _database.ScriptEvaluateAsync(
                ReleaseLockScript,
                new RedisKey[] { _lockKey },
                new RedisValue[] { _lockValue });

            var success = result.ToString() == "1";
            if (success)
            {
                _released = true;
                _logger.LogDebug("分布式锁已释放: {LockKey}", _lockKey);
            }
            else
            {
                _logger.LogWarning("释放分布式锁失败，锁可能已过期或被其他进程持有: {LockKey}", _lockKey);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "释放分布式锁时发生异常: {LockKey}", _lockKey);
            return false;
        }
    }

    /// <summary>
    /// 延长锁的过期时间
    /// </summary>
    /// <param name="extendBy">延长的时间</param>
    /// <returns>延长是否成功</returns>
    public async Task<bool> ExtendAsync(TimeSpan extendBy)
    {
        if (_disposed || _released)
        {
            _logger.LogWarning("尝试延长已释放或已销毁的锁: {LockKey}", _lockKey);
            return false;
        }

        if (extendBy <= TimeSpan.Zero)
        {
            throw new ArgumentException("延长时间必须大于零", nameof(extendBy));
        }

        try
        {
            var extendMilliseconds = (long)extendBy.TotalMilliseconds;
            var result = await _database.ScriptEvaluateAsync(
                ExtendLockScript,
                new RedisKey[] { _lockKey },
                new RedisValue[] { _lockValue, extendMilliseconds });

            var success = result.ToString() == "1";
            if (success)
            {
                _logger.LogDebug("分布式锁过期时间已延长: {LockKey}, 延长时间: {ExtendBy}", _lockKey, extendBy);
            }
            else
            {
                _logger.LogWarning("延长分布式锁过期时间失败，锁可能已过期或被其他进程持有: {LockKey}", _lockKey);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "延长分布式锁过期时间时发生异常: {LockKey}", _lockKey);
            return false;
        }
    }

    /// <summary>
    /// 异步释放资源
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            if (!_released)
            {
                await ReleaseAsync();
            }
            _disposed = true;
        }
    }
}