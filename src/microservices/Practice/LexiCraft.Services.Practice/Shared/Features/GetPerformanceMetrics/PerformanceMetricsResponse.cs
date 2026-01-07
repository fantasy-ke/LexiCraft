namespace LexiCraft.Services.Practice.Shared.Features.GetPerformanceMetrics;

/// <summary>
/// 性能指标响应模型
/// </summary>
public class PerformanceMetricsResponse
{
    /// <summary>
    /// 指标聚合的时间段（分钟）
    /// </summary>
    public int PeriodMinutes { get; set; }
    
    /// <summary>
    /// 时间段内数据库操作总数
    /// </summary>
    public int TotalOperations { get; set; }
    
    /// <summary>
    /// 平均响应时间（毫秒）
    /// </summary>
    public double AverageResponseTimeMs { get; set; }
    
    /// <summary>
    /// 最大响应时间（毫秒）
    /// </summary>
    public double MaxResponseTimeMs { get; set; }
    
    /// <summary>
    /// 最小响应时间（毫秒）
    /// </summary>
    public double MinResponseTimeMs { get; set; }
    
    /// <summary>
    /// 每秒操作数
    /// </summary>
    public double OperationsPerSecond { get; set; }
    
    /// <summary>
    /// 慢操作数量（>200毫秒）
    /// </summary>
    public int SlowOperations { get; set; }
    
    /// <summary>
    /// 慢操作百分比
    /// </summary>
    public double SlowOperationPercentage { get; set; }
    
    /// <summary>
    /// 按MongoDB集合统计的操作数
    /// </summary>
    public Dictionary<string, int> OperationsByCollection { get; set; } = new();
    
    /// <summary>
    /// 按操作类型统计的操作数（查找、插入、更新、删除）
    /// </summary>
    public Dictionary<string, int> OperationsByType { get; set; } = new();
    
    /// <summary>
    /// 指标生成时间戳
    /// </summary>
    public DateTime Timestamp { get; set; }
}