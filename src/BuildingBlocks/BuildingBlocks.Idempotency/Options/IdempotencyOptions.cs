namespace BuildingBlocks.Idempotency.Options;

/// <summary>
///     配置幂等请求头、Redis 键、租约、重放等待和请求/响应大小限制。
/// </summary>
public sealed class IdempotencyOptions
{
    /// <summary>
    ///     配置文件中的默认节名称。
    /// </summary>
    public const string SectionName = "Idempotency";

    /// <summary>
    ///     获取或设置客户端传递幂等键的请求头名称。
    /// </summary>
    public string HeaderName { get; set; } = "Idempotency-Key";

    /// <summary>
    ///     获取或设置 Redis 租约键和完成结果键的公共前缀。
    /// </summary>
    public string Prefix { get; set; } = "lexicraft:idempotency";

    /// <summary>
    ///     获取或设置缓存组件中的命名 Redis 实例；空值表示使用默认实例。
    /// </summary>
    public string? RedisInstanceName { get; set; }

    /// <summary>
    ///     获取或设置 Replay/Reject 模式成功记录的默认保留时间。
    /// </summary>
    public TimeSpan Retention { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    ///     获取或设置请求执行租约的默认有效期。
    /// </summary>
    public TimeSpan ProcessingTimeout { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    ///     获取或设置 Replay 模式等待并发首请求完成的最长时间。
    /// </summary>
    public TimeSpan ReplayWaitTimeout { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>
    ///     获取或设置 Replay 模式轮询幂等状态的时间间隔。
    /// </summary>
    public TimeSpan ReplayPollInterval { get; set; } = TimeSpan.FromMilliseconds(50);

    /// <summary>
    ///     获取或设置参与请求指纹计算的最大请求体字节数。
    /// </summary>
    public long MaxRequestBodyBytes { get; set; } = 1024 * 1024;

    /// <summary>
    ///     获取或设置可在内存中捕获并重放的最大响应体字节数。
    /// </summary>
    public long MaxResponseBodyBytes { get; set; } = 1024 * 1024;

    /// <summary>
    ///     获取或设置客户端幂等键去除首尾空白后的最大字符数。
    /// </summary>
    public int MaxKeyLength { get; set; } = 200;
}