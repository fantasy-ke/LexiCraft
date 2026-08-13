using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace BuildingBlocks.Resilience;

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
            var result = await RetryPolicy.ExecuteAsync(
                async token =>
                {
                    token.ThrowIfCancellationRequested();
                    return await operation();
                },
                cancellationToken);

            Logger.LogDebug("Successfully completed operation: {OperationName}", operationName);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Logger.LogDebug("Operation cancelled: {OperationName}", operationName);
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Operation failed after all retries: {OperationName}", operationName);
            throw;
        }
    }

    public virtual Task ExecuteWithRetryAsync(
        Func<Task> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        return ExecuteWithRetryAsync(
            async () =>
            {
                await operation();
                return true;
            },
            operationName,
            cancellationToken);
    }

    public abstract Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

    protected abstract bool ShouldRetry(Exception exception);

    private IAsyncPolicy CreateRetryPolicy()
    {
        return Policy
            .Handle<Exception>(ShouldRetry)
            .WaitAndRetryAsync(Options.RetryCount, GetSleepDuration, OnRetry);
    }

    private TimeSpan GetSleepDuration(int retryAttempt)
    {
        var baseDelay = TimeSpan.FromSeconds(Options.BaseDelaySeconds);
        if (!Options.UseExponentialBackoff) return AddJitter(baseDelay);

        var exponentialDelay = TimeSpan.FromSeconds(
            Math.Min(
                Options.BaseDelaySeconds * Math.Pow(2, retryAttempt - 1),
                Options.MaxDelaySeconds));
        return AddJitter(exponentialDelay);
    }

    private TimeSpan AddJitter(TimeSpan delay)
    {
        if (Options.JitterFactor <= 0) return delay;

        var jitter = delay.TotalMilliseconds * Options.JitterFactor * (Random.Shared.NextDouble() - 0.5);
        return TimeSpan.FromMilliseconds(Math.Max(0, delay.TotalMilliseconds + jitter));
    }

    private void OnRetry(Exception exception, TimeSpan timespan, int retryCount, Context context)
    {
        Logger.LogWarning(
            "Operation retry attempt {RetryCount} in {Delay}ms. Exception: {Exception}",
            retryCount,
            timespan.TotalMilliseconds,
            exception.Message);
    }
}
