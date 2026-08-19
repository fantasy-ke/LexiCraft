using BuildingBlocks.Caching.Locking;
using BuildingBlocks.Caching.Options;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Caching.Services;

internal sealed partial class CacheService
{
    /// <summary>
    ///     使用分布式锁的 GetOrSet 操作
    /// </summary>
    private async Task<T?> GetOrSetWithLockAsync<T>(string key, Func<Task<T>> factory, CacheServiceOptions options,
        CancellationToken cancellationToken)
    {
        var lockKey = key;
        IDistributedLock? distributedLock = null;

        try
        {
            // 尝试获取分布式锁
            distributedLock = await _lockProvider.TryAcquireLockAsync(
                lockKey,
                options.LockTimeout,
                options.LockAcquireTimeout,
                options.RedisInstanceName,
                cancellationToken);

            if (distributedLock != null)
            {
                _logger.LogDebug("获取分布式锁成功: {LockKey}", lockKey);

                // 再次检查缓存（双重检查锁定模式）
                var cachedResult = await GetAsyncResultInternal<T>(key, options, cancellationToken);
                if (cachedResult.Found)
                {
                    _logger.LogDebug("双重检查缓存命中: {Key}", key);
                    return cachedResult.Value;
                }

                // 调用工厂方法获取值
                var value = await factory();
                if (value != null)
                {
                    // 设置缓存
                    await SetAsyncInternal(key, value, options, cancellationToken);
                    _logger.LogDebug("通过工厂方法获取值并设置缓存: {Key}", key);
                }

                return value;
            }
            else
            {
                _logger.LogWarning("获取分布式锁失败: {LockKey}", lockKey);

                // 锁获取失败，执行降级策略
                return await HandleLockFailure(key, factory, options, cancellationToken);
            }
        }
        finally
        {
            if (distributedLock != null)
            {
                await distributedLock.DisposeAsync();
                _logger.LogDebug("释放分布式锁: {LockKey}", lockKey);
            }
        }
    }

    /// <summary>
    ///     不使用分布式锁的 GetOrSet 操作
    /// </summary>
    private async Task<T?> GetOrSetWithoutLockAsync<T>(string key, Func<Task<T>> factory, CacheServiceOptions options,
        CancellationToken cancellationToken)
    {
        // 直接调用工厂方法
        var value = await factory();
        if (value != null)
        {
            // 设置缓存
            await SetAsyncInternal(key, value, options, cancellationToken);
            _logger.LogDebug("通过工厂方法获取值并设置缓存 (无锁): {Key}", key);
        }

        return value;
    }


    /// <summary>
    ///     处理锁获取失败的降级策略
    /// </summary>
    private async Task<T?> HandleLockFailure<T>(string key, Func<Task<T>> factory, CacheServiceOptions options,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning("分布式锁获取失败: {Key}", key);

        // 需求 4.4: 工厂方法降级策略
        if (options.FallbackToFactory)
            try
            {
                _logger.LogDebug("锁获取失败，回退到工厂方法: {Key}", key);
                var value = await factory();

                // 尝试设置缓存（可能会失败，但不影响返回值）
                if (value != null)
                    try
                    {
                        await SetAsyncInternal(key, value, options, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "锁失败降级时设置缓存失败: {Key}", key);
                    }

                return value;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "工厂方法降级策略执行失败: {Key}", key);
                // 如果工厂方法失败，继续尝试其他降级策略
            }

        // 尝试其他降级策略
        var fallbackResult = await ExecuteFallbackStrategy<T>(key, "LockFailure", options);
        if (fallbackResult.HasValue) return fallbackResult.Value;

        _logger.LogWarning("锁获取失败且无可用降级策略: {Key}", key);
        return default;
    }
}
