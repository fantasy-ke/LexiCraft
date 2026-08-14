using BuildingBlocks.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BuildingBlocks.MongoDB.Resilience;

public class MongoResilienceService(
    IMongoClient mongoClient,
    ILogger<MongoResilienceService> logger,
    IOptionsMonitor<ResilienceOptions> options)
    : BaseResilienceService(logger, options), IMongoResilienceService
{
    protected override bool ShouldRetry(Exception exception)
    {
        return exception switch
        {
            MongoConnectionException => true,
            MongoExecutionTimeoutException => true,
            MongoException mongoException when
                mongoException.HasErrorLabel("RetryableWriteError") ||
                mongoException.HasErrorLabel("TransientTransactionError") ||
                mongoException.HasErrorLabel("UnknownTransactionCommitResult") => true,
            TimeoutException => true,
            _ => false
        };
    }

    public override async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var database = mongoClient.GetDatabase("admin");
            await database.RunCommandAsync<object>("{ ping: 1 }", cancellationToken: cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "MongoDB health check failed");
            return false;
        }
    }
}
