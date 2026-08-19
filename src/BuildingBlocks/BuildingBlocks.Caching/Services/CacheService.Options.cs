using BuildingBlocks.Caching.Options;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Caching.Services;

internal sealed partial class CacheService
{
    /// <summary>
    ///     获取有效的配置选项
    /// </summary>
    /// <param name="configure">配置委托</param>
    /// <returns>有效的配置选项</returns>
    private CacheServiceOptions GetEffectiveOptions(Action<CacheServiceOptions>? configure)
    {
        var options = new CacheServiceOptions();
        configure?.Invoke(options);

        // 应用 TTL 继承和覆盖逻辑
        ApplyTtlInheritanceRules(options);

        return options;
    }

    /// <summary>
    ///     应用 TTL 继承和覆盖逻辑
    ///     需求 2.1: 统一过期时间 - 为所有缓存项应用默认 TTL
    ///     需求 2.2: 本地缓存独立过期时间 - 本地缓存使用独立的 TTL
    ///     需求 2.3: TTL 继承 - 未设置本地缓存过期时间时继承统一过期时间
    /// </summary>
    /// <param name="options">配置选项</param>
    private void ApplyTtlInheritanceRules(CacheServiceOptions options)
    {
        // 需求 2.1: 确保全局过期时间有效（不能为零或负数）
        if (options.Expiry <= TimeSpan.Zero)
        {
            var defaultExpiry = new CacheServiceOptions().Expiry;
            _logger.LogWarning("全局过期时间无效: {Expiry}，使用默认值: {DefaultExpiry}",
                options.Expiry, defaultExpiry);
            options.Expiry = defaultExpiry;
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
        if (options.AdjustExpiryForValue != null) _logger.LogDebug("启用动态 TTL 调整委托 (普通缓存)");

        if (options.AdjustExpiryForHash != null) _logger.LogDebug("启用动态 TTL 调整委托 (Hash 缓存)");
    }

    /// <summary>
    ///     获取本地缓存键
    /// </summary>
    /// <param name="key">原始业务键。</param>
    /// <param name="options">用于隔离命名 Redis 实例的本次调用选项。</param>
    /// <returns>包含实例名的本地缓存键。</returns>
    private static string GetLocalCacheKey(string key, CacheServiceOptions options)
    {
        var instanceName = string.IsNullOrWhiteSpace(options.RedisInstanceName)
            ? "default"
            : options.RedisInstanceName;
        return $"local:{instanceName}:{key}";
    }

    /// <summary>
    ///     获取分布式缓存过期时间
    ///     需求 2.1: 统一过期时间 - 为所有缓存项应用默认 TTL
    ///     需求 2.4: 动态调整过期时间委托 - 根据数据内容动态调整 TTL
    /// </summary>
    /// <param name="options">配置选项</param>
    /// <param name="value">缓存值</param>
    /// <returns>分布式缓存过期时间</returns>
    private TimeSpan GetDistributedCacheExpiry(CacheServiceOptions options, object? value)
    {
        return AdjustExpiry(options, value);
    }


    /// <summary>
    ///     获取本地缓存过期时间
    ///     需求 2.2: 本地缓存独立过期时间 - 本地缓存使用独立的 TTL
    ///     需求 2.3: TTL 继承 - 未设置本地缓存过期时间时继承统一过期时间
    /// </summary>
    /// <param name="options">配置选项</param>
    /// <returns>本地缓存过期时间</returns>
    private static TimeSpan GetLocalExpiry(CacheServiceOptions options)
    {
        // 需求 2.2: 如果设置了本地缓存独立过期时间，使用独立 TTL
        if (options.LocalExpiry.HasValue) return options.LocalExpiry.Value;

        // 需求 2.3: 未设置本地缓存过期时间时，继承全局过期时间
        return options.Expiry;
    }

    /// <summary>
    ///     调整缓存过期时间
    ///     需求 2.1: 统一过期时间 - 为所有缓存项应用默认 TTL
    ///     需求 2.4: 动态调整过期时间委托 - 根据数据内容动态调整 TTL
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
            try
            {
                var adjustedExpiry = options.AdjustExpiryForValue(expiry, value);
                if (adjustedExpiry <= TimeSpan.Zero)
                {
                    _logger.LogWarning("Adjusted cache expiry {AdjustedExpiry} is invalid; using {DefaultExpiry}.",
                        adjustedExpiry, expiry);
                    return expiry;
                }

                _logger.LogDebug("动态调整缓存过期时间: 原始={OriginalExpiry}, 调整后={AdjustedExpiry}, 键类型={ValueType}",
                    options.Expiry, adjustedExpiry, value?.GetType().Name ?? "null");
                return adjustedExpiry;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "动态调整缓存过期时间失败，使用默认过期时间: {DefaultExpiry}, 值类型: {ValueType}",
                    options.Expiry, value?.GetType().Name ?? "null");
            }

        return expiry;
    }
}
