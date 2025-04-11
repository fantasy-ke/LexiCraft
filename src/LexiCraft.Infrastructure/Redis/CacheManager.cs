﻿using FreeRedis;
using LexiCraft.Infrastructure.Extensions;
using LexiCraft.Redis;
using Microsoft.Extensions.Options;

namespace LexiCraft.Infrastructure.Redis;

public class CacheManager : RedisCacheBaseService, ICacheManager
{
    private readonly RedisClient _redisClient;

    public RedisClient Client { get; set; }

    public CacheManager(RedisClient redisClient,IOptions<RedisCacheOptions> options) : base(redisClient,options)
    {
        _redisClient = redisClient;
        Client = _redisClient;
    }

    /// <summary>
    /// 创建缓存Key
    /// </summary>
    /// <param name="idKey"></param>
    /// <returns></returns>
    protected string BuildKey(string idKey)
    {
        return $"Cache_{GetType().FullName}_{idKey}";
    }

    /// <summary>
    /// 设置缓存
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void SetCache(string key, object value)
    {
        string cacheKey = BuildKey(key);
        _redisClient.Set(cacheKey, value.ToJson());
    }

    public async Task SetCacheAsync(string key, object value)
    {
        string cacheKey = BuildKey(key);
        await _redisClient.SetAsync(cacheKey, value.ToJson());
    }

    public void SetCache(string key, object value, TimeSpan timeout)
    {
        string cacheKey = BuildKey(key);
        _redisClient.Set(cacheKey, value.ToJson(), timeout.Seconds);
    }

    public async Task SetCacheAsync(string key, object value, TimeSpan timeout)
    {
        string cacheKey = BuildKey(key);
        await _redisClient.SetAsync(cacheKey, value.ToJson(), timeout.Seconds);
    }


    public string GetCache(string idKey)
    {
        if (idKey.IsNullEmpty())
        {
            return null;
        }
        string cacheKey = BuildKey(idKey);
        var cache = _redisClient.Get(cacheKey);
        return cache;
    }
    public async Task<string> GetCacheAsync(string key)
    {
        if (key.IsNullEmpty())
        {
            return null;
        }
        string cacheKey = BuildKey(key);
        var cache = await _redisClient.GetAsync(cacheKey);
        return cache;
    }

    public T GetCache<T>(string key)
    {
        var cache = GetCache(key);
        if (!cache.IsNullEmpty())
        {
            return cache.ToObject<T>();
        }
        return default;
    }

    public async Task<T> GetCacheAsync<T>(string key)
    {
        var cache = await GetCacheAsync(key);
        if (!string.IsNullOrEmpty(cache))
        {
            return cache.ToObject<T>();
        }
        return default;
    }

    public async Task<T> GetCacheAsync<T>(string key, Func<Task<T>> dataRetriever, TimeSpan timeout)
    {
        var result = await GetCacheAsync<T>(key);
        if (result != null)
        {
            return result;
        }
        string cacheKey = BuildKey(key);
        using (var redisLock = _redisClient.Lock(cacheKey, 10))
        {
            if (redisLock == null)
            {
                throw new Exception("抢不到所");
            }

            Task<T> task = dataRetriever();
            bool flag = !task.IsCompleted;
            if (flag)
            {
                flag = await Task.WhenAny(task, Task.Delay(10)) != task;
            }
            //if (flag)
            //{
            //    throw new UserFriendlyException("任务执行错误");
            //}
            T item = await task;
            if (item == null) { return result; }
            await _redisClient.SetAsync(cacheKey, item.ToJson(), timeout.Seconds);

            redisLock.Dispose();

            result = item;
        }

        return result;
    }

    public void RemoveCache(string key)
    {
        _redisClient.Del(BuildKey(key));
    }

    public async Task RemoveCacheAsync(string key)
    {
        await _redisClient.DelAsync(BuildKey(key));
    }
    public async Task RemoveByPrefixAsync(string key)
    {

        var keys = await _redisClient.KeysAsync("*" + BuildKey(key) + "*");
        foreach (var item in keys)
        {
            await _redisClient.DelAsync(item);
        }

    }

    public async Task<bool> ExistsCacheAsync(string key)
    {
        return await _redisClient.ExistsAsync(BuildKey(key));
    }
}
