using BuildingBlocks.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BuildingBlocks.MongoDB.Resilience;

/// <summary>
///     MongoDB 特定的弹性服务实现
/// </summary>
public class MongoResilienceService(
    IMongoClient mongoClient,
    ILogger<MongoResilienceService> logger,
    IOptionsMonitor<ResilienceOptions> options)
    : BaseResilienceService(logger, options)
{
    protected override bool ShouldRetry(Exception exception)
    {
        return exception switch
        {
            // 不应重试的异常
            MongoIncompatibleDriverException => false,
            MongoAuthenticationException => false,

            // 应该重试的 MongoDB 异常
            MongoException => true,

            // 其他应该重试的异常
            TimeoutException => true,

            _ => false
        };
    }

    public override async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 使用 ping 命令检查 MongoDB 连接
            var database = mongoClient.GetDatabase("admin");
            await database.RunCommandAsync<object>("{ ping: 1 }", cancellationToken: cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "MongoDB health check failed");
            return false;
        }
    }
}