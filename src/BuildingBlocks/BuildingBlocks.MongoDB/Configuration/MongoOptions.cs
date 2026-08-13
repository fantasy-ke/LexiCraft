namespace BuildingBlocks.MongoDB.Configuration;

public class MongoOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public bool DisableTracing { get; set; }
    public int MaxConnectionPoolSize { get; set; } = 100;
    public int MinConnectionPoolSize { get; set; }
    public TimeSpan MaxConnectionIdleTime { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan MaxConnectionLifeTime { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan SocketTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan ServerSelectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool EnablePerformanceMonitoring { get; set; } = true;
}