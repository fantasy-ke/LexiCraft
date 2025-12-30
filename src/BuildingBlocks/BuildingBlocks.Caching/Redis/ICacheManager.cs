using FreeRedis;

namespace BuildingBlocks.Caching.Redis;

public interface ICacheManager
{
    public RedisClient Client { get; }

    #region 基础操作 (L1 + L2)

    /// <summary>
    /// 获取缓存（优先从本地一级缓存获取）
    /// </summary>
    string? Get(string key);

    /// <summary>
    /// 获取缓存（优先从本地一级缓存获取）
    /// </summary>
    Task<string?> GetAsync(string key);

    /// <summary>
    /// 获取对象缓存（优先从本地一级缓存获取）
    /// </summary>
    T? Get<T>(string key);

    /// <summary>
    /// 获取对象缓存（优先从本地一级缓存获取）
    /// </summary>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// 设置缓存（同步更新本地一级缓存）
    /// </summary>
    void Set(string key, object value, TimeSpan? timeout = null);

    /// <summary>
    /// 设置缓存（同步更新本地一级缓存）
    /// </summary>
    Task SetAsync(string key, object value, TimeSpan? timeout = null);

    /// <summary>
    /// 删除缓存（同步同步本地一级缓存）
    /// </summary>
    void Remove(string key);

    /// <summary>
    /// 删除缓存（同步同步本地一级缓存）
    /// </summary>
    Task RemoveAsync(string key);

    /// <summary>
    /// 根据前缀删除缓存
    /// </summary>
    Task RemoveByPrefixAsync(string prefix);

    /// <summary>
    /// 检查缓存是否存在
    /// </summary>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// 设置缓存过期时间
    /// </summary>
    bool Expire(string key, TimeSpan timeout);

    /// <summary>
    /// 设置缓存过期时间
    /// </summary>
    Task<bool> ExpireAsync(string key, TimeSpan timeout);

    #endregion

    #region 高级操作 (二层缓存模式)

    /// <summary>
    /// 获取或设置缓存（防击穿/二层缓存模式）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="acquire">数据获取委托</param>
    /// <param name="timeout">过期时间</param>
    /// <returns></returns>
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> acquire, TimeSpan? timeout = null);

    /// <summary>
    /// 获取缓存 (支持配置)
    /// </summary>
    Task<T?> GetAsync<T>(string key, Action<CacheOptions>? configure = null);

    /// <summary>
    /// 设置缓存 (支持配置)
    /// </summary>
    Task SetAsync(string key, object value, Action<CacheOptions>? configure = null);

    /// <summary>
    /// 获取或设置缓存 (支持配置)
    /// </summary>
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> acquire, Action<CacheOptions>? configure = null);

    #endregion
}