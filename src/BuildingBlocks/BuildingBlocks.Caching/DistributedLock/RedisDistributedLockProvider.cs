using BuildingBlocks.Caching.Factories;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BuildingBlocks.Caching.DistributedLock;

/// <summary>
/// 基于 Redis 的分布式锁提供者实现
/// </summary>
public class RedisDistributedLockProvider : IDistributedLockProvider
{
    private readonly IRedisConnectionFactory _connectionFactory;
    private readonly ILogger<RedisDistributedLockProvider> _logger;
    private readonly ILogger<RedisDistributedLock> _lockLogger;

    /// <summary>
    /// Lua 脚本：设置锁（仅当键不存在时）
    /// </summary>
    private const string AcquireLockScript = @"
        if redis.call('EXISTS', KEYS[1]) == 0 then
            return redis.call('PSETEX', KEYS[1], ARGV[2], ARGV[1])
        else
            return nil
        end";

    /// <summary>
    /// 锁键前缀
    /// </summary>
    private const string LockKeyPrefix = "lock:";

    /// <summary>
    /// 初始化 Redis 分布式锁提供者
    /// </summary>
    /// <param name="connectionFactory">Redis 连接工厂</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="lockLogger">锁日志记录器</param>
    public RedisDistributedLockProvider(
        IRedisConnectionFactory connectionFactory,
        ILogger<RedisDistributedLockProvider> logger,
        ILogger<RedisDistributedLock> lockLogger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _lockLogger = lockLogger ?? throw new ArgumentNullException(nameof(lockLogger));
    }

    /// <summary>
    /// 尝试获取分布式锁
    /// </summary>
    /// <param name="lockKey">锁键</param>
    /// <param name="lockTimeout">锁超时时间</param>
    /// <param name="acquireTimeout">获取锁的超时时间</param>
    /// <param name="redisInstanceName">Redis 实例名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>获取成功返回锁对象，失败返回 null</returns>
    public async Task<IDistributedLock?> TryAcquireLockAsync(
        string lockKey,
        TimeSpan lockTimeout,
        TimeSpan acquireTimeout,
        string? redisInstanceName = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lockKey))
            throw new ArgumentException("锁键不能为空", nameof(lockKey));

        if (lockTimeout <= TimeSpan.Zero)
            throw new ArgumentException("锁超时时间必须大于零", nameof(lockTimeout));

        if (acquireTimeout < TimeSpan.Zero)
            throw new ArgumentException("获取锁超时时间不能为负数", nameof(acquireTimeout));

        var fullLockKey = LockKeyPrefix + lockKey;
        var lockValue = GenerateLockValue();
        var database = GetDatabase(redisInstanceName);

        _logger.LogDebug("尝试获取分布式锁: {LockKey}, 超时: {LockTimeout}, 获取超时: {AcquireTimeout}",
            fullLockKey, lockTimeout, acquireTimeout);

        var startTime = DateTime.UtcNow;
        var endTime = startTime.Add(acquireTimeout);

        while (DateTime.UtcNow < endTime)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var lockTimeoutMs = (long)lockTimeout.TotalMilliseconds;
                var result = await database.ScriptEvaluateAsync(
                    AcquireLockScript,
                    new RedisKey[] { fullLockKey },
                    new RedisValue[] { lockValue, lockTimeoutMs });

                if (!result.IsNull)
                {
                    var expiresAt = DateTime.UtcNow.Add(lockTimeout);
                    var distributedLock = new RedisDistributedLock(
                        database, _lockLogger, fullLockKey, lockValue, expiresAt);

                    _logger.LogDebug("成功获取分布式锁: {LockKey}, 值: {LockValue}, 过期时间: {ExpiresAt}",
                        fullLockKey, lockValue, expiresAt);

                    return distributedLock;
                }

                // 锁被占用，等待一小段时间后重试
                var delay = TimeSpan.FromMilliseconds(Math.Min(50, acquireTimeout.TotalMilliseconds / 10));
                await Task.Delay(delay, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("获取分布式锁被取消: {LockKey}", fullLockKey);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取分布式锁时发生异常: {LockKey}", fullLockKey);
                return null;
            }
        }

        _logger.LogWarning("获取分布式锁超时: {LockKey}, 超时时间: {AcquireTimeout}", fullLockKey, acquireTimeout);
        return null;
    }

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
    public async Task<IDistributedLock> AcquireLockAsync(
        string lockKey,
        TimeSpan lockTimeout,
        TimeSpan acquireTimeout,
        string? redisInstanceName = null,
        CancellationToken cancellationToken = default)
    {
        var distributedLock = await TryAcquireLockAsync(lockKey, lockTimeout, acquireTimeout, redisInstanceName, cancellationToken);

        if (distributedLock == null)
        {
            throw new LockAcquisitionTimeoutException(lockKey, acquireTimeout);
        }

        return distributedLock;
    }

    /// <summary>
    /// 检查锁是否存在
    /// </summary>
    /// <param name="lockKey">锁键</param>
    /// <param name="redisInstanceName">Redis 实例名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>锁是否存在</returns>
    public async Task<bool> IsLockHeldAsync(
        string lockKey,
        string? redisInstanceName = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lockKey))
            throw new ArgumentException("锁键不能为空", nameof(lockKey));

        var fullLockKey = LockKeyPrefix + lockKey;
        var database = GetDatabase(redisInstanceName);

        try
        {
            var exists = await database.KeyExistsAsync(fullLockKey);
            _logger.LogDebug("检查分布式锁是否存在: {LockKey}, 结果: {Exists}", fullLockKey, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查分布式锁是否存在时发生异常: {LockKey}", fullLockKey);
            return false;
        }
    }

    /// <summary>
    /// 强制释放锁（危险操作，仅在确认锁已失效时使用）
    /// </summary>
    /// <param name="lockKey">锁键</param>
    /// <param name="redisInstanceName">Redis 实例名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>释放是否成功</returns>
    public async Task<bool> ForceReleaseLockAsync(
        string lockKey,
        string? redisInstanceName = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lockKey))
            throw new ArgumentException("锁键不能为空", nameof(lockKey));

        var fullLockKey = LockKeyPrefix + lockKey;
        var database = GetDatabase(redisInstanceName);

        try
        {
            var deleted = await database.KeyDeleteAsync(fullLockKey);
            _logger.LogWarning("强制释放分布式锁: {LockKey}, 结果: {Deleted}", fullLockKey, deleted);
            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "强制释放分布式锁时发生异常: {LockKey}", fullLockKey);
            return false;
        }
    }

    /// <summary>
    /// 获取 Redis 数据库实例
    /// </summary>
    /// <param name="redisInstanceName">Redis 实例名称</param>
    /// <returns>Redis 数据库实例</returns>
    private IDatabase GetDatabase(string? redisInstanceName = null)
    {
        if (string.IsNullOrWhiteSpace(redisInstanceName))
        {
            return _connectionFactory.GetDatabase();
        }
        
        return _connectionFactory.GetDatabase(redisInstanceName);
    }

    /// <summary>
    /// 生成唯一的锁值
    /// </summary>
    /// <returns>锁值</returns>
    private static string GenerateLockValue()
    {
        // 使用 GUID + 时间戳确保唯一性
        return $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}:{DateTimeOffset.UtcNow.Ticks}";
    }
}