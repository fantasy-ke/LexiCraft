using BuildingBlocks.Caching.Internal;
using BuildingBlocks.Caching.Options;
using BuildingBlocks.Caching.Redis.Connections;
using BuildingBlocks.Caching.Redis.Serialization;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BuildingBlocks.Caching.Redis;

/// <summary>
///     Redis 数据访问实现，仅作为 <see cref="CacheService"/> 的内部依赖。
/// </summary>
internal sealed class RedisCacheStore : IRedisCacheStore
{
    private readonly IRedisConnectionFactory _connectionFactory;
    private readonly ILogger<RedisCacheStore> _logger;

    public RedisCacheStore(
        IRedisConnectionFactory connectionFactory,
        ILogger<RedisCacheStore> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CacheReadResult<T>> GetAsync<T>(
        string key,
        CacheServiceOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            var data = await GetDatabase(options.RedisInstanceName)
                .StringGetAsync(key)
                .WaitAsync(cancellationToken);

            if (!data.HasValue)
            {
                _logger.LogDebug("缓存未命中: {Key}", key);
                return CacheReadResult<T>.Miss;
            }

            _logger.LogDebug("缓存命中: {Key}", key);
            return CacheReadResult<T>.Hit(DeserializeValue<T>(data!, options));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取缓存失败: {Key}, 实例: {Instance}", key, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        CacheServiceOptions options,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            var effectiveExpiry = expiry ?? options.Expiry;
            await GetDatabase(options.RedisInstanceName)
                .StringSetAsync(key, SerializeValue(value, options), effectiveExpiry)
                .WaitAsync(cancellationToken);

            _logger.LogDebug("缓存设置成功: {Key}, 过期时间: {Expiry}", key, effectiveExpiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置缓存失败: {Key}, 实例: {Instance}", key, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    public async Task<bool> RemoveAsync(
        string key,
        CacheServiceOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            var removed = await GetDatabase(options.RedisInstanceName)
                .KeyDeleteAsync(key)
                .WaitAsync(cancellationToken);
            _logger.LogDebug("缓存删除完成: {Key}, 已删除: {Removed}", key, removed);
            return removed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除缓存失败: {Key}, 实例: {Instance}", key, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    public async Task<bool> ExistsAsync(
        string key,
        CacheServiceOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            return await GetDatabase(options.RedisInstanceName)
                .KeyExistsAsync(key)
                .WaitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查缓存是否存在失败: {Key}, 实例: {Instance}", key, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    public async Task<bool> SetExpirationAsync(
        string key,
        TimeSpan expiry,
        CacheServiceOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            return await GetDatabase(options.RedisInstanceName)
                .KeyExpireAsync(key, expiry)
                .WaitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置缓存过期时间失败: {Key}, 实例: {Instance}", key,
                options.RedisInstanceName ?? "default");
            throw;
        }
    }

    public async Task<Dictionary<string, string>?> HashGetAsync(
        string key,
        IEnumerable<string> fields,
        CacheServiceOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(options);

        var fieldArray = fields.ToArray();
        if (fieldArray.Length == 0)
            return new Dictionary<string, string>();

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            var redisFields = fieldArray.Select(field => (RedisValue)field).ToArray();
            var values = await database.HashGetAsync(key, redisFields).WaitAsync(cancellationToken);

            if (values.All(value => !value.HasValue) &&
                !await database.KeyExistsAsync(key).WaitAsync(cancellationToken))
            {
                _logger.LogDebug("Hash 缓存不存在: {Key}", key);
                return null;
            }

            var result = new Dictionary<string, string>(fieldArray.Length, StringComparer.Ordinal);
            for (var index = 0; index < fieldArray.Length; index++)
            {
                if (values[index].HasValue)
                    result[fieldArray[index]] = values[index].ToString();
            }

            _logger.LogDebug("Hash 缓存读取成功: {Key}, 请求字段数: {FieldCount}, 返回字段数: {ResultCount}",
                key, fieldArray.Length, result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hash 缓存读取失败: {Key}, 实例: {Instance}", key, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    public async Task HashSetAsync(
        string key,
        Dictionary<string, string> values,
        CacheServiceOptions options,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(options);
        if (values.Count == 0)
            throw new ArgumentException("Hash 缓存值不能为空", nameof(values));

        try
        {
            var database = GetDatabase(options.RedisInstanceName);
            var entries = values.Select(pair => new HashEntry(pair.Key, pair.Value)).ToArray();
            var effectiveExpiry = expiry ?? options.Expiry;

            var transaction = database.CreateTransaction();
            var hashSetTask = transaction.HashSetAsync(key, entries);
            var expiryTask = transaction.KeyExpireAsync(key, effectiveExpiry);

            var committed = await transaction.ExecuteAsync().WaitAsync(cancellationToken);
            if (!committed)
                throw new InvalidOperationException($"Hash 缓存事务提交失败: {key}");

            await hashSetTask.WaitAsync(cancellationToken);
            if (!await expiryTask.WaitAsync(cancellationToken))
                throw new InvalidOperationException($"Hash 缓存过期时间设置失败: {key}");

            _logger.LogDebug("Hash 缓存设置成功: {Key}, 字段数: {FieldCount}, 过期时间: {Expiry}",
                key, values.Count, effectiveExpiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hash 缓存设置失败: {Key}, 实例: {Instance}", key, options.RedisInstanceName ?? "default");
            throw;
        }
    }

    private IDatabase GetDatabase(string? redisInstanceName)
    {
        return string.IsNullOrWhiteSpace(redisInstanceName)
            ? _connectionFactory.GetDatabase()
            : _connectionFactory.GetDatabase(redisInstanceName);
    }

    private byte[] SerializeValue<T>(T value, CacheServiceOptions options)
    {
        var data = options.EnableBinarySerialization
            ? MemoryPackCacheSerializer.Serialize(value)
            : JsonCacheSerializer.Serialize(value);

        if (!options.EnableCompression || data.Length <= 1024)
            return data;

        var compressed = GZipCacheCompressor.Compress(data);
        _logger.LogDebug("缓存数据已压缩: {OriginalSize}, 压缩后: {CompressedSize}",
            data.Length, compressed.Length);
        return compressed;
    }

    private T? DeserializeValue<T>(byte[]? data, CacheServiceOptions options)
    {
        if (data == null || data.Length == 0)
            return default;

        if (options.EnableCompression)
        {
            try
            {
                data = GZipCacheCompressor.Decompress(data);
            }
            catch
            {
                _logger.LogDebug("数据不是 GZip 格式，将按原始数据反序列化");
            }
        }

        return options.EnableBinarySerialization
            ? MemoryPackCacheSerializer.Deserialize<T>(data)
            : JsonCacheSerializer.Deserialize<T>(data);
    }
}
