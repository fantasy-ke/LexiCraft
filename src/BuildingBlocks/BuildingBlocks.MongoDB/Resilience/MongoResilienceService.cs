using BuildingBlocks.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BuildingBlocks.MongoDB.Resilience;

/// <summary>为 MongoDB 事务外读取提供瞬时故障判断，并提供 admin ping 健康检查。</summary>
/// <param name="mongoClient">共享 MongoDB 客户端。</param>
/// <param name="logger">弹性与健康检查日志记录器。</param>
/// <param name="options">可动态读取的通用弹性选项。</param>
/// <remarks>
///     即使异常标签包含可重试写入或事务错误，当前仓储也只在事务外读取路径调用本服务；
///     写命令依赖驱动 retryable writes，并仍要求业务幂等、唯一索引或完整事务重试。
/// </remarks>
public class MongoResilienceService(
    IMongoClient mongoClient,
    ILogger<MongoResilienceService> logger,
    IOptionsMonitor<ResilienceOptions> options)
    : BaseResilienceService(logger, options), IMongoResilienceService
{
    /// <inheritdoc />
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

    /// <summary>向 MongoDB <c>admin</c> 数据库发送 ping 命令。</summary>
    /// <param name="cancellationToken">用于取消健康检查的令牌。</param>
    /// <returns>ping 成功时为 <see langword="true"/>；非取消异常时记录警告并返回 <see langword="false"/>。</returns>
    /// <exception cref="OperationCanceledException">调用方请求取消时重新抛出。</exception>
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
