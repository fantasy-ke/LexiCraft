using BuildingBlocks.Caching.Abstractions;
using System;
using System.Threading.Tasks;

namespace BuildingBlocks.Caching.Services;

/// <summary>
/// 缓存管理器适配器（兼容层实现）
/// </summary>
[Obsolete("请使用 IDistributedCacheService 或 ICacheService 替代")]
public class CacheManagerAdapter : ICacheManager
{
    private readonly IDistributedCacheService _distributedCacheService;

    /// <summary>
    /// 初始化缓存管理器适配器
    /// </summary>
    /// <param name="distributedCacheService">分布式缓存服务</param>
    public CacheManagerAdapter(IDistributedCacheService distributedCacheService)
    {
        _distributedCacheService = distributedCacheService ?? throw new ArgumentNullException(nameof(distributedCacheService));
    }

    /// <summary>
    /// 异步获取缓存值
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <returns>缓存值，不存在时返回 null</returns>
    public async Task<T?> GetAsync<T>(string key)
    {
        return await _distributedCacheService.GetAsync<T>(key);
    }

    /// <summary>
    /// 异步设置缓存值
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="expiry">过期时间</param>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        await _distributedCacheService.SetAsync(key, value, expiry);
    }

    /// <summary>
    /// 异步删除缓存
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>是否成功删除</returns>
    public async Task<bool> RemoveAsync(string key)
    {
        return await _distributedCacheService.RemoveAsync(key);
    }

    /// <summary>
    /// 异步检查缓存是否存在
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>缓存是否存在</returns>
    public async Task<bool> ExistsAsync(string key)
    {
        return await _distributedCacheService.ExistsAsync(key);
    }
}