using BuildingBlocks.Resilience;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LexiCraft.Services.Practice.Shared.HealthChecks;

public class MongoDbHealthCheck : IHealthCheck
{
    private readonly IResilienceService _resilienceService;

    public MongoDbHealthCheck(IResilienceService resilienceService)
    {
        _resilienceService = resilienceService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var isHealthy = await _resilienceService.IsHealthyAsync(cancellationToken);
            
            if (isHealthy)
            {
                return HealthCheckResult.Healthy("MongoDB connection is healthy");
            }
            
            return HealthCheckResult.Unhealthy("MongoDB connection is not responding");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB health check failed", ex);
        }
    }
}