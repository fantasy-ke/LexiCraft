using BuildingBlocks.Caching.Internal;
using BuildingBlocks.Caching.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Caching.Services;

internal sealed partial class CacheService
{
    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, Action<CacheServiceOptions>? configure = null,
        CancellationToken cancellationToken = default)
    {
        var options = GetEffectiveOptions(configure);
        var result = await GetAsyncResultInternal<T>(key, options, cancellationToken);
        return result.Value;
    }

    private async Task<CacheReadResult<T>> GetAsyncResultInternal<T>(
        string key,
        CacheServiceOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            return await TryGetAsyncInternal<T>(key, options, cancellationToken);
        }
        catch (Exception ex)
        {
            var fallback = await HandleException<T>(ex, options, key, "GetAsync");
            return fallback is null
                ? CacheReadResult<T>.Miss
                : CacheReadResult<T>.Hit(fallback);
        }
    }

    private async Task<CacheReadResult<T>> TryGetAsyncInternal<T>(
        string key,
        CacheServiceOptions options,
        CancellationToken cancellationToken)
    {
        if (options.UseLocal)
        {
            var localKey = GetLocalCacheKey(key, options);
            if (_memoryCache.TryGetValue(localKey, out T? localValue))
            {
                    _logger.LogDebug("缓存命中 (本地): {Key}", key);
                return CacheReadResult<T>.Hit(localValue);
            }
        }

        if (options.UseDistributed)
        {
            var distributedResult = await _distributedCacheService.GetAsync<T>(key, options, cancellationToken);
            if (distributedResult.Found)
            {
                    _logger.LogDebug("缓存命中 (分布式): {Key}", key);

                if (options.UseLocal)
                {
                    var localExpiry = GetLocalExpiry(options);
                    var localKey = GetLocalCacheKey(key, options);
                    _memoryCache.Set(localKey, distributedResult.Value, localExpiry);
                        _logger.LogDebug("同步到本地缓存: {Key}", key);
                }

                return distributedResult;
            }
        }

        _logger.LogDebug("缓存未命中: {Key}", key);
        return CacheReadResult<T>.Miss;
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, Action<CacheServiceOptions>? configure = null,
        CancellationToken cancellationToken = default)
    {
        var options = GetEffectiveOptions(configure);
        await SetAsyncInternal(key, value, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> RemoveAsync(string key, Action<CacheServiceOptions>? configure = null,
        CancellationToken cancellationToken = default)
    {
        var options = GetEffectiveOptions(configure);
        var success = true;

        try
        {
            // 删除分布式缓存
            if (options.UseDistributed)
            {
                var distributedResult = await _distributedCacheService.RemoveAsync(key, options, cancellationToken);
                success = success && distributedResult;
                _logger.LogDebug("删除分布式缓存: {Key}, 结果: {Success}", key, distributedResult);
            }

            // 删除本地缓存
            if (options.UseLocal)
            {
                var localKey = GetLocalCacheKey(key, options);
                _memoryCache.Remove(localKey);
                _logger.LogDebug("删除本地缓存: {Key}", key);
            }

            return success;
        }
        catch (Exception ex)
        {
            await HandleException<object>(ex, options, key, "RemoveAsync");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, Action<CacheServiceOptions>? configure = null,
        CancellationToken cancellationToken = default)
    {
        var options = GetEffectiveOptions(configure);

        try
        {
            // 检查本地缓存
            if (options.UseLocal)
            {
                var localKey = GetLocalCacheKey(key, options);
                if (_memoryCache.TryGetValue(localKey, out _))
                {
                    _logger.LogDebug("本地缓存存在: {Key}", key);
                    return true;
                }
            }

            // 检查分布式缓存
            if (options.UseDistributed)
            {
                var exists = await _distributedCacheService.ExistsAsync(key, options, cancellationToken);
                _logger.LogDebug("分布式缓存存在检查: {Key}, 结果: {Exists}", key, exists);
                return exists;
            }

            return false;
        }
        catch (Exception ex)
        {
            await HandleException<object>(ex, options, key, "ExistsAsync");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SetExpirationAsync(string key, TimeSpan expirationTime,
        Action<CacheServiceOptions>? configure = null, CancellationToken cancellationToken = default)
    {
        var options = GetEffectiveOptions(configure);

        try
        {
            // 只有分布式缓存支持设置过期时间
            if (options.UseDistributed)
            {
                var result = await _distributedCacheService.SetExpirationAsync(key, expirationTime, options, cancellationToken);
                _logger.LogDebug("设置分布式缓存过期时间: {Key}, 过期时间: {Expiration}, 结果: {Success}", key, expirationTime, result);
                return result;
            }

            _logger.LogWarning("未启用分布式缓存，无法设置过期时间: {Key}", key);
            return false;
        }
        catch (Exception ex)
        {
            await HandleException<object>(ex, options, key, "SetExpirationAsync");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory,
        Action<CacheServiceOptions>? configure = null, CancellationToken cancellationToken = default)
    {
        var options = GetEffectiveOptions(configure);

        try
        {
            // 首先尝试获取缓存
            var cachedResult = await GetAsyncResultInternal<T>(key, options, cancellationToken);
            if (cachedResult.Found) return cachedResult.Value;

            // 缓存未命中，需要通过工厂方法获取值
            if (options.EnableLock)
                // 使用分布式锁防止缓存击穿
                return await GetOrSetWithLockAsync(key, factory, options, cancellationToken);

            // 不使用锁，直接调用工厂方法
            return await GetOrSetWithoutLockAsync(key, factory, options, cancellationToken);
        }
        catch (Exception ex)
        {
            return await HandleException<T>(ex, options, key, "GetOrSetAsync");
        }
    }


    /// <summary>
    ///     内部设置缓存方法，使用已解析的配置选项
    /// </summary>
    private async Task SetAsyncInternal<T>(string key, T value, CacheServiceOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            // 调整 TTL
            var expiry = GetDistributedCacheExpiry(options, value);

            // 设置分布式缓存
            if (options.UseDistributed)
            {
                await _distributedCacheService.SetAsync(key, value, options, expiry, cancellationToken);
                _logger.LogDebug("设置分布式缓存: {Key}, TTL: {Expiry}", key, expiry);
            }

            // 设置本地缓存
            if (options.UseLocal)
            {
                var localExpiry = GetLocalExpiry(options);
                var localKey = GetLocalCacheKey(key, options);
                _memoryCache.Set(localKey, value, localExpiry);
                _logger.LogDebug("设置本地缓存: {Key}, TTL: {LocalExpiry}", key, localExpiry);
            }
        }
        catch (Exception ex)
        {
            await HandleException<object>(ex, options, key, "SetAsync");
        }
    }
}
