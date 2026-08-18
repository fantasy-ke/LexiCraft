namespace BuildingBlocks.MongoDB.Configuration;

/// <summary>定义 MongoDB 客户端、连接池、超时、追踪和轻量监控选项。</summary>
public class MongoOptions
{
    /// <summary>获取或设置包含数据库名的 MongoDB 连接字符串。</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>获取或设置是否禁用 MongoDB 驱动诊断活动订阅。</summary>
    public bool DisableTracing { get; set; }

    /// <summary>获取或设置连接池最大连接数；必须大于 0。</summary>
    public int MaxConnectionPoolSize { get; set; } = 100;

    /// <summary>获取或设置连接池最小连接数；必须位于 0 与最大连接数之间。</summary>
    public int MinConnectionPoolSize { get; set; }

    /// <summary>获取或设置连接在池中的最大空闲时间。</summary>
    public TimeSpan MaxConnectionIdleTime { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>获取或设置池中连接的最长生命周期。</summary>
    public TimeSpan MaxConnectionLifeTime { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>获取或设置建立服务器连接的超时时间。</summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>获取或设置套接字读写超时时间。</summary>
    public TimeSpan SocketTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>获取或设置服务器选择超时时间。</summary>
    public TimeSpan ServerSelectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>获取或设置是否启用有界的进程内仓储操作指标。</summary>
    public bool EnablePerformanceMonitoring { get; set; } = true;
}
