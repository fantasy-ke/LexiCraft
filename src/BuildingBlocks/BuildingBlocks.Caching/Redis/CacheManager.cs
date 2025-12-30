using System.IO.Compression;
using System.Linq;
using BuildingBlocks.Extensions.System;
using FreeRedis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace BuildingBlocks.Caching.Redis;

public class CacheManager : ICacheManager
{
    private readonly RedisClient _redisClient;
    private readonly IOptions<RedisCacheOptions> _options;
    private readonly ILogger<CacheManager> _logger;

    public CacheManager(
        RedisClient redisClient, 
        IOptions<RedisCacheOptions> options,
        ILogger<CacheManager> logger)
    {
        _redisClient = redisClient;
        _options = options;
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

    public bool Expire(string key, TimeSpan timeout)
    {
         if (key.IsNullEmpty()) return false;
         return _redisClient.Expire(FormatKey(key), (int)timeout.TotalSeconds);
    }

    public async Task<bool> ExpireAsync(string key, TimeSpan timeout)
    {
        if (key.IsNullEmpty()) return false;
        return await _redisClient.ExpireAsync(FormatKey(key), (int)timeout.TotalSeconds);
    }

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

    #region 高级配置操作实现
    
    public async Task<T?> GetAsync<T>(string key, Action<CacheOptions>? configure)
    {
        if (key.IsNullEmpty()) return default;
        
        var options = new CacheOptions();
        configure?.Invoke(options);

        var cacheKey = FormatKey(key);

        try
        {
            // 如果启用了GZip
            if (options.EnableGZip)
            {
                // 获取字节数组
                var bytes = await _redisClient.GetAsync<byte[]>(cacheKey);
                if (bytes == null || bytes.Length == 0) return default;
                
                // 解压并反序列化
                return Decompress<T>(bytes);
            }
            // 否则正常获取
            // 注意：FreeRedis ClientSideCaching 默认是全局的，如果 options.EnableLocalCache = false，
            // 我们需要绕过本地缓存？ FreeRedis 目前没有直接绕过的 API，除非使用不同的 Client 实例
            // 这里暂且忽略 EnableLocalCache = false 的特殊逻辑，或者认为 GetAsync 默认就走 Client 逻辑
            
            return await _redisClient.GetAsync<T>(cacheKey);
        }
        catch (Exception ex)
        {
            if (options.HideErrors)
            {
                _logger.LogError(ex, $"Redis GetAsync Error for key: {key}");
                return default;
            }
            throw;
        }
    }

    public async Task SetAsync(string key, object value, Action<CacheOptions>? configure)
    {
        if (key.IsNullEmpty()) return;
        
        var options = new CacheOptions();
        configure?.Invoke(options);
        
        var cacheKey = FormatKey(key);

        try
        {
            if (options.EnableGZip)
            {
                // 压缩
                var bytes = Compress(value);
                if (options.Expiration.HasValue)
                     await _redisClient.SetAsync(cacheKey, bytes, (int)options.Expiration.Value.TotalSeconds);
                else
                     await _redisClient.SetAsync(cacheKey, bytes);
            }
            else
            {
                if (options.Expiration.HasValue)
                    await _redisClient.SetAsync(cacheKey, value, (int)options.Expiration.Value.TotalSeconds);
                else
                    await _redisClient.SetAsync(cacheKey, value);
            }
        }
        catch (Exception ex)
        {
            if (options.HideErrors)
            {
                 _logger.LogError(ex, $"Redis SetAsync Error for key: {key}");
                 return;
            }
            throw;
        }
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, Action<CacheOptions>? configure = null)
    {
        // 尝试获取
        var value = await GetAsync<T>(key, configure);
        if (value != null && !value.Equals(default(T))) 
        {
            return value;
        }

        // 获取不到则执行工厂方法
        value = await factory();

        if (value == null) return default;

        // 设置缓存
        await SetAsync(key, value, configure);

        return value;
    }

    #endregion

    #region Helpers

    private byte[] Compress(object value)
    {
        var json = JsonConvert.SerializeObject(value);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        
        using var mStream = new MemoryStream();
        using (var gStream = new GZipStream(mStream, CompressionMode.Compress))
        {
            gStream.Write(bytes, 0, bytes.Length);
        }
        return mStream.ToArray();
    }

    private T? Decompress<T>(byte[] bytes)
    {
        using var mStream = new MemoryStream(bytes);
        using var gStream = new GZipStream(mStream, CompressionMode.Decompress);
        using var resultStream = new MemoryStream();
        gStream.CopyTo(resultStream);
        
        var json = System.Text.Encoding.UTF8.GetString(resultStream.ToArray());
        return JsonConvert.DeserializeObject<T>(json);
    }

    /// <summary>
    /// 组合 KeyPrefix 和 TypeName 进行格式化
    /// </summary>
    protected string FormatKey(string key)
    {
        var typePrefix = $"Cache_{GetType().Name}";
        var preKey = $"{typePrefix}_{key}";
        return string.IsNullOrWhiteSpace(_options.Value.KeyPrefix) ? preKey : $"{_options.Value.KeyPrefix}:{preKey}";
    }

    #endregion
}