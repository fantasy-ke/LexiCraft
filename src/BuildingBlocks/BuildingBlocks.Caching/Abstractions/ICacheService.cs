using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BuildingBlocks.Caching.Configuration;

namespace BuildingBlocks.Caching.Abstractions;

/// <summary>
/// 缓存服务接口，提供高级缓存操作
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// 异步获取缓存值，如果不存在则通过工厂方法设置
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="factory">当缓存不存在时用于生成值的工厂方法</param>
    /// <param name="configure">配置选项委托，用于覆盖默认配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存值或工厂方法生成的值</returns>
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, Action<CacheServiceOptions>? configure = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步设置缓存值
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="configure">配置选项委托，用于覆盖默认配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SetAsync<T>(string key, T value, Action<CacheServiceOptions>? configure = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步获取缓存值
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="configure">配置选项委托，用于覆盖默认配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存值，不存在时返回 null</returns>
    Task<T?> GetAsync<T>(string key, Action<CacheServiceOptions>? configure = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步删除缓存
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="configure">配置选项委托，用于覆盖默认配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功删除</returns>
    Task<bool> RemoveAsync(string key, Action<CacheServiceOptions>? configure = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步检查缓存是否存在
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="configure">配置选项委托，用于覆盖默认配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存是否存在</returns>
    Task<bool> ExistsAsync(string key, Action<CacheServiceOptions>? configure = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步设置缓存过期时间
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="expirationTime">过期时间</param>
    /// <param name="configure">配置选项委托，用于覆盖默认配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功设置过期时间</returns>
    Task<bool> SetExpirationAsync(string key, TimeSpan expirationTime, Action<CacheServiceOptions>? configure = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步获取或设置 Hash 缓存值（返回强类型）
    /// </summary>
    /// <typeparam name="TResult">返回值类型</typeparam>
    /// <param name="hashKey">Hash 缓存键</param>
    /// <param name="queryFields">要查询的字段列表</param>
    /// <param name="parseFromHash">从 Hash 字典解析为强类型对象的函数</param>
    /// <param name="buildFullCache">当缓存不存在或过期时用于构建完整缓存的工厂方法</param>
    /// <param name="configure">配置选项委托，用于覆盖默认配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>解析后的强类型对象，不存在时返回 null</returns>
    Task<TResult?> GetOrSetHashAsync<TResult>(
        string hashKey,
        IEnumerable<string> queryFields,
        Func<Dictionary<string, string>, TResult?> parseFromHash,
        Func<Task<Dictionary<string, string>>> buildFullCache,
        Action<CacheServiceOptions>? configure = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步获取或设置 Hash 缓存值（返回原始字典）
    /// </summary>
    /// <param name="hashKey">Hash 缓存键</param>
    /// <param name="queryFields">要查询的字段列表</param>
    /// <param name="buildFullCache">当缓存不存在或过期时用于构建完整缓存的工厂方法</param>
    /// <param name="configure">配置选项委托，用于覆盖默认配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Hash 字段值字典，不存在时返回 null</returns>
    Task<Dictionary<string, string>?> GetOrSetHashAsync(
        string hashKey,
        IEnumerable<string> queryFields,
        Func<Task<Dictionary<string, string>>> buildFullCache,
        Action<CacheServiceOptions>? configure = null,
        CancellationToken cancellationToken = default);
}