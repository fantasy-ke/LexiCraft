using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace BuildingBlocks.Resilience;

/// <summary>
/// 基础弹性服务实现，提供通用的重试策略
/// </summary>
public abstract class BaseResilienceService : IResilienceService
{
    protected readonly ILogger Logger;
    protected readonly ResilienceOptions Options;
    protected readonly IAsyncPolicy RetryPolicy;

    protected BaseResilienceService(ILogger logger, IOptionsMonitor<ResilienceOptions> options)
    {
        Logger = logger;
		Options = options.CurrentValue;
        RetryPolicy = CreateRetryPolicy();
    }

    public virtual async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation, 
        string operationName, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogDebug("Executing operation with retry: {OperationName}", operationName);
            
            var result = await RetryPolicy.ExecuteAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await operation();
            });

            Logger.LogDebug("Successfully completed operation: {OperationName}", operationName);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Operation failed after all retries: {OperationName}", operationName);
            throw;
        }
    }

    public virtual async Task ExecuteWithRetryAsync(
        Func<Task> operation, 
        string operationName, 
        CancellationToken cancellationToken = default)
    {
        await ExecuteWithRetryAsync(async () =>
        {
            await operation();
            return true; // 为void操作返回虚拟值
        }, operationName, cancellationToken);
    }

    public abstract Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建重试策略，子类可以重写以自定义异常处理
    /// </summary>
    protected IAsyncPolicy CreateRetryPolicy()
    {
        var policyBuilder = Policy.Handle<Exception>(ShouldRetry);

        return policyBuilder.WaitAndRetryAsync(
            retryCount: Options.RetryCount,
            sleepDurationProvider: GetSleepDuration,
            onRetry: OnRetry);
    }

    /// <summary>
    /// 判断异常是否应该重试，子类可以重写
    /// </summary>
    protected abstract bool ShouldRetry(Exception exception);

    /// <summary>
    /// 获取重试延迟时间
    /// </summary>
    private TimeSpan GetSleepDuration(int retryAttempt)
    {
        var baseDelay = TimeSpan.FromSeconds(Options.BaseDelaySeconds);
        
        if (!Options.UseExponentialBackoff)
        {
            return AddJitter(baseDelay);
        }

        var exponentialDelay = TimeSpan.FromSeconds(
            Math.Min(Options.BaseDelaySeconds * Math.Pow(2, retryAttempt - 1), Options.MaxDelaySeconds));
        
        return AddJitter(exponentialDelay);
    }

    /// <summary>
    /// 添加抖动以避免雷群效应
    /// </summary>
    private TimeSpan AddJitter(TimeSpan delay)
    {
        if (Options.JitterFactor <= 0) return delay;

        var random = new Random();
        var jitter = delay.TotalMilliseconds * Options.JitterFactor * (random.NextDouble() - 0.5);
        var jitteredDelay = delay.TotalMilliseconds + jitter;
        
        return TimeSpan.FromMilliseconds(Math.Max(0, jitteredDelay));
    }

    /// <summary>
    /// 重试回调
    /// </summary>
    private void OnRetry(Exception exception, TimeSpan timespan, int retryCount, Context context)
    {
        Logger.LogWarning(
            "Operation retry attempt {RetryCount} in {Delay}ms. Exception: {Exception}",
            retryCount, timespan.TotalMilliseconds, exception.Message);
    }
}
