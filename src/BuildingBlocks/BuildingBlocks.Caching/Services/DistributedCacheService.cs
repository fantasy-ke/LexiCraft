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
    /// 获取 Redis 数据库实例
    /// </summary>
    /// <param name="redisInstanceName">Redis 实例名称</param>
    /// <returns>Redis 数据库实例</returns>
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

        if (options.EnableCompression && data.Length > 1024)
        {
            var originalSize = data.Length;
            var compressed = GZipCacheCompressor.Compress(data);

            _logger.LogDebug("数据已压缩，原始大小: {OriginalSize}, 压缩后大小: {CompressedSize}", 
                originalSize, compressed.Length);

            return compressed;
        }

        return data;
    }

    /// <summary>
    /// 反序列化值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="data">序列化的数据</param>
    /// <param name="options">缓存配置选项</param>
    /// <returns>反序列化后的值</returns>
    private T? DeserializeValue<T>(byte[] data, CacheServiceOptions options)
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
