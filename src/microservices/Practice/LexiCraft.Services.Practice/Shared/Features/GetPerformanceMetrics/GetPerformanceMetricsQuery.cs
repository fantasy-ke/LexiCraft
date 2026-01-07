using MediatR;

namespace LexiCraft.Services.Practice.Shared.Features.GetPerformanceMetrics;

/// <summary>
/// 获取性能指标的查询
/// </summary>
public class GetPerformanceMetricsQuery : IRequest<PerformanceMetricsResponse>
{
    /// <summary>
    /// 时间段（分钟）
    /// </summary>
    public int PeriodMinutes { get; set; } = 5;
}