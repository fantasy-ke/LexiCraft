namespace BuildingBlocks.MongoDB.Configuration;

public class MongoOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public bool DisableTracing { get; set; }
    
    /// <summary>
    /// 连接池中的最大连接数（默认值：100）
    /// </summary>
    public int MaxConnectionPoolSize { get; set; } = 100;
    
    /// <summary>
    /// 连接池中的最小连接数（默认值：0）
    /// </summary>
    public int MinConnectionPoolSize { get; set; } = 0;
    
    /// <summary>
    /// 线程等待连接可用的最大时间（默认值：30秒）
    /// </summary>
    public TimeSpan MaxConnectionIdleTime { get; set; } = TimeSpan.FromMinutes(10);
    
    /// <summary>
    /// 连接的最大生存时间（默认值：30分钟）
    /// </summary>
    public TimeSpan MaxConnectionLifeTime { get; set; } = TimeSpan.FromMinutes(30);
    
    /// <summary>
    /// 连接超时时间（默认值：30秒）
    /// </summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// Socket超时时间（默认值：30秒）
    /// </summary>
    public TimeSpan SocketTimeout { get; set; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// 服务器选择超时时间（默认值：30秒）
    /// </summary>
    public TimeSpan ServerSelectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// 启用性能监控和指标收集
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = true;
}
