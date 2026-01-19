using BuildingBlocks.Caching.Configuration;

namespace BuildingBlocks.Caching.Abstractions;

/// <summary>
/// 分布式缓存服务接口，提供基础缓存操作
/// </summary>
public interface IDistributedCacheService
{
    /// <summary>
    /// 异步获取缓存值
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>缓存值，不存在时返回 null</returns>
    Task<T?> GetAsync<T>(string key, CacheServiceOptions options);

    /// <summary>
    /// 异步设置缓存值
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="options">缓存配置选项</param>
    /// <param name="expiry">过期时间，为空时使用 options.Expiry</param>
    Task SetAsync<T>(string key, T value, CacheServiceOptions options, TimeSpan? expiry = null);

    /// <summary>
    /// 异步删除缓存
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>是否成功删除</returns>
    Task<bool> RemoveAsync(string key, CacheServiceOptions options);

    /// <summary>
    /// 异步检查缓存是否存在
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>缓存是否存在</returns>
    Task<bool> ExistsAsync(string key, CacheServiceOptions options);

    /// <summary>
    /// 异步设置缓存过期时间
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="expiry">过期时间</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>是否成功设置过期时间</returns>
    Task<bool> SetExpirationAsync(string key, TimeSpan expiry, CacheServiceOptions options);

    /// <summary>
    /// Hash 操作 - 异步获取多个字段
    /// </summary>
    /// <param name="key">Hash 键</param>
    /// <param name="fields">要获取的字段列表</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>字段值字典，Hash 不存在时返回 null</returns>
    Task<Dictionary<string, string>?> HashGetAsync(string key, IEnumerable<string> fields, CacheServiceOptions options);

    /// <summary>
    /// Hash 操作 - 异步设置多个字段
    /// </summary>
    /// <param name="key">Hash 键</param>
    /// <param name="values">字段值字典</param>
    /// <param name="options">缓存配置选项</param>
    /// <param name="expiry">过期时间，为空时使用 options.Expiry</param>
    Task HashSetAsync(string key, Dictionary<string, string> values, CacheServiceOptions options, TimeSpan? expiry = null);

    /// <summary>
    /// Hash 操作 - 异步检查字段是否存在
    /// </summary>
    /// <param name="key">Hash 键</param>
    /// <param name="field">字段名</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>字段是否存在</returns>
    Task<bool> HashExistsAsync(string key, string field, CacheServiceOptions options);
}