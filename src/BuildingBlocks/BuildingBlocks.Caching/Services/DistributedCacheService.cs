using BuildingBlocks.Caching.Abstractions;
using BuildingBlocks.Caching.Configuration;
using BuildingBlocks.Caching.Factories;
using BuildingBlocks.Caching.Serialization;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BuildingBlocks.Caching.Services;

/// <summary>
/// 分布式缓存服务实现
/// </summary>
public class DistributedCacheService : IDistributedCacheService
{
    private readonly IRedisConnectionFactory _connectionFactory;
    private readonly ILogger<DistributedCacheService> _logger;

    /// <summary>
    /// 初始化分布式缓存服务
    /// </summary>
    /// <param name="connectionFactory">Redis 连接工厂</param>
    /// <param name="logger">日志记录器</param>
    public DistributedCacheService(
        IRedisConnectionFactory connectionFactory,
        ILogger<DistributedCacheService> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 异步获取缓存值
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>缓存值，不存在时返回 null</returns>
    public async Task<T?> GetAsync<T>(string key, CacheServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("缓存键不能为空", nameof(key));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            var data = await database.StringGetAsync(key);

            if (!data.HasValue)
            {
                _logger.LogDebug("缓存未命中: {Key}", key);
                return default;
            }

            _logger.LogDebug("缓存命中: {Key}", key);
            return DeserializeValue<T>(data!, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取缓存失败: {Key}, 实例: {Instance}", key, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// 异步设置缓存值
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="options">缓存配置选项</param>
    /// <param name="expiry">过期时间，为空时使用 options.Expiry</param>
    public async Task SetAsync<T>(string key, T value, CacheServiceOptions options, TimeSpan? expiry = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("缓存键不能为空", nameof(key));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            var serializedData = SerializeValue(value, options);
            var effectiveExpiry = expiry ?? options.Expiry;

            await database.StringSetAsync(key, serializedData, effectiveExpiry);
            _logger.LogDebug("缓存设置成功: {Key}, 过期时间: {Expiry}", key, effectiveExpiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置缓存失败: {Key}, 实例: {Instance}", key, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// 异步删除缓存
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>是否成功删除</returns>
    public async Task<bool> RemoveAsync(string key, CacheServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("缓存键不能为空", nameof(key));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            var result = await database.KeyDeleteAsync(key);
            
            _logger.LogDebug("缓存删除: {Key}, 结果: {Result}", key, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除缓存失败: {Key}, 实例: {Instance}", key, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// 异步检查缓存是否存在
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>缓存是否存在</returns>
    public async Task<bool> ExistsAsync(string key, CacheServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("缓存键不能为空", nameof(key));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            var result = await database.KeyExistsAsync(key);
            
            _logger.LogDebug("缓存存在性检查: {Key}, 结果: {Result}", key, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查缓存存在性失败: {Key}, 实例: {Instance}", key, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// 异步设置缓存过期时间
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="expiry">过期时间</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>是否成功设置过期时间</returns>
    public async Task<bool> SetExpirationAsync(string key, TimeSpan expiry, CacheServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("缓存键不能为空", nameof(key));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            var result = await database.KeyExpireAsync(key, expiry);
            
            _logger.LogDebug("设置缓存过期时间: {Key}, 过期时间: {Expiry}, 结果: {Result}", key, expiry, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置缓存过期时间失败: {Key}, 实例: {Instance}", key, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// Hash 操作 - 异步获取多个字段
    /// </summary>
    /// <param name="key">Hash 键</param>
    /// <param name="fields">要获取的字段列表</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>字段值字典，Hash 不存在时返回 null</returns>
    public async Task<Dictionary<string, string>?> HashGetAsync(string key, IEnumerable<string> fields, CacheServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Hash 键不能为空", nameof(key));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        if (fields == null)
            throw new ArgumentNullException(nameof(fields));

        var fieldArray = fields.ToArray();
        if (fieldArray.Length == 0)
            return new Dictionary<string, string>();

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            var redisFields = fieldArray.Select(f => (RedisValue)f).ToArray();
            var values = await database.HashGetAsync(key, redisFields);

            // 检查 Hash 是否存在
            if (values.All(v => !v.HasValue))
            {
                var exists = await database.KeyExistsAsync(key);
                if (!exists)
                {
                    _logger.LogDebug("Hash 不存在: {Key}", key);
                    return null;
                }
            }

            var result = new Dictionary<string, string>();
            for (int i = 0; i < fieldArray.Length; i++)
            {
                if (values[i].HasValue)
                {
                    result[fieldArray[i]] = values[i].ToString()!;
                }
            }

            _logger.LogDebug("Hash 获取成功: {Key}, 字段数: {FieldCount}, 返回数: {ResultCount}", 
                key, fieldArray.Length, result.Count);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hash 获取失败: {Key}, 实例: {Instance}", key, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// Hash 操作 - 异步设置多个字段
    /// </summary>
    /// <param name="key">Hash 键</param>
    /// <param name="values">字段值字典</param>
    /// <param name="options">缓存配置选项</param>
    /// <param name="expiry">过期时间，为空时使用 options.Expiry</param>
    public async Task HashSetAsync(string key, Dictionary<string, string> values, CacheServiceOptions options, TimeSpan? expiry = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Hash 键不能为空", nameof(key));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        if (values == null || values.Count == 0)
            throw new ArgumentException("Hash 值不能为空", nameof(values));

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            var hashFields = values.Select(kvp => new HashEntry(kvp.Key, kvp.Value)).ToArray();
            
            await database.HashSetAsync(key, hashFields);

            // 设置过期时间
            var effectiveExpiry = expiry ?? options.Expiry;
            await database.KeyExpireAsync(key, effectiveExpiry);

            _logger.LogDebug("Hash 设置成功: {Key}, 字段数: {FieldCount}, 过期时间: {Expiry}", 
                key, values.Count, effectiveExpiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hash 设置失败: {Key}, 实例: {Instance}", key, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// Hash 操作 - 异步检查字段是否存在
    /// </summary>
    /// <param name="key">Hash 键</param>
    /// <param name="field">字段名</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>字段是否存在</returns>
    public async Task<bool> HashExistsAsync(string key, string field, CacheServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Hash 键不能为空", nameof(key));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(field))
            throw new ArgumentException("字段名不能为空", nameof(field));

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            var result = await database.HashExistsAsync(key, field);
            
            _logger.LogDebug("Hash 字段存在性检查: {Key}.{Field}, 结果: {Result}", key, field, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hash 字段存在性检查失败: {Key}.{Field}, 实例: {Instance}", 
                key, field, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// 原子性设置缓存（仅当键不存在时）
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="options">缓存配置选项</param>
    /// <param name="expiry">过期时间，为空时使用 options.Expiry</param>
    /// <returns>是否设置成功</returns>
    public async Task<bool> SetIfNotExistsAsync<T>(string key, T value, CacheServiceOptions options, TimeSpan? expiry = null)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("缓存键不能为空", nameof(key));
        if (options == null) throw new ArgumentNullException(nameof(options));

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            var serializedData = SerializeValue(value, options);
            var effectiveExpiry = expiry ?? options.Expiry;
            
            return await database.StringSetAsync(key, serializedData, effectiveExpiry, When.NotExists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SetIfNotExistsAsync 失败: {Key}, 实例: {Instance}", key, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// 获取或设置缓存值
    /// </summary>
    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, CacheServiceOptions options, TimeSpan? expiry = null)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("缓存键不能为空", nameof(key));
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        var value = await GetAsync<T>(key, options);
        if (value != null && !EqualityComparer<T>.Default.Equals(value, default(T)))
        {
            return value;
        }

        value = await factory();
        
        if (value != null)
        {
            await SetAsync(key, value, options, expiry);
        }
        
        return value;
    }

    /// <summary>
    /// 原子性递增
    /// </summary>
    public async Task<long> IncrementAsync(string key, long increment, CacheServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("缓存键不能为空", nameof(key));
        if (options == null) throw new ArgumentNullException(nameof(options));

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            return await database.StringIncrementAsync(key, increment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IncrementAsync 失败: {Key}, 实例: {Instance}", key, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// 原子性递减
    /// </summary>
    public async Task<long> DecrementAsync(string key, long decrement, CacheServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("缓存键不能为空", nameof(key));
        if (options == null) throw new ArgumentNullException(nameof(options));

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            return await database.StringDecrementAsync(key, decrement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DecrementAsync 失败: {Key}, 实例: {Instance}", key, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// 获取分布式锁
    /// </summary>
    public async Task<bool> AcquireLockAsync(string key, TimeSpan expirationTime, CacheServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("锁键不能为空", nameof(key));
        if (options == null) throw new ArgumentNullException(nameof(options));

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            return await database.StringSetAsync(key, "locked", expirationTime, When.NotExists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AcquireLockAsync 失败: {Key}, 实例: {Instance}", key, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// 释放分布式锁
    /// </summary>
    public async Task<bool> ReleaseLockAsync(string key, CacheServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("锁键不能为空", nameof(key));
        if (options == null) throw new ArgumentNullException(nameof(options));

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            return await database.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReleaseLockAsync 失败: {Key}, 实例: {Instance}", key, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// 检查是否持有分布式锁
    /// </summary>
    public async Task<bool> IsLockAcquiredAsync(string key, CacheServiceOptions options)
    {
        return await ExistsAsync(key, options);
    }

    /// <summary>
    /// 批量删除缓存
    /// </summary>
    public async Task<long> RemoveManyAsync(IEnumerable<string>? keys, CacheServiceOptions options)
    {
        if (keys == null || !keys.Any()) return 0;
        if (options == null) throw new ArgumentNullException(nameof(options));

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
            return await database.KeyDeleteAsync(redisKeys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RemoveManyAsync 失败, 实例: {Instance}", options.RedisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// 扫描匹配的键
    /// </summary>
    public async Task<IEnumerable<string>> ScanKeysAsync(string pattern, int pageSize, CacheServiceOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        try
        {
            var connection = string.IsNullOrWhiteSpace(options.RedisInstanceName) 
                ? _connectionFactory.GetConnection() 
                : _connectionFactory.GetConnection(options.RedisInstanceName);

            var keys = new List<string>();
            foreach (var endpoint in connection.GetEndPoints())
            {
                var server = connection.GetServer(endpoint);
                await Task.Run(() => 
                {
                    foreach (var key in server.Keys(pattern: pattern, pageSize: pageSize))
                    {
                        keys.Add(key.ToString());
                    }
                });
            }
            return keys.Distinct();
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "ScanKeysAsync 失败: {Pattern}, 实例: {Instance}", pattern, options.RedisInstanceName ?? "default");
             return Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// 获取二进制缓存值
    /// </summary>
    public async Task<byte[]?> GetBytesAsync(string key, CacheServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("缓存键不能为空", nameof(key));
        if (options == null) throw new ArgumentNullException(nameof(options));

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            var data = await database.StringGetAsync(key);
            return (byte[]?)data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetBytesAsync 失败: {Key}, 实例: {Instance}", key, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// 设置二进制缓存值
    /// </summary>
    public async Task<bool> SetBytesAsync(string key, byte[] value, TimeSpan? expiry, CacheServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("缓存键不能为空", nameof(key));
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            var effectiveExpiry = expiry ?? options.Expiry;
            return await database.StringSetAsync(key, value, effectiveExpiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SetBytesAsync 失败: {Key}, 实例: {Instance}", key, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// 设置Hash字段值
    /// </summary>
    public async Task<bool> HashSetAsync(string key, string field, string value, CacheServiceOptions options, TimeSpan? expiry = null)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Hash 键不能为空", nameof(key));
        if (string.IsNullOrWhiteSpace(field)) throw new ArgumentException("字段名不能为空", nameof(field));
        if (options == null) throw new ArgumentNullException(nameof(options));

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            var result = await database.HashSetAsync(key, field, value);
            
            var effectiveExpiry = expiry ?? options.Expiry;
            if (effectiveExpiry > TimeSpan.Zero)
            {
                await database.KeyExpireAsync(key, effectiveExpiry);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HashSetAsync 失败: {Key}.{Field}, 实例: {Instance}", key, field, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// 获取Hash字段值
    /// </summary>
    public async Task<string?> HashGetAsync(string key, string field, CacheServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Hash 键不能为空", nameof(key));
        if (string.IsNullOrWhiteSpace(field)) throw new ArgumentException("字段名不能为空", nameof(field));
        if (options == null) throw new ArgumentNullException(nameof(options));

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            var value = await database.HashGetAsync(key, field);
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HashGetAsync 失败: {Key}.{Field}, 实例: {Instance}", key, field, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// 删除Hash字段
    /// </summary>
    public async Task<bool> HashDeleteAsync(string key, string field, CacheServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Hash 键不能为空", nameof(key));
        if (string.IsNullOrWhiteSpace(field)) throw new ArgumentException("字段名不能为空", nameof(field));
        if (options == null) throw new ArgumentNullException(nameof(options));

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            return await database.HashDeleteAsync(key, field);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HashDeleteAsync 失败: {Key}.{Field}, 实例: {Instance}", key, field, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// 删除Hash多个字段
    /// </summary>
    public async Task<long> HashDeleteManyAsync(string key, IEnumerable<string> fields, CacheServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Hash 键不能为空", nameof(key));
        if (fields == null) throw new ArgumentNullException(nameof(fields));
        if (options == null) throw new ArgumentNullException(nameof(options));

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            var redisFields = fields.Select(f => (RedisValue)f).ToArray();
            return await database.HashDeleteAsync(key, redisFields);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HashDeleteManyAsync 失败: {Key}, 实例: {Instance}", key, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// 递增Hash字段值
    /// </summary>
    public async Task<long> HashIncrementAsync(string key, string field, long increment, CacheServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Hash 键不能为空", nameof(key));
        if (string.IsNullOrWhiteSpace(field)) throw new ArgumentException("字段名不能为空", nameof(field));
        if (options == null) throw new ArgumentNullException(nameof(options));

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            return await database.HashIncrementAsync(key, field, increment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HashIncrementAsync 失败: {Key}.{Field}, 实例: {Instance}", key, field, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// 递减Hash字段值
    /// </summary>
    public async Task<long> HashDecrementAsync(string key, string field, long decrement, CacheServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Hash 键不能为空", nameof(key));
        if (string.IsNullOrWhiteSpace(field)) throw new ArgumentException("字段名不能为空", nameof(field));
        if (options == null) throw new ArgumentNullException(nameof(options));

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            return await database.HashDecrementAsync(key, field, decrement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HashDecrementAsync 失败: {Key}.{Field}, 实例: {Instance}", key, field, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// 获取Hash字段数量
    /// </summary>
    public async Task<long> HashLengthAsync(string key, CacheServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Hash 键不能为空", nameof(key));
        if (options == null) throw new ArgumentNullException(nameof(options));

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            return await database.HashLengthAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HashLengthAsync 失败: {Key}, 实例: {Instance}", key, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// 获取Hash所有字段名
    /// </summary>
    public async Task<IEnumerable<string>> HashKeysAsync(string key, CacheServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Hash 键不能为空", nameof(key));
        if (options == null) throw new ArgumentNullException(nameof(options));

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            var keys = await database.HashKeysAsync(key);
            return keys.Select(k => k.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HashKeysAsync 失败: {Key}, 实例: {Instance}", key, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// 获取Hash所有值
    /// </summary>
    public async Task<IEnumerable<string>> HashValuesAsync(string key, CacheServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Hash 键不能为空", nameof(key));
        if (options == null) throw new ArgumentNullException(nameof(options));

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            var values = await database.HashValuesAsync(key);
            return values.Select(v => v.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HashValuesAsync 失败: {Key}, 实例: {Instance}", key, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// 批量递增Hash字段值
    /// </summary>
    public async Task HashIncrementManyAsync(string hashId, IEnumerable<KeyValuePair<string, long>> keyValuePairs, TimeSpan? expiry, CacheServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(hashId)) throw new ArgumentException("Hash 键不能为空", nameof(hashId));
        ArgumentNullException.ThrowIfNull(keyValuePairs);
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            var tasks = new List<Task>();
            foreach (var kvp in keyValuePairs)
            {
                tasks.Add(database.HashIncrementAsync(hashId, kvp.Key, kvp.Value));
            }
            await Task.WhenAll(tasks);
            
            var effectiveExpiry = expiry ?? options.Expiry;
            if (effectiveExpiry > TimeSpan.Zero)
            {
                await database.KeyExpireAsync(hashId, effectiveExpiry);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HashIncrementManyAsync 失败: {Key}, 实例: {Instance}", hashId, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    private IDatabase GetDatabase(string? redisInstanceName)
    {
        return string.IsNullOrWhiteSpace(redisInstanceName)
            ? _connectionFactory.GetDatabase()
            : _connectionFactory.GetDatabase(redisInstanceName);
    }

    /// <summary>
    /// 序列化值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="value">要序列化的值</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>序列化后的字节数组</returns>
    private byte[] SerializeValue<T>(T value, CacheServiceOptions options)
    {
        var data = options.EnableBinarySerialization
            ? MemoryPackCacheSerializer.Serialize(value)
            : JsonCacheSerializer.Serialize(value);

        if (!options.EnableCompression || data.Length <= 1024) return data;
        var originalSize = data.Length;
        var compressed = GZipCacheCompressor.Compress(data);

        _logger.LogDebug("数据已压缩，原始大小: {OriginalSize}, 压缩后大小: {CompressedSize}", 
            originalSize, compressed.Length);

        return compressed;

    }

    /// <summary>
    /// 反序列化值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="data">序列化的数据</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>反序列化后的值</returns>
    private T? DeserializeValue<T>(byte[]? data, CacheServiceOptions options)
    {
        if (data == null || data.Length == 0)
            return default;

        // 如果启用了压缩，先尝试解压缩
        if (options.EnableCompression)
        {
            try
            {
                data = GZipCacheCompressor.Decompress(data);
            }
            catch
            {
                // 如果解压缩失败，可能是未压缩的数据，继续使用原始数据
                _logger.LogDebug("数据解压缩失败，使用原始数据进行反序列化");
            }
        }

        return options.EnableBinarySerialization
            ? MemoryPackCacheSerializer.Deserialize<T>(data)
            : JsonCacheSerializer.Deserialize<T>(data);
    }
}
