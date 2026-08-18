using BuildingBlocks.Caching.Internal;
using BuildingBlocks.Caching.Options;
using BuildingBlocks.Caching.Redis.Connections;
using BuildingBlocks.Caching.Redis.Serialization;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BuildingBlocks.Caching.Redis;

/// <summary>
///     Redis 数据访问实现，仅作为 <see cref="Services.CacheService"/> 的内部依赖。
/// </summary>
/// <remarks>
///     本类型只做 Redis I/O、序列化与压缩，不实现两级缓存编排、防击穿锁和错误降级；
///     所有方法在 Redis 不可用或序列化失败时记录日志后原样抛出，由上层根据
///     <see cref="CacheServiceOptions.HideErrors"/> 决定是否降级。
///     异步等待统一使用 <c>Task.WaitAsync(CancellationToken)</c>：取消只让调用方的等待提前结束，
///     已发出的 Redis 命令仍可能在服务端完成。
/// </remarks>
/// <param name="connectionFactory">按命名实例解析共享 Redis 连接的工厂。</param>
/// <param name="logger">用于记录命中、未命中及依赖失败的日志记录器。</param>
/// <exception cref="ArgumentNullException"><paramref name="connectionFactory"/> 或 <paramref name="logger"/> 为 <see langword="null"/> 时抛出。</exception>
internal sealed class RedisCacheStore(
    IRedisConnectionFactory connectionFactory,
    ILogger<RedisCacheStore> logger)
    : IRedisCacheStore
{
    private readonly IRedisConnectionFactory _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    private readonly ILogger<RedisCacheStore> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <summary>
    ///     按命名实例解析 Redis 数据库；空值或空白值使用默认实例。
    /// </summary>
    private IDatabase GetDatabase(string? redisInstanceName)
    {
        return string.IsNullOrWhiteSpace(redisInstanceName)
            ? _connectionFactory.GetDatabase()
            : _connectionFactory.GetDatabase(redisInstanceName);
    }

    /// <summary>
    ///     按选项选择 MemoryPack 或 JSON 序列化，并在启用压缩且序列化结果超过 1024 字节时应用 GZip。
    /// </summary>
    /// <remarks>
    ///     压缩阈值是硬编码的 1024 字节，未在结果中写入任何压缩标记；
    ///     读取端只能通过尝试解压来判断，因此同一键的读写必须使用一致的序列化和压缩选项。
    /// </remarks>
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

    /// <summary>
    ///     按选项尝试 GZip 解压后使用 MemoryPack 或 JSON 反序列化 Redis 原始字节。
    /// </summary>
    /// <remarks>
    ///     启用压缩时解压失败会被吞掉并按未压缩数据继续处理，以兼容阈值以下写入的短值；
    ///     但反序列化失败会以 <see cref="InvalidOperationException"/> 抛出。
    ///     切换 <see cref="CacheServiceOptions.EnableBinarySerialization"/> 或修改 MemoryPack 契约会造成
    ///     旧缓存值无法读取，属于二进制兼容风险，必须配合键名或版本前缀变更。
    /// </remarks>
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
