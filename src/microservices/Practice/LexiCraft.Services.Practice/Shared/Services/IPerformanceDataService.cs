namespace LexiCraft.Services.Practice.Shared.Services;

/// <summary>
/// 性能数据聚合和发布处理服务
/// </summary>
public interface IPerformanceDataService
{
    /// <summary>
    /// 发布用户最近练习会话的性能数据
    /// </summary>
    Task PublishRecentPerformanceDataAsync(Guid userId, DateTime? fromDate = null);
}