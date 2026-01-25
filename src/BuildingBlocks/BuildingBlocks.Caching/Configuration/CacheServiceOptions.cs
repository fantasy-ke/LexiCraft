namespace BuildingBlocks.Caching.Configuration;

/// <summary>
///     缓存服务配置选项
/// </summary>
public class CacheServiceOptions
{
    /// <summary>
    ///     是否使用分布式缓存
    /// </summary>
    public bool UseDistributed { get; set; } = true;

    /// <summary>
    ///     是否使用本地缓存
    /// </summary>
    public bool UseLocal { get; set; }

    /// <summary>
    ///     全局缓存过期时间
    /// </summary>
    public TimeSpan Expiry { get; set; } = TimeSpan.FromMinutes(180);

    /// <summary>
    ///     本地缓存独立过期时间
    /// </summary>
    public TimeSpan? LocalExpiry { get; set; }

    /// <summary>
    ///     是否隐藏异常
    /// </summary>
    public bool HideErrors { get; set; } = true;

    /// <summary>
    ///     是否启用 GZip 压缩
    /// </summary>
    public bool EnableCompression { get; set; }

    /// <summary>
    ///     是否启用二进制序列化
    /// </summary>
    public bool EnableBinarySerialization { get; set; }

    /// <summary>
    ///     是否启用分布式锁
    /// </summary>
    public bool EnableLock { get; set; } = true;

    /// <summary>
    ///     锁超时时间
    /// </summary>
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     锁获取超时时间
    /// </summary>
    public TimeSpan LockAcquireTimeout { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     锁失败是否回退到工厂方法
    /// </summary>
    public bool FallbackToFactory { get; set; } = true;

    /// <summary>
    ///     是否回退到默认值
    /// </summary>
    public bool FallbackToDefault { get; set; }

    /// <summary>
    ///     默认降级值
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    ///     自定义降级函数
    /// </summary>
    public Func<string, string, object>? FallbackFunction { get; set; }

    /// <summary>
    ///     异常回调
    /// </summary>
    public Func<Exception, object>? OnError { get; set; }

    /// <summary>
    ///     动态调整 Hash 缓存 TTL
    /// </summary>
    public Func<TimeSpan, Dictionary<string, string>, TimeSpan>? AdjustExpiryForHash { get; set; }

    /// <summary>
    ///     动态调整普通缓存 TTL
    /// </summary>
    public Func<TimeSpan, object?, TimeSpan>? AdjustExpiryForValue { get; set; }


    /// <summary>
    ///     Redis 实例名称
    /// </summary>
    public string? RedisInstanceName { get; set; }

    // 预设配置静态方法

    /// <summary>
    ///     分布式缓存预设配置
    /// </summary>
    public static CacheServiceOptions Distributed => new()
    {
        UseDistributed = true,
        UseLocal = false,
        Expiry = TimeSpan.FromHours(1),
        EnableLock = true,
        HideErrors = true
    };

    /// <summary>
    ///     本地缓存预设配置
    /// </summary>
    public static CacheServiceOptions Local => new()
    {
        UseDistributed = false,
        UseLocal = true,
        Expiry = TimeSpan.FromMinutes(30),
        EnableLock = false,
        HideErrors = true
    };

    /// <summary>
    ///     混合缓存预设配置
    /// </summary>
    public static CacheServiceOptions Hybrid => new()
    {
        UseDistributed = true,
        UseLocal = true,
        Expiry = TimeSpan.FromHours(1),
        LocalExpiry = TimeSpan.FromMinutes(10),
        EnableLock = true,
        HideErrors = true
    };

    /// <summary>
    ///     启用分布式锁的预设配置
    /// </summary>
    public static CacheServiceOptions WithLock => new()
    {
        UseDistributed = true,
        UseLocal = false,
        Expiry = TimeSpan.FromMinutes(30),
        EnableLock = true,
        LockTimeout = TimeSpan.FromSeconds(2),
        LockAcquireTimeout = TimeSpan.FromSeconds(2),
        FallbackToFactory = true,
        HideErrors = true
    };

    /// <summary>
    ///     高可用性预设配置（启用多种降级策略）
    /// </summary>
    public static CacheServiceOptions HighAvailability => new()
    {
        UseDistributed = true,
        UseLocal = true,
        Expiry = TimeSpan.FromHours(2),
        LocalExpiry = TimeSpan.FromMinutes(15),
        EnableLock = true,
        HideErrors = true,
        FallbackToFactory = true,
        FallbackToDefault = false,
        LockTimeout = TimeSpan.FromSeconds(3),
        LockAcquireTimeout = TimeSpan.FromSeconds(3)
    };

    /// <summary>
    ///     二进制序列化预设配置
    /// </summary>
    public static CacheServiceOptions BinarySerialization => new()
    {
        UseDistributed = true,
        UseLocal = false,
        Expiry = TimeSpan.FromHours(1),
        EnableBinarySerialization = true,
        EnableCompression = true,
        EnableLock = true,
        HideErrors = true
    };

    /// <summary>
    ///     高性能预设配置（启用压缩和二进制序列化）
    /// </summary>
    public static CacheServiceOptions HighPerformance => new()
    {
        UseDistributed = true,
        UseLocal = true,
        Expiry = TimeSpan.FromHours(1),
        LocalExpiry = TimeSpan.FromMinutes(5),
        EnableBinarySerialization = true,
        EnableCompression = true,
        EnableLock = true,
        HideErrors = true,
        LockTimeout = TimeSpan.FromMilliseconds(500),
        LockAcquireTimeout = TimeSpan.FromMilliseconds(500)
    };

    /// <summary>
    ///     开发环境预设配置（不隐藏异常，便于调试）
    /// </summary>
    public static CacheServiceOptions Development => new()
    {
        UseDistributed = true,
        UseLocal = true,
        Expiry = TimeSpan.FromMinutes(10),
        LocalExpiry = TimeSpan.FromMinutes(2),
        EnableLock = false,
        HideErrors = false,
        EnableBinarySerialization = false,
        EnableCompression = false
    };

    /// <summary>
    ///     生产环境预设配置（高可用性和性能优化）
    /// </summary>
    public static CacheServiceOptions Production => new()
    {
        UseDistributed = true,
        UseLocal = true,
        Expiry = TimeSpan.FromHours(4),
        LocalExpiry = TimeSpan.FromMinutes(30),
        EnableBinarySerialization = true,
        EnableCompression = true,
        EnableLock = true,
        HideErrors = true,
        FallbackToFactory = true,
        LockTimeout = TimeSpan.FromSeconds(2),
        LockAcquireTimeout = TimeSpan.FromSeconds(2)
    };
}