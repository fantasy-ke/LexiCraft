namespace BuildingBlocks.Caching.Options;

/// <summary>
///     定义单次缓存调用使用的缓存层、TTL、序列化、锁及错误降级策略。
/// </summary>
/// <remarks>
///     <see cref="BuildingBlocks.Caching.Abstractions.ICacheService"/> 会为每次调用创建新实例并应用配置委托；
///     此类型不会从配置文件自动绑定。除非另有说明，非正 TTL 会回退到组件默认值。
/// </remarks>
public class CacheServiceOptions
{
    /// <summary>
    ///     获取或设置是否访问 Redis 分布式缓存；默认为 <see langword="true"/>。
    /// </summary>
    public bool UseDistributed { get; set; } = true;

    /// <summary>
    ///     获取或设置是否使用当前进程的内存缓存；默认为 <see langword="false"/>。
    /// </summary>
    /// <remarks>Hash API 不使用本地缓存。进程间不会自动失效本地副本。</remarks>
    public bool UseLocal { get; set; }

    /// <summary>
    ///     获取或设置 Redis 值的基础 TTL，也是 Hash 内部时间戳的逻辑有效期；默认为 180 分钟。
    /// </summary>
    public TimeSpan Expiry { get; set; } = TimeSpan.FromMinutes(180);

    /// <summary>
    ///     获取或设置本地缓存的独立 TTL；为 <see langword="null"/> 时继承 <see cref="Expiry"/>。
    /// </summary>
    public TimeSpan? LocalExpiry { get; set; }

    /// <summary>
    ///     获取或设置是否记录并隐藏非取消异常；默认为 <see langword="true"/>。
    /// </summary>
    /// <remarks>
    ///     隐藏错误后，操作返回配置的回调/降级值或 <see langword="default"/>。设置为 <see langword="false"/>
    ///     时，组件以 <see cref="InvalidOperationException"/> 包装原异常。<see cref="OperationCanceledException"/>
    ///     始终原样传播，不受此选项影响。
    /// </remarks>
    public bool HideErrors { get; set; } = true;

    /// <summary>
    ///     获取或设置是否尝试以 GZip 解压读取，并对序列化后超过 1024 字节的 Redis 值进行 GZip 压缩。
    /// </summary>
    /// <remarks>该选项只影响普通 Redis String 值，不影响本地缓存或 Hash 字符串字段。</remarks>
    public bool EnableCompression { get; set; }

    /// <summary>
    ///     获取或设置是否使用 MemoryPack 代替默认 JSON 序列化普通 Redis String 值。
    /// </summary>
    /// <remarks>读写同一键必须使用兼容的序列化设置和数据契约。</remarks>
    public bool EnableBinarySerialization { get; set; }

    /// <summary>
    ///     获取或设置缓存未命中重建时是否使用单 Redis 实例锁防止并发击穿；默认为 <see langword="true"/>。
    /// </summary>
    public bool EnableLock { get; set; } = true;

    /// <summary>
    ///     获取或设置已取得锁的租期；默认为 1 秒。
    /// </summary>
    /// <remarks>组件不会自动续期，工厂和写缓存的总耗时应显著小于此值。</remarks>
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     获取或设置等待取得锁的最长时间；默认为 1 秒，零表示仅尝试一次。
    /// </summary>
    public TimeSpan LockAcquireTimeout { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     获取或设置锁获取失败后是否直接执行工厂；默认为 <see langword="true"/>。
    /// </summary>
    /// <remarks>启用后可保持可用性，但会失去防击穿保证，多个调用方可能同时执行工厂。</remarks>
    public bool FallbackToFactory { get; set; } = true;

    /// <summary>
    ///     获取或设置错误或锁失败时是否尝试返回 <see cref="DefaultValue"/>。
    /// </summary>
    public bool FallbackToDefault { get; set; }

    /// <summary>
    ///     获取或设置默认降级值；仅在 <see cref="FallbackToDefault"/> 启用且值可赋给目标类型时生效。
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    ///     获取或设置自定义降级函数；参数依次为缓存键和操作名称，返回值必须兼容目标类型。
    /// </summary>
    /// <remarks>默认值降级先于此函数执行；函数异常会被记录并忽略。</remarks>
    public Func<string, string, object>? FallbackFunction { get; set; }

    /// <summary>
    ///     获取或设置非取消异常回调；返回值兼容目标类型时会直接作为操作结果。
    /// </summary>
    /// <remarks>
    ///     此回调先于 <see cref="HideErrors"/> 判断执行。回调自身失败且未隐藏错误时，组件抛出包含原缓存异常的
    ///     <see cref="InvalidOperationException"/>。
    /// </remarks>
    public Func<Exception, object>? OnError { get; set; }

    /// <summary>
    ///     获取或设置根据完整 Hash 内容调整 Redis 物理 TTL 的函数；参数为基础 TTL 和包含内部时间戳的字段字典。
    /// </summary>
    /// <remarks>返回非正值或抛出异常时使用 <see cref="Expiry"/>；不会改变内部时间戳的逻辑有效期。</remarks>
    public Func<TimeSpan, Dictionary<string, string>, TimeSpan>? AdjustExpiryForHash { get; set; }

    /// <summary>
    ///     获取或设置根据普通缓存值调整 Redis 物理 TTL 的函数；参数为基础 TTL 和待缓存值。
    /// </summary>
    /// <remarks>返回非正值或抛出异常时使用 <see cref="Expiry"/>；本地缓存仍使用 <see cref="LocalExpiry"/> 规则。</remarks>
    public Func<TimeSpan, object?, TimeSpan>? AdjustExpiryForValue { get; set; }

    /// <summary>
    ///     获取或设置本次操作使用的命名 Redis 实例；空值或空白值表示 <c>default</c>。
    /// </summary>
    /// <remarks>实例名也包含在本地缓存内部键中，以隔离相同业务键在不同 Redis 实例上的本地副本。</remarks>
    public string? RedisInstanceName { get; set; }

    // 预设配置静态方法

    /// <summary>
    ///     获取仅使用 Redis、TTL 为 1 小时并启用锁和错误隐藏的新选项实例。
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
    ///     获取仅使用进程内缓存、TTL 为 30 分钟且不使用锁的新选项实例。
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
    ///     获取 Redis 与本地缓存并用的新选项实例；Redis TTL 为 1 小时，本地 TTL 为 10 分钟。
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
    ///     获取仅使用 Redis 并将锁租期和获取等待均设为 2 秒的新选项实例。
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
    ///     获取混合缓存、较长 TTL、3 秒锁及工厂回退的新选项实例。
    /// </summary>
    /// <remarks>此预设隐藏依赖错误，但未配置默认值或自定义降级函数。</remarks>
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
    ///     获取仅使用 Redis、启用 MemoryPack 与 GZip 并使用 1 小时 TTL 的新选项实例。
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
    ///     获取混合缓存、MemoryPack、GZip、5 分钟本地 TTL 与 500 毫秒锁的新选项实例。
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
    ///     获取混合缓存、短 TTL、关闭锁和压缩，并公开依赖异常的新选项实例。
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
    ///     获取混合缓存、MemoryPack、GZip、较长 TTL、2 秒锁及工厂回退的新选项实例。
    /// </summary>
    /// <remarks>使用前仍需确认 DTO 的 MemoryPack 兼容性、数据陈旧窗口和临界区耗时。</remarks>
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