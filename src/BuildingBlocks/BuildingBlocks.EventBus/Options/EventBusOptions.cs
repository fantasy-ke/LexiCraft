namespace BuildingBlocks.EventBus.Options;

/// <summary>
///     EventBus 配置选项
/// </summary>
public class EventBusOptions
{
    /// <summary>
    ///     是否开启本地内存传递 (基于 Channel)
    /// </summary>
    public bool EnableLocal { get; set; } = true;

    /// <summary>
    ///     是否开启 Redis 分布式分发 (用于跨微服务集成)
    /// </summary>
    public bool EnableRedis { get; set; } = false;

    /// <summary>
    ///     Redis 配置详情
    /// </summary>
    public RedisConfig Redis { get; set; } = new();
}

/// <summary>
///     Redis 具体配置
/// </summary>
public class RedisConfig
{
    /// <summary>
    ///     Redis 连接字符串
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    ///     频道名前缀
    /// </summary>
    public string Prefix { get; set; } = "lexi";

    /// <summary>
    ///     幂等性记录过期时间 (秒)，默认 24 小时
    /// </summary>
    public int IdempotencyExpireSeconds { get; set; } = 86400;
}