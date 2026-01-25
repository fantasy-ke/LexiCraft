namespace BuildingBlocks.MongoDB.Performance;

/// <summary>
///     MongoDB 性能监控接口
/// </summary>
public interface IMongoPerformanceMonitor
{
    /// <summary>
    ///     开始监控操作
    /// </summary>
    IDisposable StartOperation(string operationName, string collectionName);

    /// <summary>
    ///     获取性能指标
    /// </summary>
    Task<PerformanceMetrics> GetMetricsAsync(TimeSpan? period = null);
}

/// <summary>
///     性能指标
/// </summary>
public class PerformanceMetrics
{
    /// <summary>
    ///     总操作数
    /// </summary>
    public int TotalOperations { get; set; }

    /// <summary>
    ///     平均响应时间
    /// </summary>
    public TimeSpan AverageResponseTime { get; set; }

    /// <summary>
    ///     最大响应时间
    /// </summary>
    public TimeSpan MaxResponseTime { get; set; }

    /// <summary>
    ///     最小响应时间
    /// </summary>
    public TimeSpan MinResponseTime { get; set; }

    /// <summary>
    ///     每秒操作数
    /// </summary>
    public double OperationsPerSecond { get; set; }

    /// <summary>
    ///     慢操作数量（>200ms）
    /// </summary>
    public int SlowOperations { get; set; }

    /// <summary>
    ///     按集合分组的操作统计
    /// </summary>
    public Dictionary<string, int> OperationsByCollection { get; set; } = new();

    /// <summary>
    ///     按操作类型分组的统计
    /// </summary>
    public Dictionary<string, int> OperationsByType { get; set; } = new();
}

/// <summary>
///     操作指标
/// </summary>
internal class OperationMetric
{
    public string OperationName { get; set; } = string.Empty;
    public string CollectionName { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public DateTime Timestamp { get; set; }
}