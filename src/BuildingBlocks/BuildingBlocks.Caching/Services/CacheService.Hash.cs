using BuildingBlocks.Caching.Locking;
using BuildingBlocks.Caching.Options;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Caching.Services;

internal sealed partial class CacheService
{
    private const string HashTimestampField = "cache_timestamp";

    /// <inheritdoc />
    public async Task<TResult?> GetOrSetHashAsync<TResult>(
        string hashKey,
        IEnumerable<string> queryFields,
        Func<Dictionary<string, string>, TResult?> parseFromHash,
        Func<Task<Dictionary<string, string>>> buildFullCache,
        Action<CacheServiceOptions>? configure = null,
        CancellationToken cancellationToken = default)
    {
        var options = GetEffectiveOptions(configure);

        try
        {
            var queryFieldsList = queryFields
                .Append(HashTimestampField)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            // 首先尝试从缓存获取 Hash 数据
            if (options.UseDistributed)
            {
                var hashData = await _distributedCacheService.HashGetAsync(hashKey, queryFieldsList, options, cancellationToken);
                if (hashData != null && hashData.Count > 0)
                {
                    // 检查时间戳验证缓存是否过期
                    if (IsHashCacheValid(hashData, options))
                    {
                        _logger.LogDebug("Hash 缓存命中: {HashKey}", hashKey);
                        return parseFromHash(hashData);
                    }

                    _logger.LogDebug("Hash 缓存已过期: {HashKey}", hashKey);
                }
            }

            // Hash 缓存未命中或已过期，需要重建
            if (options.EnableLock)
                return await GetOrSetHashWithLockAsync(hashKey, queryFieldsList, parseFromHash, buildFullCache, options,
                    cancellationToken);

            return await GetOrSetHashWithoutLockAsync(hashKey, queryFieldsList, parseFromHash, buildFullCache, options,
                cancellationToken);
        }
        catch (Exception ex)
        {
            return await HandleException<TResult>(ex, options, hashKey, "GetOrSetHashAsync");
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, string>?> GetOrSetHashAsync(
        string hashKey,
        IEnumerable<string> queryFields,
        Func<Task<Dictionary<string, string>>> buildFullCache,
        Action<CacheServiceOptions>? configure = null,
        CancellationToken cancellationToken = default)
    {
        return await GetOrSetHashAsync<Dictionary<string, string>>(
            hashKey,
            queryFields,
            hashData => hashData,
            buildFullCache,
            configure,
            cancellationToken);
    }

    /// <summary>
    ///     获取分布式缓存 Hash 过期时间
    ///     需求 2.1: 统一过期时间 - 为所有缓存项应用默认 TTL
    ///     需求 2.5: Hash 缓存 TTL 调整 - 基于 Hash 内容调整过期时间
    /// </summary>
    /// <param name="options">配置选项</param>
    /// <param name="hashData">Hash 缓存数据</param>
    /// <returns>分布式缓存 Hash 过期时间</returns>
    private TimeSpan GetDistributedCacheHashExpiry(CacheServiceOptions options, Dictionary<string, string> hashData)
    {
        return AdjustHashExpiry(options, hashData);
    }

    /// <summary>
    ///     使用分布式锁的 Hash GetOrSet 操作
    /// </summary>
    private async Task<TResult?> GetOrSetHashWithLockAsync<TResult>(
        string hashKey,
        List<string> queryFields,
        Func<Dictionary<string, string>, TResult?> parseFromHash,
        Func<Task<Dictionary<string, string>>> buildFullCache,
        CacheServiceOptions options,
        CancellationToken cancellationToken)
    {
        var lockKey = hashKey;
        IDistributedLock? distributedLock = null;

        try
        {
            // 尝试获取分布式锁
            distributedLock = await _lockProvider.TryAcquireLockAsync(
                lockKey,
                options.LockTimeout,
                options.LockAcquireTimeout,
                options.RedisInstanceName,
                cancellationToken);

            if (distributedLock != null)
            {
                _logger.LogDebug("获取 Hash 分布式锁成功: {LockKey}", lockKey);

                // 再次检查缓存（双重检查锁定模式）
                if (options.UseDistributed)
                {
                    var hashData = await _distributedCacheService.HashGetAsync(hashKey, queryFields, options, cancellationToken);
                    if (hashData != null && hashData.Count > 0 && IsHashCacheValid(hashData, options))
                    {
                        _logger.LogDebug("Hash 双重检查缓存命中: {HashKey}", hashKey);
                        return parseFromHash(hashData);
                    }
                }

                // 调用工厂方法构建完整缓存
                var fullCacheData = await buildFullCache();
                if (fullCacheData != null && fullCacheData.Count > 0)
                {
                    // 添加时间戳
                    var cacheDataWithTimestamp = new Dictionary<string, string>(fullCacheData)
                    {
                        [HashTimestampField] = DateTimeOffset.UtcNow.ToString("O")
                    };

                    // 调整 Hash 缓存 TTL
                    var expiry = GetDistributedCacheHashExpiry(options, cacheDataWithTimestamp);

                    // 设置 Hash 缓存
                    if (options.UseDistributed)
                    {
                        await _distributedCacheService.HashSetAsync(hashKey, cacheDataWithTimestamp, options, expiry, cancellationToken);
                        _logger.LogDebug("通过工厂方法构建并设置 Hash 缓存: {HashKey}, TTL: {Expiry}", hashKey, expiry);
                    }

                    // 解析并返回结果
                    return parseFromHash(cacheDataWithTimestamp);
                }

                return default;
            }
            else
            {
                _logger.LogWarning("获取 Hash 分布式锁失败: {LockKey}", lockKey);

                // 锁获取失败，执行降级策略
                return await HandleHashLockFailure<TResult>(hashKey, queryFields, parseFromHash, buildFullCache,
                    options, cancellationToken);
            }
        }
        finally
        {
            if (distributedLock != null)
            {
                await distributedLock.DisposeAsync();
                _logger.LogDebug("释放 Hash 分布式锁: {LockKey}", lockKey);
            }
        }
    }

    /// <summary>
    ///     不使用分布式锁的 Hash GetOrSet 操作
    /// </summary>
    private async Task<TResult?> GetOrSetHashWithoutLockAsync<TResult>(
        string hashKey,
        List<string> queryFields,
        Func<Dictionary<string, string>, TResult?> parseFromHash,
        Func<Task<Dictionary<string, string>>> buildFullCache,
        CacheServiceOptions options,
        CancellationToken cancellationToken)
    {
        // 直接调用工厂方法构建完整缓存
        var fullCacheData = await buildFullCache();
        if (fullCacheData != null && fullCacheData.Count > 0)
        {
            // 添加时间戳
            var cacheDataWithTimestamp = new Dictionary<string, string>(fullCacheData)
            {
                [HashTimestampField] = DateTimeOffset.UtcNow.ToString("O")
            };

            // 调整 Hash 缓存 TTL
            var expiry = GetDistributedCacheHashExpiry(options, cacheDataWithTimestamp);

            // 设置 Hash 缓存
            if (options.UseDistributed)
            {
                await _distributedCacheService.HashSetAsync(hashKey, cacheDataWithTimestamp, options, expiry, cancellationToken);
                _logger.LogDebug("通过工厂方法构建并设置 Hash 缓存 (无锁): {HashKey}, TTL: {Expiry}", hashKey, expiry);
            }

            // 解析并返回结果
            return parseFromHash(cacheDataWithTimestamp);
        }

        return default;
    }

    /// <summary>
    ///     检查 Hash 缓存是否有效（基于时间戳）
    /// </summary>
    private bool IsHashCacheValid(Dictionary<string, string> hashData, CacheServiceOptions options)
    {
        if (!hashData.TryGetValue(HashTimestampField, out var timestampStr))
            // 没有时间戳，认为缓存有效
            return true;

        if (DateTimeOffset.TryParse(timestampStr, out var timestamp))
        {
            var age = DateTimeOffset.UtcNow - timestamp;
            var isValid = age < options.Expiry;

            if (!isValid) _logger.LogDebug("Hash 缓存已过期: 年龄 {Age}, 最大年龄 {MaxAge}", age, options.Expiry);

            return isValid;
        }

        // 时间戳格式无效，认为缓存有效
        return true;
    }

    /// <summary>
    ///     调整 Hash 缓存过期时间
    ///     需求 2.1: 统一过期时间 - 为所有缓存项应用默认 TTL
    ///     需求 2.5: Hash 缓存 TTL 调整 - 基于 Hash 内容调整过期时间
    /// </summary>
    /// <param name="options">配置选项</param>
    /// <param name="hashData">Hash 缓存数据</param>
    /// <returns>调整后的过期时间</returns>
    private TimeSpan AdjustHashExpiry(CacheServiceOptions options, Dictionary<string, string> hashData)
    {
        // 需求 2.1: 使用全局缓存过期时间作为基础 TTL
        var expiry = options.Expiry;

        // 需求 2.5: 如果提供了 Hash 缓存 TTL 调整委托，则调用委托函数
        if (options.AdjustExpiryForHash != null)
            try
            {
                var adjustedExpiry = options.AdjustExpiryForHash(expiry, hashData);
                if (adjustedExpiry <= TimeSpan.Zero)
                {
                    _logger.LogWarning("Adjusted hash cache expiry {AdjustedExpiry} is invalid; using {DefaultExpiry}.",
                        adjustedExpiry, expiry);
                    return expiry;
                }

                _logger.LogDebug("动态调整 Hash 缓存过期时间: 原始={OriginalExpiry}, 调整后={AdjustedExpiry}, Hash字段数={FieldCount}",
                    options.Expiry, adjustedExpiry, hashData.Count);
                return adjustedExpiry;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "动态调整 Hash 缓存过期时间失败，使用默认过期时间: {DefaultExpiry}, Hash字段数: {FieldCount}",
                    options.Expiry, hashData.Count);
            }

        return expiry;
    }

    /// <summary>
    ///     处理 Hash 锁获取失败的降级策略
    /// </summary>
    private async Task<TResult?> HandleHashLockFailure<TResult>(
        string hashKey,
        List<string> queryFields,
        Func<Dictionary<string, string>, TResult?> parseFromHash,
        Func<Task<Dictionary<string, string>>> buildFullCache,
        CacheServiceOptions options,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning("Hash 分布式锁获取失败: {HashKey}", hashKey);

        // 需求 4.4: 工厂方法降级策略
        if (options.FallbackToFactory)
            try
            {
                _logger.LogDebug("Hash 锁获取失败，回退到工厂方法: {HashKey}", hashKey);
                var fullCacheData = await buildFullCache();

                if (fullCacheData != null && fullCacheData.Count > 0)
                    // 尝试设置缓存（可能会失败，但不影响返回值）
                    try
                    {
                        var cacheDataWithTimestamp = new Dictionary<string, string>(fullCacheData)
                        {
                            [HashTimestampField] = DateTimeOffset.UtcNow.ToString("O")
                        };

                        var expiry = GetDistributedCacheHashExpiry(options, cacheDataWithTimestamp);

                        if (options.UseDistributed)
                            await _distributedCacheService.HashSetAsync(hashKey, cacheDataWithTimestamp, options,
                                expiry);

                        return parseFromHash(cacheDataWithTimestamp);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Hash 锁失败降级时设置缓存失败: {HashKey}", hashKey);
                        return parseFromHash(fullCacheData);
                    }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hash 工厂方法降级策略执行失败: {HashKey}", hashKey);
                // 如果工厂方法失败，继续尝试其他降级策略
            }

        // 尝试其他降级策略
        var fallbackResult = await ExecuteFallbackStrategy<TResult>(hashKey, "HashLockFailure", options);
        if (fallbackResult.HasValue) return fallbackResult.Value;

        _logger.LogWarning("Hash 锁获取失败且无可用降级策略: {HashKey}", hashKey);
        return default;
    }
}
