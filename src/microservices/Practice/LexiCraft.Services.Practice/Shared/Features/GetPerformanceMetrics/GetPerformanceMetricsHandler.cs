using BuildingBlocks.MongoDB.Performance;
using MediatR;

namespace LexiCraft.Services.Practice.Shared.Features.GetPerformanceMetrics;

public class GetPerformanceMetricsHandler : IRequestHandler<GetPerformanceMetricsQuery, PerformanceMetricsResponse>
{
    private readonly IMongoPerformanceMonitor _performanceMonitor;

    public GetPerformanceMetricsHandler(IMongoPerformanceMonitor performanceMonitor)
    {
        _performanceMonitor = performanceMonitor;
    }

    public async Task<PerformanceMetricsResponse> Handle(GetPerformanceMetricsQuery request, CancellationToken cancellationToken)
    {
        var period = TimeSpan.FromMinutes(request.PeriodMinutes);
        var metrics = await _performanceMonitor.GetMetricsAsync(period);
        
        return new PerformanceMetricsResponse
        {
            PeriodMinutes = request.PeriodMinutes,
            TotalOperations = metrics.TotalOperations,
            AverageResponseTimeMs = metrics.AverageResponseTime.TotalMilliseconds,
            MaxResponseTimeMs = metrics.MaxResponseTime.TotalMilliseconds,
            MinResponseTimeMs = metrics.MinResponseTime.TotalMilliseconds,
            OperationsPerSecond = metrics.OperationsPerSecond,
            SlowOperations = metrics.SlowOperations,
            SlowOperationPercentage = metrics.TotalOperations > 0 
                ? (double)metrics.SlowOperations / metrics.TotalOperations * 100 
                : 0,
            OperationsByCollection = metrics.OperationsByCollection,
            OperationsByType = metrics.OperationsByType,
            Timestamp = DateTime.UtcNow
        };
    }
}