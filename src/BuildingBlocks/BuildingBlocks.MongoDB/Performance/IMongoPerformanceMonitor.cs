namespace BuildingBlocks.MongoDB.Performance;

/// <summary>定义 MongoDB 仓储操作的轻量进程内性能监控端口。</summary>
public interface IMongoPerformanceMonitor
{
    /// <summary>开始计时一次仓储操作。</summary>
    /// <param name="operationName">用于聚合的操作名称。</param>
    /// <param name="collectionName">目标集合名称。</param>
    /// <returns>释放时停止计时并记录指标的句柄；监控关闭时返回空操作句柄。</returns>
    IDisposable StartOperation(string operationName, string collectionName);

    /// <summary>汇总指定时间窗口内的进程内指标。</summary>
    /// <param name="period">回溯时间窗口；为空时使用最近 5 分钟。</param>
    /// <returns>性能指标快照。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="period"/> 小于或等于零时抛出。</exception>
    Task<PerformanceMetrics> GetMetricsAsync(TimeSpan? period = null);
}

/// <summary>表示指定时间窗口内的 MongoDB 仓储性能指标快照。</summary>
public class PerformanceMetrics
{
    /// <summary>获取或设置窗口内总操作数。</summary>
    public int TotalOperations { get; set; }

    /// <summary>获取或设置平均响应时间。</summary>
    public TimeSpan AverageResponseTime { get; set; }

    /// <summary>获取或设置最大响应时间。</summary>
    public TimeSpan MaxResponseTime { get; set; }

    /// <summary>获取或设置最小响应时间。</summary>
    public TimeSpan MinResponseTime { get; set; }

    /// <summary>获取或设置按所选完整窗口计算的每秒操作数。</summary>
    public double OperationsPerSecond { get; set; }

    /// <summary>获取或设置耗时超过 200 毫秒的操作数。</summary>
    public int SlowOperations { get; set; }

    /// <summary>获取或设置按集合名称分组的操作数。</summary>
    public Dictionary<string, int> OperationsByCollection { get; set; } = new();

    /// <summary>获取或设置按操作名称分组的操作数。</summary>
    public Dictionary<string, int> OperationsByType { get; set; } = new();
}

/// <summary>保存单次仓储操作的内部计时数据；仅保留进程内且受容量上限约束。</summary>
internal class OperationMetric
{
    /// <summary>操作名称。</summary>
    public string OperationName { get; set; } = string.Empty;

    /// <summary>集合名称。</summary>
    public string CollectionName { get; set; } = string.Empty;

    /// <summary>操作耗时。</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>记录完成时的 UTC 时间。</summary>
    public DateTime Timestamp { get; set; }
}
