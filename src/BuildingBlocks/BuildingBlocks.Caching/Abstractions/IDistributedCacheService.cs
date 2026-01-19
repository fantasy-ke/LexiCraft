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
    /// 原子性设置缓存（仅当键不存在时）
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="options">缓存配置选项</param>
    /// <param name="expiry">过期时间，为空时使用 options.Expiry</param>
    /// <returns>是否设置成功</returns>
    Task<bool> SetIfNotExistsAsync<T>(string key, T value, CacheServiceOptions options, TimeSpan? expiry = null);

    /// <summary>
    /// 获取或设置缓存值
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="factory">值工厂函数</param>
    /// <param name="options">缓存配置选项</param>
    /// <param name="expiry">过期时间，为空时使用 options.Expiry</param>
    /// <returns>缓存值</returns>
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, CacheServiceOptions options, TimeSpan? expiry = null);

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
    /// 原子性递增
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="increment">递增值</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>递增后的值</returns>
    Task<long> IncrementAsync(string key, long increment, CacheServiceOptions options);

    /// <summary>
    /// 原子性递减
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="decrement">递减值</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>递减后的值</returns>
    Task<long> DecrementAsync(string key, long decrement, CacheServiceOptions options);

    /// <summary>
    /// 获取分布式锁
    /// </summary>
    /// <param name="key">锁键</param>
    /// <param name="expirationTime">锁过期时间</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>是否获取成功</returns>
    Task<bool> AcquireLockAsync(string key, TimeSpan expirationTime, CacheServiceOptions options);

    /// <summary>
    /// 释放分布式锁
    /// </summary>
    /// <param name="key">锁键</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>是否释放成功</returns>
    Task<bool> ReleaseLockAsync(string key, CacheServiceOptions options);

    /// <summary>
    /// 检查是否持有分布式锁
    /// </summary>
    /// <param name="key">锁键</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>是否持有锁</returns>
    Task<bool> IsLockAcquiredAsync(string key, CacheServiceOptions options);

    /// <summary>
    /// 批量删除缓存
    /// </summary>
    /// <param name="keys">缓存键集合</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>删除成功的数量</returns>
    Task<long> RemoveManyAsync(IEnumerable<string> keys, CacheServiceOptions options);

    /// <summary>
    /// 扫描匹配的键
    /// </summary>
    /// <param name="pattern">匹配模式</param>
    /// <param name="pageSize">页面大小</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>匹配的键集合</returns>
    Task<IEnumerable<string>> ScanKeysAsync(string pattern, int pageSize, CacheServiceOptions options);

    /// <summary>
    /// 获取二进制缓存值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>二进制数据</returns>
    Task<byte[]?> GetBytesAsync(string key, CacheServiceOptions options);

    /// <summary>
    /// 设置二进制缓存值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="value">二进制数据</param>
    /// <param name="expiry">过期时间</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>是否设置成功</returns>
    Task<bool> SetBytesAsync(string key, byte[] value, TimeSpan? expiry, CacheServiceOptions options);

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

    /// <summary>
    /// 设置Hash字段值
    /// </summary>
    /// <param name="key">Hash键</param>
    /// <param name="field">字段名</param>
    /// <param name="value">字段值</param>
    /// <param name="options">缓存配置选项</param>
    /// <param name="expiry">过期时间</param>
    /// <returns>是否设置成功</returns>
    Task<bool> HashSetAsync(string key, string field, string value, CacheServiceOptions options, TimeSpan? expiry = null);

    /// <summary>
    /// 获取Hash字段值
    /// </summary>
    /// <param name="key">Hash键</param>
    /// <param name="field">字段名</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>字段值</returns>
    Task<string?> HashGetAsync(string key, string field, CacheServiceOptions options);

    /// <summary>
    /// 删除Hash字段
    /// </summary>
    /// <param name="key">Hash键</param>
    /// <param name="field">字段名</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>是否删除成功</returns>
    Task<bool> HashDeleteAsync(string key, string field, CacheServiceOptions options);

    /// <summary>
    /// 删除Hash多个字段
    /// </summary>
    /// <param name="key">Hash键</param>
    /// <param name="fields">字段名集合</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>删除成功的数量</returns>
    Task<long> HashDeleteManyAsync(string key, IEnumerable<string> fields, CacheServiceOptions options);

    /// <summary>
    /// 递增Hash字段值
    /// </summary>
    /// <param name="key">Hash键</param>
    /// <param name="field">字段名</param>
    /// <param name="increment">递增值</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>递增后的值</returns>
    Task<long> HashIncrementAsync(string key, string field, long increment, CacheServiceOptions options);

    /// <summary>
    /// 递减Hash字段值
    /// </summary>
    /// <param name="key">Hash键</param>
    /// <param name="field">字段名</param>
    /// <param name="decrement">递减值</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>递减后的值</returns>
    Task<long> HashDecrementAsync(string key, string field, long decrement, CacheServiceOptions options);

    /// <summary>
    /// 获取Hash字段数量
    /// </summary>
    /// <param name="key">Hash键</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>字段数量</returns>
    Task<long> HashLengthAsync(string key, CacheServiceOptions options);

    /// <summary>
    /// 获取Hash所有字段名
    /// </summary>
    /// <param name="key">Hash键</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>字段名集合</returns>
    Task<IEnumerable<string>> HashKeysAsync(string key, CacheServiceOptions options);

    /// <summary>
    /// 获取Hash所有值
    /// </summary>
    /// <param name="key">Hash键</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>值集合</returns>
    Task<IEnumerable<string>> HashValuesAsync(string key, CacheServiceOptions options);

    /// <summary>
    /// 批量递增Hash字段值（使用Lua脚本原子性执行）
    /// </summary>
    /// <param name="hashId">Hash键</param>
    /// <param name="keyValuePairs">字段和递增值的键值对集合</param>
    /// <param name="expiry">过期时间</param>
    /// <param name="options">缓存配置选项</param>
    Task HashIncrementManyAsync(string hashId, IEnumerable<KeyValuePair<string, long>> keyValuePairs, TimeSpan? expiry, CacheServiceOptions options);
}