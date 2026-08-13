using BuildingBlocks.MongoDB.Resilience;
using BuildingBlocks.Resilience;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BuildingBlocks.Persistence.Tests;

public class MongoResilienceServiceTests
{
    [Fact]
    public async Task Generic_MongoException_is_not_retried()
    {
        var service = CreateService(retryCount: 2);
        var attempts = 0;

        await Assert.ThrowsAsync<MongoException>(() => service.ExecuteWithRetryAsync<int>(
            () =>
            {
                attempts++;
                throw new MongoException("non-transient");
            },
            "non-transient"));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task TimeoutException_is_retried_using_configured_count()
    {
        var service = CreateService(retryCount: 2);
        var attempts = 0;

        await Assert.ThrowsAsync<TimeoutException>(() => service.ExecuteWithRetryAsync<int>(
            () =>
            {
                attempts++;
                throw new TimeoutException("transient");
            },
            "transient"));

        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task Pre_cancelled_operation_does_not_invoke_delegate()
    {
        var service = CreateService(retryCount: 2);
        var attempts = 0;
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ExecuteWithRetryAsync(
            () =>
            {
                attempts++;
                return Task.FromResult(1);
            },
            "cancelled",
            cancellationSource.Token));

        Assert.Equal(0, attempts);
    }

    private static MongoResilienceService CreateService(int retryCount)
    {
        var options = new ResilienceOptions
        {
            RetryCount = retryCount,
            BaseDelaySeconds = 0,
            MaxDelaySeconds = 0,
            JitterFactor = 0
        };

        return new MongoResilienceService(
            new MongoClient("mongodb://localhost/test"),
            NullLogger<MongoResilienceService>.Instance,
            new StaticOptionsMonitor<ResilienceOptions>(options));
    }

    private sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue { get; } = value;

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
