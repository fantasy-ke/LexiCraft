namespace BuildingBlocks.MongoDB.Configuration;

public class MongoOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public bool DisableTracing { get; set; }
    
    /// <summary>
    /// Maximum number of connections in the connection pool (default: 100)
    /// </summary>
    public int MaxConnectionPoolSize { get; set; } = 100;
    
    /// <summary>
    /// Minimum number of connections in the connection pool (default: 0)
    /// </summary>
    public int MinConnectionPoolSize { get; set; } = 0;
    
    /// <summary>
    /// Maximum time a thread can wait for a connection to become available (default: 30 seconds)
    /// </summary>
    public TimeSpan MaxConnectionIdleTime { get; set; } = TimeSpan.FromMinutes(10);
    
    /// <summary>
    /// Maximum lifetime of a connection (default: 30 minutes)
    /// </summary>
    public TimeSpan MaxConnectionLifeTime { get; set; } = TimeSpan.FromMinutes(30);
    
    /// <summary>
    /// Connection timeout (default: 30 seconds)
    /// </summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// Socket timeout (default: 30 seconds)
    /// </summary>
    public TimeSpan SocketTimeout { get; set; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// Server selection timeout (default: 30 seconds)
    /// </summary>
    public TimeSpan ServerSelectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// Enable performance monitoring and metrics collection
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = true;
}
