using System.Linq;
using BuildingBlocks.Extensions.System;
using FreeRedis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Caching.Redis;

public class CacheManager : RedisCacheBaseService, ICacheManager
{
    private readonly RedisClient _redisClient;
    private readonly ILogger<CacheManager> _logger;

    public CacheManager(
        RedisClient redisClient, 
        IOptions<RedisCacheOptions> options,
        ILogger<CacheManager> logger) : base(redisClient, options)
    {
        _redisClient = redisClient;
        _logger = logger;
    }

    public RedisClient Client => _redisClient;

    #region 基础操作实现

    public string? Get(string key)
    {
        if (key.IsNullEmpty()) return null;
        return _redisClient.Get(FormatKey(key));
    }

    public async Task<string?> GetAsync(string key)
    {
        if (key.IsNullEmpty()) return null;
        return await _redisClient.GetAsync(FormatKey(key));
    }

    public T? Get<T>(string key)
    {
        if (key.IsNullEmpty()) return default;
        return _redisClient.Get<T>(FormatKey(key));
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        if (key.IsNullEmpty()) return default;
        return await _redisClient.GetAsync<T>(FormatKey(key));
    }

    public void Set(string key, object value, TimeSpan? timeout = null)
    {
        if (key.IsNullEmpty()) return;
        var formattedKey = FormatKey(key);
        if (timeout.HasValue)
            _redisClient.Set(formattedKey, value, (int)timeout.Value.TotalSeconds);
        else
            _redisClient.Set(formattedKey, value);
    }

    public async Task SetAsync(string key, object value, TimeSpan? timeout = null)
    {
        if (key.IsNullEmpty()) return;
        var formattedKey = FormatKey(key);
        if (timeout.HasValue)
            await _redisClient.SetAsync(formattedKey, value, (int)timeout.Value.TotalSeconds);
        else
            await _redisClient.SetAsync(formattedKey, value);
    }

    public void Remove(string key)
    {
        if (key.IsNullEmpty()) return;
        _redisClient.Del(FormatKey(key));
    }

    public async Task RemoveAsync(string key)
    {
        if (key.IsNullEmpty()) return;
        await _redisClient.DelAsync(FormatKey(key));
    }

    public async Task RemoveByPrefixAsync(string prefix)
    {
        if (prefix.IsNullEmpty()) return;
        var formattedPrefix = FormatKey(prefix);
        var keys = await _redisClient.KeysAsync(formattedPrefix + "*");
        if (keys != null && keys.Length > 0)
        {
            await _redisClient.DelAsync(keys);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        if (key.IsNullEmpty()) return false;
        return await _redisClient.ExistsAsync(FormatKey(key));
    }

    #region Implementation of IRedisCacheBaseService (Mapping & Compatibility)

    // Bridge SetCache calls to our optimized Set/SetAsync
    public void SetCache(string key, object value) => Set(key, value);
    public Task SetCacheAsync(string key, object value) => SetAsync(key, value);
    public void SetCache(string key, object value, TimeSpan timeout) => Set(key, value, timeout);
    public Task SetCacheAsync(string key, object value, TimeSpan timeout) => SetAsync(key, value, timeout);

    // Ensure Del/DelAsync are prefix-safe (shadowing/overriding base implementation)
    public new long Del(params string[] keys) => _redisClient.Del(keys.Select(FormatKey).ToArray());
    public new async Task<long> DelAsync(params string[] keys) => await _redisClient.DelAsync(keys.Select(FormatKey).ToArray());

    #endregion

    #endregion

    #region 高级操作实现

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> acquire, TimeSpan? timeout = null)
    {
        if (key.IsNullEmpty()) return await acquire();

        var cacheKey = FormatKey(key);
        
        // 1. 尝试从缓存获取 (如果开启了 ClientSideCaching，这里会自动命中 L1)
        var result = await _redisClient.GetAsync<T>(cacheKey);
        if (result != null) return result;

        // 2. 缓存未命中，加锁防止击穿
        var lockKey = $"Lock:{cacheKey}";
        using (var redisLock = _redisClient.Lock(lockKey, 10)) // 10秒锁
        {
            if (redisLock == null)
            {
                _logger.LogWarning($"Failed to acquire lock for {lockKey}, waiting and retrying...");
                await Task.Delay(100);
                return await GetOrSetAsync(key, acquire, timeout);
            }

            // 再次检查缓存（双重检查）
            result = await _redisClient.GetAsync<T>(cacheKey);
            if (result != null) return result;

            // 3. 回源获取数据
            result = await acquire();

            if (result != null)
            {
                // 4. 写入缓存
                if (timeout.HasValue)
                    await _redisClient.SetAsync(cacheKey, result, (int)timeout.Value.TotalSeconds);
                else
                    await _redisClient.SetAsync(cacheKey, result);
            }
        }

        return result;
    }

    #endregion

    /// <summary>
    /// 组合 KeyPrefix 和 TypeName 进行格式化
    /// </summary>
    protected new string FormatKey(string key)
    {
        var typePrefix = $"Cache_{GetType().Name}";
        var formattedKey = $"{typePrefix}_{key}";
        return base.FormatKey(formattedKey);
    }
}