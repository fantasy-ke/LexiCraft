using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildingBlocks.Caching.Abstractions;
using BuildingBlocks.Caching.Configuration;
using BuildingBlocks.Caching.DistributedLock;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Caching.Services;

/// <summary>
/// 缓存服务实现，提供高级缓存操作
/// </summary>
public class CacheService : ICacheService
{
    private readonly IDistributedCacheService _distributedCacheService;
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedLockProvider _lockProvider;
    private readonly ILogger<CacheService> _logger;
    private readonly CacheServiceOptions _defaultOptions;

    /// <summary>
    /// 初始化缓存服务
    /// </summary>
    /// <param name="distributedCacheService">分布式缓存服务</param>
    /// <param name="memoryCache">内存缓存</param>
    /// <param name="lockProvider">分布式锁提供者</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="options">默认配置选项</param>
    public CacheService(
        IDistributedCacheService distributedCacheService,
        IMemoryCache memoryCache,
        IDistributedLockProvider lockProvider,
        ILogger<CacheService> logger,
        IOptions<CacheServiceOptions> options)
    {
        _distributedCacheService = distributedCacheService ?? throw new ArgumentNullException(nameof(distributedCacheService));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _lockProvider = lockProvider ?? throw new ArgumentNullException(nameof(lockProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, Action<CacheServiceOptions>? configure = null, CancellationToken cancellationToken = default)
    {
        var options = GetEffectiveOptions(configure);
        
        try
        {
            // 混合缓存模式：优先从本地缓存读取
            if (options.UseLocal)
            {
                var localKey = GetLocalCacheKey(key);
                if (_memoryCache.TryGetValue(localKey, out T? localValue))
                {
                    _logger.LogDebug("缓存命中 (本地): {Key}", key);
                    return localValue;
                }
            }

            // 从分布式缓存读取
            if (options.UseDistributed)
            {
                var distributedValue = await _distributedCacheService.GetAsync<T>(key, options.RedisInstanceName);
                if (distributedValue != null)
                {
                    _logger.LogDebug("缓存命中 (分布式): {Key}", key);
                    
                    // 如果启用本地缓存，将分布式缓存的值同步到本地缓存
                    if (options.UseLocal)
                    {
                        var localExpiry = GetLocalExpiry(options);
                        var localKey = GetLocalCacheKey(key);
                        _memoryCache.Set(localKey, distributedValue, localExpiry);
                        _logger.LogDebug("同步到本地缓存: {Key}", key);
                    }
                    
                    return distributedValue;
                }
            }

            _logger.LogDebug("缓存未命中: {Key}", key);
            return default(T);
        }
        catch (Exception ex)
        {
            return await HandleException<T>(ex, options, key, "GetAsync");
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, Action<CacheServiceOptions>? configure = null, CancellationToken cancellationToken = default)
    {
        var options = GetEffectiveOptions(configure);
        
        try
        {
            // 调整 TTL
            var expiry = GetDistributedCacheExpiry(options, value);
            
            // 设置分布式缓存
            if (options.UseDistributed)
            {
                await _distributedCacheService.SetAsync(key, value, expiry, options.RedisInstanceName);
                _logger.LogDebug("设置分布式缓存: {Key}, TTL: {Expiry}", key, expiry);
            }

            // 设置本地缓存
            if (options.UseLocal)
            {
                var localExpiry = GetLocalExpiry(options);
                var localKey = GetLocalCacheKey(key);
                _memoryCache.Set(localKey, value, localExpiry);
                _logger.LogDebug("设置本地缓存: {Key}, TTL: {LocalExpiry}", key, localExpiry);
            }
        }
        catch (Exception ex)
        {
            await HandleException<object>(ex, options, key, "SetAsync");
        }
    }

    /// <inheritdoc />
    public async Task<bool> RemoveAsync(string key, Action<CacheServiceOptions>? configure = null, CancellationToken cancellationToken = default)
    {
        var options = GetEffectiveOptions(configure);
        var success = true;
        
        try
        {
            // 删除分布式缓存
            if (options.UseDistributed)
            {
                var distributedResult = await _distributedCacheService.RemoveAsync(key, options.RedisInstanceName);
                success = success && distributedResult;
                _logger.LogDebug("删除分布式缓存: {Key}, 结果: {Success}", key, distributedResult);
            }

            // 删除本地缓存
            if (options.UseLocal)
            {
                var localKey = GetLocalCacheKey(key);
                _memoryCache.Remove(localKey);
                _logger.LogDebug("删除本地缓存: {Key}", key);
            }

            return success;
        }
        catch (Exception ex)
        {
            await HandleException<object>(ex, options, key, "RemoveAsync");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, Action<CacheServiceOptions>? configure = null, CancellationToken cancellationToken = default)
    {
        var options = GetEffectiveOptions(configure);
        
        try
        {
            // 检查本地缓存
            if (options.UseLocal)
            {
                var localKey = GetLocalCacheKey(key);
                if (_memoryCache.TryGetValue(localKey, out _))
                {
                    _logger.LogDebug("本地缓存存在: {Key}", key);
                    return true;
                }
            }

            // 检查分布式缓存
            if (options.UseDistributed)
            {
                var exists = await _distributedCacheService.ExistsAsync(key, options.RedisInstanceName);
                _logger.LogDebug("分布式缓存存在检查: {Key}, 结果: {Exists}", key, exists);
                return exists;
            }

            return false;
        }
        catch (Exception ex)
        {
            await HandleException<object>(ex, options, key, "ExistsAsync");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SetExpirationAsync(string key, TimeSpan expirationTime, Action<CacheServiceOptions>? configure = null, CancellationToken cancellationToken = default)
    {
        var options = GetEffectiveOptions(configure);
        
        try
        {
            // 只有分布式缓存支持设置过期时间
            if (options.UseDistributed)
            {
                var result = await _distributedCacheService.SetExpirationAsync(key, expirationTime, options.RedisInstanceName);
                _logger.LogDebug("设置分布式缓存过期时间: {Key}, 过期时间: {Expiration}, 结果: {Success}", key, expirationTime, result);
                return result;
            }

            _logger.LogWarning("未启用分布式缓存，无法设置过期时间: {Key}", key);
            return false;
        }
        catch (Exception ex)
        {
            await HandleException<object>(ex, options, key, "SetExpirationAsync");
            return false;
        }
    }

    /// <summary>
    /// 获取有效的配置选项
    /// </summary>
    /// <param name="configure">配置委托</param>
    /// <returns>有效的配置选项</returns>
    private CacheServiceOptions GetEffectiveOptions(Action<CacheServiceOptions>? configure)
    {
        var options = new CacheServiceOptions
        {
            UseDistributed = _defaultOptions.UseDistributed,
            UseLocal = _defaultOptions.UseLocal,
            Expiry = _defaultOptions.Expiry,
            LocalExpiry = _defaultOptions.LocalExpiry,
            HideErrors = _defaultOptions.HideErrors,
            EnableCompression = _defaultOptions.EnableCompression,
            EnableBinarySerialization = _defaultOptions.EnableBinarySerialization,
            EnableLock = _defaultOptions.EnableLock,
            LockTimeout = _defaultOptions.LockTimeout,
            LockAcquireTimeout = _defaultOptions.LockAcquireTimeout,
            FallbackToFactory = _defaultOptions.FallbackToFactory,
            FallbackToDefault = _defaultOptions.FallbackToDefault,
            DefaultValue = _defaultOptions.DefaultValue,
            FallbackFunction = _defaultOptions.FallbackFunction,
            OnError = _defaultOptions.OnError,
            AdjustExpiryForHash = _defaultOptions.AdjustExpiryForHash,
            AdjustExpiryForValue = _defaultOptions.AdjustExpiryForValue,
            RedisInstanceName = _defaultOptions.RedisInstanceName
        };

        configure?.Invoke(options);
        
        // 应用 TTL 继承和覆盖逻辑
        ApplyTtlInheritanceRules(options);
        
        return options;
    }

    /// <summary>
    /// 应用 TTL 继承和覆盖逻辑
    /// 需求 2.1: 统一过期时间 - 为所有缓存项应用默认 TTL
    /// 需求 2.2: 本地缓存独立过期时间 - 本地缓存使用独立的 TTL
    /// 需求 2.3: TTL 继承 - 未设置本地缓存过期时间时继承统一过期时间
    /// </summary>
    /// <param name="options">配置选项</param>
    private void ApplyTtlInheritanceRules(CacheServiceOptions options)
    {
        // 需求 2.1: 确保全局过期时间有效（不能为零或负数）
        if (options.Expiry <= TimeSpan.Zero)
        {
            _logger.LogWarning("全局过期时间无效: {Expiry}，使用默认值: {DefaultExpiry}", 
                options.Expiry, _defaultOptions.Expiry);
            options.Expiry = _defaultOptions.Expiry;
        }

        // 需求 2.2 & 2.3: 处理本地缓存过期时间的继承逻辑
        if (options.UseLocal)
        {
            if (options.LocalExpiry.HasValue)
            {
                // 需求 2.2: 如果设置了本地缓存独立过期时间，验证其有效性
                if (options.LocalExpiry.Value <= TimeSpan.Zero)
                {
                    _logger.LogWarning("本地缓存过期时间无效: {LocalExpiry}，继承全局过期时间: {GlobalExpiry}", 
                        options.LocalExpiry.Value, options.Expiry);
                    options.LocalExpiry = null; // 重置为 null，让其继承全局过期时间
                }
                else
                {
                    _logger.LogDebug("使用本地缓存独立过期时间: {LocalExpiry}，全局过期时间: {GlobalExpiry}", 
                        options.LocalExpiry.Value, options.Expiry);
                }
            }
            else
            {
                // 需求 2.3: 未设置本地缓存过期时间时，继承全局过期时间
                _logger.LogDebug("本地缓存继承全局过期时间: {GlobalExpiry}", options.Expiry);
            }
        }

        // 验证动态 TTL 调整委托的有效性
        if (options.AdjustExpiryForValue != null)
        {
            _logger.LogDebug("启用动态 TTL 调整委托 (普通缓存)");
        }

        if (options.AdjustExpiryForHash != null)
        {
            _logger.LogDebug("启用动态 TTL 调整委托 (Hash 缓存)");
        }
    }

    /// <summary>
    /// 获取本地缓存键
    /// </summary>
    /// <param name="key">原始键</param>
    /// <returns>本地缓存键</returns>
    private static string GetLocalCacheKey(string key)
    {
        return $"local:{key}";
    }

    /// <summary>
    /// 获取分布式缓存过期时间
    /// 需求 2.1: 统一过期时间 - 为所有缓存项应用默认 TTL
    /// 需求 2.4: 动态调整过期时间委托 - 根据数据内容动态调整 TTL
    /// </summary>
    /// <param name="options">配置选项</param>
    /// <param name="value">缓存值</param>
    /// <returns>分布式缓存过期时间</returns>
    private TimeSpan GetDistributedCacheExpiry(CacheServiceOptions options, object? value)
    {
        return AdjustExpiry(options, value);
    }

    /// <summary>
    /// 获取分布式缓存 Hash 过期时间
    /// 需求 2.1: 统一过期时间 - 为所有缓存项应用默认 TTL
    /// 需求 2.5: Hash 缓存 TTL 调整 - 基于 Hash 内容调整过期时间
    /// </summary>
    /// <param name="options">配置选项</param>
    /// <param name="hashData">Hash 缓存数据</param>
    /// <returns>分布式缓存 Hash 过期时间</returns>
    private TimeSpan GetDistributedCacheHashExpiry(CacheServiceOptions options, Dictionary<string, string> hashData)
    {
        return AdjustHashExpiry(options, hashData);
    }

    /// <summary>
    /// 获取本地缓存过期时间
    /// 需求 2.2: 本地缓存独立过期时间 - 本地缓存使用独立的 TTL
    /// 需求 2.3: TTL 继承 - 未设置本地缓存过期时间时继承统一过期时间
    /// </summary>
    /// <param name="options">配置选项</param>
    /// <returns>本地缓存过期时间</returns>
    private static TimeSpan GetLocalExpiry(CacheServiceOptions options)
    {
        // 需求 2.2: 如果设置了本地缓存独立过期时间，使用独立 TTL
        if (options.LocalExpiry.HasValue)
        {
            return options.LocalExpiry.Value;
        }
        
        // 需求 2.3: 未设置本地缓存过期时间时，继承全局过期时间
        return options.Expiry;
    }

    /// <summary>
    /// 调整缓存过期时间
    /// 需求 2.1: 统一过期时间 - 为所有缓存项应用默认 TTL
    /// 需求 2.4: 动态调整过期时间委托 - 根据数据内容动态调整 TTL
    /// </summary>
    /// <param name="options">配置选项</param>
    /// <param name="value">缓存值</param>
    /// <returns>调整后的过期时间</returns>
    private TimeSpan AdjustExpiry(CacheServiceOptions options, object? value)
    {
        // 需求 2.1: 使用全局缓存过期时间作为基础 TTL
        var expiry = options.Expiry;
        
        // 需求 2.4: 如果提供了动态调整过期时间委托，则调用委托函数
        if (options.AdjustExpiryForValue != null)
        {
            try
            {
                var adjustedExpiry = options.AdjustExpiryForValue(expiry, value);
                _logger.LogDebug("动态调整缓存过期时间: 原始={OriginalExpiry}, 调整后={AdjustedExpiry}, 键类型={ValueType}", 
                    options.Expiry, adjustedExpiry, value?.GetType().Name ?? "null");
                return adjustedExpiry;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "动态调整缓存过期时间失败，使用默认过期时间: {DefaultExpiry}, 值类型: {ValueType}", 
                    options.Expiry, value?.GetType().Name ?? "null");
            }
        }

        return expiry;
    }

    /// <summary>
    /// 处理异常
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="ex">异常</param>
    /// <param name="options">配置选项</param>
    /// <param name="key">缓存键</param>
    /// <param name="operation">操作名称</param>
    /// <returns>默认值或异常处理结果</returns>
    private async Task<T?> HandleException<T>(Exception ex, CacheServiceOptions options, string key, string operation)
    {
        _logger.LogError(ex, "缓存操作异常: {Operation}, 键: {Key}, 异常类型: {ExceptionType}", operation, key, ex.GetType().Name);

        // 调用异常回调（需求 4.3: 异常回调机制）
        if (options.OnError != null)
        {
            try
            {
                var callbackResult = options.OnError(ex);
                _logger.LogDebug("异常回调执行完成: {Operation}, 键: {Key}", operation, key);
                
                // 如果回调返回了合适的类型，直接返回
                if (callbackResult is T result)
                {
                    return result;
                }
                
                // 如果回调返回了其他类型，尝试转换
                if (callbackResult != null && typeof(T).IsAssignableFrom(callbackResult.GetType()))
                {
                    return (T)callbackResult;
                }
            }
            catch (Exception callbackEx)
            {
                _logger.LogError(callbackEx, "异常回调执行失败: {Operation}, 键: {Key}", operation, key);
                
                // 如果异常回调本身失败且不隐藏异常，抛出原始异常
                if (!options.HideErrors)
                {
                    throw new InvalidOperationException($"缓存操作失败且异常回调执行失败: {operation}, 键: {key}", ex);
                }
            }
        }

        // 需求 4.2: 透明异常模式 - 如果不隐藏异常，则重新抛出
        if (!options.HideErrors)
        {
            throw new InvalidOperationException($"缓存操作失败: {operation}, 键: {key}", ex);
        }

        // 需求 4.1: 异常隐藏模式 - 执行降级逻辑
        _logger.LogDebug("隐藏异常并执行降级逻辑: {Operation}, 键: {Key}", operation, key);

        // 尝试执行降级策略
        var fallbackResult = await ExecuteFallbackStrategy<T>(key, operation, options);
        if (fallbackResult.HasValue)
        {
            return fallbackResult.Value;
        }

        // 如果没有可用的降级策略，返回默认值
        return default(T);
    }

    /// <summary>
    /// 执行降级策略
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="operation">操作名称</param>
    /// <param name="options">配置选项</param>
    /// <returns>降级策略执行结果</returns>
    private async Task<(bool HasValue, T? Value)> ExecuteFallbackStrategy<T>(string key, string operation, CacheServiceOptions options)
    {
        // 需求 4.5: 默认值降级策略
        if (options.FallbackToDefault && options.DefaultValue != null)
        {
            try
            {
                if (options.DefaultValue is T defaultValue)
                {
                    _logger.LogDebug("使用默认值降级策略: {Operation}, 键: {Key}", operation, key);
                    return (true, defaultValue);
                }
                
                // 尝试类型转换
                if (typeof(T).IsAssignableFrom(options.DefaultValue.GetType()))
                {
                    _logger.LogDebug("使用默认值降级策略 (类型转换): {Operation}, 键: {Key}", operation, key);
                    return (true, (T)options.DefaultValue);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "默认值降级策略执行失败: {Operation}, 键: {Key}", operation, key);
            }
        }

        // 需求 4.6: 自定义函数降级策略
        if (options.FallbackFunction != null)
        {
            try
            {
                var fallbackResult = options.FallbackFunction(key, operation);
                if (fallbackResult is T result)
                {
                    _logger.LogDebug("使用自定义函数降级策略: {Operation}, 键: {Key}", operation, key);
                    return (true, result);
                }
                
                // 尝试类型转换
                if (fallbackResult != null && typeof(T).IsAssignableFrom(fallbackResult.GetType()))
                {
                    _logger.LogDebug("使用自定义函数降级策略 (类型转换): {Operation}, 键: {Key}", operation, key);
                    return (true, (T)fallbackResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "自定义函数降级策略执行失败: {Operation}, 键: {Key}", operation, key);
            }
        }

        return (false, default(T));
    }

    /// <inheritdoc />
    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, Action<CacheServiceOptions>? configure = null, CancellationToken cancellationToken = default)
    {
        var options = GetEffectiveOptions(configure);
        
        try
        {
            // 首先尝试获取缓存
            var cachedValue = await GetAsync<T>(key, configure, cancellationToken);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            // 缓存未命中，需要通过工厂方法获取值
            if (options.EnableLock)
            {
                // 使用分布式锁防止缓存击穿
                return await GetOrSetWithLockAsync(key, factory, options, cancellationToken);
            }
            else
            {
                // 不使用锁，直接调用工厂方法
                return await GetOrSetWithoutLockAsync(key, factory, options, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            return await HandleException<T>(ex, options, key, "GetOrSetAsync");
        }
    }

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
            var queryFieldsList = queryFields.ToList();
            
            // 首先尝试从缓存获取 Hash 数据
            if (options.UseDistributed)
            {
                var hashData = await _distributedCacheService.HashGetAsync(hashKey, queryFieldsList, options.RedisInstanceName);
                if (hashData != null && hashData.Count > 0)
                {
                    // 检查时间戳验证缓存是否过期
                    if (IsHashCacheValid(hashData, options))
                    {
                        _logger.LogDebug("Hash 缓存命中: {HashKey}", hashKey);
                        return parseFromHash(hashData);
                    }
                    else
                    {
                        _logger.LogDebug("Hash 缓存已过期: {HashKey}", hashKey);
                    }
                }
            }

            // Hash 缓存未命中或已过期，需要重建
            if (options.EnableLock)
            {
                return await GetOrSetHashWithLockAsync(hashKey, queryFieldsList, parseFromHash, buildFullCache, options, cancellationToken);
            }
            else
            {
                return await GetOrSetHashWithoutLockAsync(hashKey, queryFieldsList, parseFromHash, buildFullCache, options, cancellationToken);
            }
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
    /// 使用分布式锁的 GetOrSet 操作
    /// </summary>
    private async Task<T?> GetOrSetWithLockAsync<T>(string key, Func<Task<T>> factory, CacheServiceOptions options, CancellationToken cancellationToken)
    {
        var lockKey = $"lock:{key}";
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
                _logger.LogDebug("获取分布式锁成功: {LockKey}", lockKey);
                
                // 再次检查缓存（双重检查锁定模式）
                var cachedValue = await GetAsync<T>(key, null, cancellationToken);
                if (cachedValue != null)
                {
                    _logger.LogDebug("双重检查缓存命中: {Key}", key);
                    return cachedValue;
                }

                // 调用工厂方法获取值
                var value = await factory();
                if (value != null)
                {
                    // 设置缓存
                    await SetAsync(key, value, null, cancellationToken);
                    _logger.LogDebug("通过工厂方法获取值并设置缓存: {Key}", key);
                }

                return value;
            }
            else
            {
                _logger.LogWarning("获取分布式锁失败: {LockKey}", lockKey);
                
                // 锁获取失败，执行降级策略
                return await HandleLockFailure<T>(key, factory, options, cancellationToken);
            }
        }
        finally
        {
            if (distributedLock != null)
            {
                await distributedLock.DisposeAsync();
                _logger.LogDebug("释放分布式锁: {LockKey}", lockKey);
            }
        }
    }

    /// <summary>
    /// 不使用分布式锁的 GetOrSet 操作
    /// </summary>
    private async Task<T?> GetOrSetWithoutLockAsync<T>(string key, Func<Task<T>> factory, CacheServiceOptions options, CancellationToken cancellationToken)
    {
        // 直接调用工厂方法
        var value = await factory();
        if (value != null)
        {
            // 设置缓存
            await SetAsync(key, value, null, cancellationToken);
            _logger.LogDebug("通过工厂方法获取值并设置缓存 (无锁): {Key}", key);
        }

        return value;
    }

    /// <summary>
    /// 使用分布式锁的 Hash GetOrSet 操作
    /// </summary>
    private async Task<TResult?> GetOrSetHashWithLockAsync<TResult>(
        string hashKey,
        List<string> queryFields,
        Func<Dictionary<string, string>, TResult?> parseFromHash,
        Func<Task<Dictionary<string, string>>> buildFullCache,
        CacheServiceOptions options,
        CancellationToken cancellationToken)
    {
        var lockKey = $"lock:{hashKey}";
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
                    var hashData = await _distributedCacheService.HashGetAsync(hashKey, queryFields, options.RedisInstanceName);
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
                        ["cache_timestamp"] = DateTimeOffset.UtcNow.ToString("O")
                    };

                    // 调整 Hash 缓存 TTL
                    var expiry = GetDistributedCacheHashExpiry(options, cacheDataWithTimestamp);

                    // 设置 Hash 缓存
                    if (options.UseDistributed)
                    {
                        await _distributedCacheService.HashSetAsync(hashKey, cacheDataWithTimestamp, expiry, options.RedisInstanceName);
                        _logger.LogDebug("通过工厂方法构建并设置 Hash 缓存: {HashKey}, TTL: {Expiry}", hashKey, expiry);
                    }

                    // 解析并返回结果
                    return parseFromHash(cacheDataWithTimestamp);
                }

                return default(TResult);
            }
            else
            {
                _logger.LogWarning("获取 Hash 分布式锁失败: {LockKey}", lockKey);
                
                // 锁获取失败，执行降级策略
                return await HandleHashLockFailure<TResult>(hashKey, queryFields, parseFromHash, buildFullCache, options, cancellationToken);
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
    /// 不使用分布式锁的 Hash GetOrSet 操作
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
                ["cache_timestamp"] = DateTimeOffset.UtcNow.ToString("O")
            };

            // 调整 Hash 缓存 TTL
            var expiry = GetDistributedCacheHashExpiry(options, cacheDataWithTimestamp);

            // 设置 Hash 缓存
            if (options.UseDistributed)
            {
                await _distributedCacheService.HashSetAsync(hashKey, cacheDataWithTimestamp, expiry, options.RedisInstanceName);
                _logger.LogDebug("通过工厂方法构建并设置 Hash 缓存 (无锁): {HashKey}, TTL: {Expiry}", hashKey, expiry);
            }

            // 解析并返回结果
            return parseFromHash(cacheDataWithTimestamp);
        }

        return default(TResult);
    }

    /// <summary>
    /// 检查 Hash 缓存是否有效（基于时间戳）
    /// </summary>
    private bool IsHashCacheValid(Dictionary<string, string> hashData, CacheServiceOptions options)
    {
        if (!hashData.TryGetValue("cache_timestamp", out var timestampStr))
        {
            // 没有时间戳，认为缓存有效
            return true;
        }

        if (DateTimeOffset.TryParse(timestampStr, out var timestamp))
        {
            var age = DateTimeOffset.UtcNow - timestamp;
            var isValid = age < options.Expiry;
            
            if (!isValid)
            {
                _logger.LogDebug("Hash 缓存已过期: 年龄 {Age}, 最大年龄 {MaxAge}", age, options.Expiry);
            }
            
            return isValid;
        }

        // 时间戳格式无效，认为缓存有效
        return true;
    }

    /// <summary>
    /// 调整 Hash 缓存过期时间
    /// 需求 2.1: 统一过期时间 - 为所有缓存项应用默认 TTL
    /// 需求 2.5: Hash 缓存 TTL 调整 - 基于 Hash 内容调整过期时间
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
        {
            try
            {
                var adjustedExpiry = options.AdjustExpiryForHash(expiry, hashData);
                _logger.LogDebug("动态调整 Hash 缓存过期时间: 原始={OriginalExpiry}, 调整后={AdjustedExpiry}, Hash字段数={FieldCount}", 
                    options.Expiry, adjustedExpiry, hashData.Count);
                return adjustedExpiry;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "动态调整 Hash 缓存过期时间失败，使用默认过期时间: {DefaultExpiry}, Hash字段数: {FieldCount}", 
                    options.Expiry, hashData.Count);
            }
        }

        return expiry;
    }

    /// <summary>
    /// 处理锁获取失败的降级策略
    /// </summary>
    private async Task<T?> HandleLockFailure<T>(string key, Func<Task<T>> factory, CacheServiceOptions options, CancellationToken cancellationToken)
    {
        _logger.LogWarning("分布式锁获取失败: {Key}", key);

        // 需求 4.4: 工厂方法降级策略
        if (options.FallbackToFactory)
        {
            try
            {
                _logger.LogDebug("锁获取失败，回退到工厂方法: {Key}", key);
                var value = await factory();
                
                // 尝试设置缓存（可能会失败，但不影响返回值）
                if (value != null)
                {
                    try
                    {
                        await SetAsync(key, value, null, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "锁失败降级时设置缓存失败: {Key}", key);
                    }
                }
                
                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "工厂方法降级策略执行失败: {Key}", key);
                
                // 如果工厂方法失败，继续尝试其他降级策略
            }
        }

        // 尝试其他降级策略
        var fallbackResult = await ExecuteFallbackStrategy<T>(key, "LockFailure", options);
        if (fallbackResult.HasValue)
        {
            return fallbackResult.Value;
        }

        _logger.LogWarning("锁获取失败且无可用降级策略: {Key}", key);
        return default(T);
    }

    /// <summary>
    /// 处理 Hash 锁获取失败的降级策略
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
        {
            try
            {
                _logger.LogDebug("Hash 锁获取失败，回退到工厂方法: {HashKey}", hashKey);
                var fullCacheData = await buildFullCache();
                
                if (fullCacheData != null && fullCacheData.Count > 0)
                {
                    // 尝试设置缓存（可能会失败，但不影响返回值）
                    try
                    {
                        var cacheDataWithTimestamp = new Dictionary<string, string>(fullCacheData)
                        {
                            ["cache_timestamp"] = DateTimeOffset.UtcNow.ToString("O")
                        };
                        
                        var expiry = GetDistributedCacheHashExpiry(options, cacheDataWithTimestamp);
                        
                        if (options.UseDistributed)
                        {
                            await _distributedCacheService.HashSetAsync(hashKey, cacheDataWithTimestamp, expiry, options.RedisInstanceName);
                        }
                        
                        return parseFromHash(cacheDataWithTimestamp);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Hash 锁失败降级时设置缓存失败: {HashKey}", hashKey);
                        return parseFromHash(fullCacheData);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hash 工厂方法降级策略执行失败: {HashKey}", hashKey);
                
                // 如果工厂方法失败，继续尝试其他降级策略
            }
        }

        // 尝试其他降级策略
        var fallbackResult = await ExecuteFallbackStrategy<TResult>(hashKey, "HashLockFailure", options);
        if (fallbackResult.HasValue)
        {
            return fallbackResult.Value;
        }

        _logger.LogWarning("Hash 锁获取失败且无可用降级策略: {HashKey}", hashKey);
        return default(TResult);
    }
}