using System;
using System.Threading.Tasks;

namespace BuildingBlocks.Caching.Abstractions;

/// <summary>
/// 缓存管理器接口（兼容层）
/// </summary>
[Obsolete("请使用 IDistributedCacheService 或 ICacheService 替代")]
public interface ICacheManager
{
    /// <summary>
    /// 异步获取缓存值
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <returns>缓存值，不存在时返回 null</returns>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// 异步设置缓存值
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="expiry">过期时间</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);

    /// <summary>
    /// 异步删除缓存
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>是否成功删除</returns>
    Task<bool> RemoveAsync(string key);

    /// <summary>
    /// 异步检查缓存是否存在
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>缓存是否存在</returns>
    Task<bool> ExistsAsync(string key);
}