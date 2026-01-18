using BuildingBlocks.Caching.Abstractions;
using BuildingBlocks.Caching.Configuration;
using BuildingBlocks.Caching.Factories;
using BuildingBlocks.Caching.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BuildingBlocks.Caching.Services;

/// <summary>
/// 分布式缓存服务实现
/// </summary>
public class DistributedCacheService : IDistributedCacheService
{
    private readonly IRedisConnectionFactory _connectionFactory;
    private readonly ILogger<DistributedCacheService> _logger;
    private readonly ICacheSerializer _jsonSerializer;
    private readonly ICacheSerializer _binarySerializer;
    private readonly ICacheCompressor _compressor;
    private readonly CacheServiceOptions _defaultOptions;

    /// <summary>
    /// 初始化分布式缓存服务
    /// </summary>
    /// <param name="connectionFactory">Redis 连接工厂</param>
    /// <param name="options">缓存服务选项</param>
    /// <param name="jsonSerializer">JSON 序列化器</param>
    /// <param name="binarySerializer">二进制序列化器</param>
    /// <param name="compressor">压缩器</param>
    /// <param name="logger">日志记录器</param>
    public DistributedCacheService(
        IRedisConnectionFactory connectionFactory,
        IOptions<CacheServiceOptions> options,
        JsonCacheSerializer jsonSerializer,
        MemoryPackCacheSerializer binarySerializer,
        GZipCacheCompressor compressor,
        ILogger<DistributedCacheService> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _defaultOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
        _binarySerializer = binarySerializer ?? throw new ArgumentNullException(nameof(binarySerializer));
        _compressor = compressor ?? throw new ArgumentNullException(nameof(compressor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 异步获取缓存值
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="redisInstanceName">Redis 实例名称，为空时使用默认实例</param>
    /// <returns>缓存值，不存在时返回 null</returns>
    public async Task<T?> GetAsync<T>(string key, string? redisInstanceName = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("缓存键不能为空", nameof(key));

        try
        {
            var database = GetDatabase(redisInstanceName);
            var data = await database.StringGetAsync(key);

            if (!data.HasValue)
            {
                _logger.LogDebug("缓存未命中: {Key}", key);
                return default;
            }

            _logger.LogDebug("缓存命中: {Key}", key);
            return DeserializeValue<T>(data!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取缓存失败: {Key}, 实例: {Instance}", key, redisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// 异步设置缓存值
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="expiry">过期时间，为空时使用默认过期时间</param>
    /// <param name="redisInstanceName">Redis 实例名称，为空时使用默认实例</param>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, string? redisInstanceName = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("缓存键不能为空", nameof(key));

        try
        {
            var database = GetDatabase(redisInstanceName);
            var serializedData = SerializeValue(value);
            var effectiveExpiry = expiry ?? _defaultOptions.Expiry;

            await database.StringSetAsync(key, serializedData, effectiveExpiry);
            _logger.LogDebug("缓存设置成功: {Key}, 过期时间: {Expiry}", key, effectiveExpiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置缓存失败: {Key}, 实例: {Instance}", key, redisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// 异步删除缓存
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="redisInstanceName">Redis 实例名称，为空时使用默认实例</param>
    /// <returns>是否成功删除</returns>
    public async Task<bool> RemoveAsync(string key, string? redisInstanceName = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("缓存键不能为空", nameof(key));

        try
        {
            var database = GetDatabase(redisInstanceName);
            var result = await database.KeyDeleteAsync(key);
            
            _logger.LogDebug("缓存删除: {Key}, 结果: {Result}", key, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除缓存失败: {Key}, 实例: {Instance}", key, redisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// 异步检查缓存是否存在
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="redisInstanceName">Redis 实例名称，为空时使用默认实例</param>
    /// <returns>缓存是否存在</returns>
    public async Task<bool> ExistsAsync(string key, string? redisInstanceName = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("缓存键不能为空", nameof(key));

        try
        {
            var database = GetDatabase(redisInstanceName);
            var result = await database.KeyExistsAsync(key);
            
            _logger.LogDebug("缓存存在性检查: {Key}, 结果: {Result}", key, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查缓存存在性失败: {Key}, 实例: {Instance}", key, redisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// 异步设置缓存过期时间
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="expiry">过期时间</param>
    /// <param name="redisInstanceName">Redis 实例名称，为空时使用默认实例</param>
    /// <returns>是否成功设置过期时间</returns>
    public async Task<bool> SetExpirationAsync(string key, TimeSpan expiry, string? redisInstanceName = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("缓存键不能为空", nameof(key));

        try
        {
            var database = GetDatabase(redisInstanceName);
            var result = await database.KeyExpireAsync(key, expiry);
            
            _logger.LogDebug("设置缓存过期时间: {Key}, 过期时间: {Expiry}, 结果: {Result}", key, expiry, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置缓存过期时间失败: {Key}, 实例: {Instance}", key, redisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// Hash 操作 - 异步获取多个字段
    /// </summary>
    /// <param name="key">Hash 键</param>
    /// <param name="fields">要获取的字段列表</param>
    /// <param name="redisInstanceName">Redis 实例名称，为空时使用默认实例</param>
    /// <returns>字段值字典，Hash 不存在时返回 null</returns>
    public async Task<Dictionary<string, string>?> HashGetAsync(string key, IEnumerable<string> fields, string? redisInstanceName = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Hash 键不能为空", nameof(key));

        if (fields == null)
            throw new ArgumentNullException(nameof(fields));

        var fieldArray = fields.ToArray();
        if (fieldArray.Length == 0)
            return new Dictionary<string, string>();

        try
        {
            var database = GetDatabase(redisInstanceName);
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
            _logger.LogError(ex, "Hash 获取失败: {Key}, 实例: {Instance}", key, redisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// Hash 操作 - 异步设置多个字段
    /// </summary>
    /// <param name="key">Hash 键</param>
    /// <param name="values">字段值字典</param>
    /// <param name="expiry">过期时间，为空时使用默认过期时间</param>
    /// <param name="redisInstanceName">Redis 实例名称，为空时使用默认实例</param>
    public async Task HashSetAsync(string key, Dictionary<string, string> values, TimeSpan? expiry = null, string? redisInstanceName = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Hash 键不能为空", nameof(key));

        if (values == null || values.Count == 0)
            throw new ArgumentException("Hash 值不能为空", nameof(values));

        try
        {
            var database = GetDatabase(redisInstanceName);
            var hashFields = values.Select(kvp => new HashEntry(kvp.Key, kvp.Value)).ToArray();
            
            await database.HashSetAsync(key, hashFields);

            // 设置过期时间
            var effectiveExpiry = expiry ?? _defaultOptions.Expiry;
            await database.KeyExpireAsync(key, effectiveExpiry);

            _logger.LogDebug("Hash 设置成功: {Key}, 字段数: {FieldCount}, 过期时间: {Expiry}", 
                key, values.Count, effectiveExpiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hash 设置失败: {Key}, 实例: {Instance}", key, redisInstanceName ?? "default");
            throw;
        }
    }

    /// <summary>
    /// Hash 操作 - 异步检查字段是否存在
    /// </summary>
    /// <param name="key">Hash 键</param>
    /// <param name="field">字段名</param>
    /// <param name="redisInstanceName">Redis 实例名称，为空时使用默认实例</param>
    /// <returns>字段是否存在</returns>
    public async Task<bool> HashExistsAsync(string key, string field, string? redisInstanceName = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Hash 键不能为空", nameof(key));

        if (string.IsNullOrWhiteSpace(field))
            throw new ArgumentException("字段名不能为空", nameof(field));

        try
        {
            var database = GetDatabase(redisInstanceName);
            var result = await database.HashExistsAsync(key, field);
            
            _logger.LogDebug("Hash 字段存在性检查: {Key}.{Field}, 结果: {Result}", key, field, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hash 字段存在性检查失败: {Key}.{Field}, 实例: {Instance}", 
                key, field, redisInstanceName ?? "default");
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
    /// <returns>序列化后的字节数组</returns>
    private byte[] SerializeValue<T>(T value)
    {
        // 选择序列化器
        var serializer = _defaultOptions.EnableBinarySerialization ? _binarySerializer : _jsonSerializer;
        var data = serializer.Serialize(value);

        // 如果启用压缩且数据大小超过阈值，则进行压缩
        if (_defaultOptions.EnableCompression && data.Length > 1024) // 1KB 阈值
        {
            data = _compressor.Compress(data);
            _logger.LogDebug("数据已压缩，原始大小: {OriginalSize}, 压缩后大小: {CompressedSize}", 
                serializer.Serialize(value).Length, data.Length);
        }

        return data;
    }

    /// <summary>
    /// 反序列化值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="data">序列化的数据</param>
    /// <returns>反序列化后的值</returns>
    private T? DeserializeValue<T>(byte[] data)
    {
        if (data == null || data.Length == 0)
            return default;

        // 如果启用了压缩，先尝试解压缩
        if (_defaultOptions.EnableCompression)
        {
            try
            {
                data = _compressor.Decompress(data);
            }
            catch
            {
                // 如果解压缩失败，可能是未压缩的数据，继续使用原始数据
                _logger.LogDebug("数据解压缩失败，使用原始数据进行反序列化");
            }
        }

        // 选择序列化器进行反序列化
        var serializer = _defaultOptions.EnableBinarySerialization ? _binarySerializer : _jsonSerializer;
        return serializer.Deserialize<T>(data);
    }
}