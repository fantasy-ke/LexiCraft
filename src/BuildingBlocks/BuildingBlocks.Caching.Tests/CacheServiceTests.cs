using BuildingBlocks.Caching.Internal;
using BuildingBlocks.Caching.Locking;
using BuildingBlocks.Caching.Options;
using BuildingBlocks.Caching.Redis;
using BuildingBlocks.Caching.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BuildingBlocks.Caching.Tests;

public sealed class CacheServiceTests
{
    [Fact]
    public async Task GetOrSetAsync_PreservesResolvedOptionsDuringLockDoubleCheck()
    {
        var store = new FakeRedisCacheStore();
        store.EnqueueMiss();
        store.EnqueueHit("cached");
        var lockProvider = new SuccessfulLockProvider();
        var service = CreateService(store, lockProvider);
        var factoryCalls = 0;

        var result = await service.GetOrSetAsync(
            "cache-key",
            () =>
            {
                factoryCalls++;
                return Task.FromResult("factory");
            },
            options =>
            {
                options.UseLocal = false;
                options.UseDistributed = true;
                options.EnableLock = true;
                options.RedisInstanceName = "secondary";
            });

        Assert.Equal("cached", result);
        Assert.Equal(0, factoryCalls);
        Assert.Equal("cache-key", lockProvider.LastLockKey);
        Assert.Equal(new[] { "secondary", "secondary" }, store.ObservedInstanceNames);
    }

    [Fact]
    public async Task LocalCache_IsPartitionedByRedisInstance()
    {
        var service = CreateService(new FakeRedisCacheStore(), new SuccessfulLockProvider());

        await service.SetAsync("shared-key", "first", LocalOptions("instance-a"));
        await service.SetAsync("shared-key", "second", LocalOptions("instance-b"));

        Assert.Equal("first", await service.GetAsync<string>("shared-key", LocalOptions("instance-a")));
        Assert.Equal("second", await service.GetAsync<string>("shared-key", LocalOptions("instance-b")));
    }

    [Fact]
    public async Task GetOrSetAsync_ValueTypeMiss_CallsFactory()
    {
        var store = new FakeRedisCacheStore();
        store.EnqueueMiss();
        var service = CreateService(store, new SuccessfulLockProvider());
        var factoryCalls = 0;

        var result = await service.GetOrSetAsync(
            "number-key",
            () =>
            {
                factoryCalls++;
                return Task.FromResult(42);
            },
            options =>
            {
                options.UseLocal = false;
                options.UseDistributed = true;
                options.EnableLock = false;
            });

        Assert.Equal(42, result);
        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    public async Task GetOrSetAsync_ResolvesOptionsOnce()
    {
        var store = new FakeRedisCacheStore();
        store.EnqueueMiss();
        var service = CreateService(store, new SuccessfulLockProvider());
        var configureCalls = 0;

        await service.GetOrSetAsync(
            "cache-key",
            () => Task.FromResult("factory"),
            options =>
            {
                configureCalls++;
                options.UseLocal = false;
                options.UseDistributed = true;
                options.EnableLock = false;
            });

        Assert.Equal(1, configureCalls);
    }

    [Fact]
    public async Task GetOrSetHashAsync_ReadsTimestampUsedForExpiryValidation()
    {
        var store = new FakeRedisCacheStore
        {
            HashValues = new Dictionary<string, string>
            {
                ["value"] = "cached",
                ["cache_timestamp"] = DateTimeOffset.UtcNow.AddHours(-1).ToString("O")
            }
        };
        var service = CreateService(store, new SuccessfulLockProvider());
        var factoryCalls = 0;

        var result = await service.GetOrSetHashAsync(
            "hash-key",
            ["value"],
            values => values["value"],
            () =>
            {
                factoryCalls++;
                return Task.FromResult(new Dictionary<string, string> { ["value"] = "rebuilt" });
            },
            options =>
            {
                options.UseDistributed = true;
                options.EnableLock = false;
                options.Expiry = TimeSpan.FromMinutes(1);
            });

        Assert.Equal("rebuilt", result);
        Assert.Equal(1, factoryCalls);
        Assert.Contains("cache_timestamp", store.ObservedHashFields);
    }

    [Fact]
    public async Task SetAsync_InvalidAdjustedExpiry_UsesConfiguredExpiry()
    {
        var store = new FakeRedisCacheStore();
        var service = CreateService(store, new SuccessfulLockProvider());
        var expectedExpiry = TimeSpan.FromMinutes(10);

        await service.SetAsync(
            "cache-key",
            "value",
            options =>
            {
                options.UseLocal = false;
                options.UseDistributed = true;
                options.Expiry = expectedExpiry;
                options.AdjustExpiryForValue = (_, _) => TimeSpan.Zero;
            });

        Assert.Equal(expectedExpiry, store.LastSetExpiry);
    }

    [Fact]
    public async Task Cancellation_IsNeverHidden()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var store = new FakeRedisCacheStore
        {
            GetException = new OperationCanceledException(cancellationSource.Token)
        };
        var service = CreateService(store, new SuccessfulLockProvider());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync<string>(
            "cache-key",
            options =>
            {
                options.UseLocal = false;
                options.UseDistributed = true;
                options.HideErrors = true;
            },
            cancellationSource.Token));
    }

    private static CacheService CreateService(
        IRedisCacheStore store,
        IDistributedLockProvider lockProvider)
    {
        return new CacheService(
            store,
            new MemoryCache(new MemoryCacheOptions()),
            lockProvider,
            NullLogger<CacheService>.Instance);
    }

    private static Action<CacheServiceOptions> LocalOptions(string instanceName)
    {
        return options =>
        {
            options.UseDistributed = false;
            options.UseLocal = true;
            options.RedisInstanceName = instanceName;
        };
    }

    private sealed class FakeRedisCacheStore : IRedisCacheStore
    {
        private readonly Queue<(bool Found, object? Value)> _getResults = new();

        public List<string?> ObservedInstanceNames { get; } = new();
        public List<string> ObservedHashFields { get; } = new();
        public Exception? GetException { get; init; }
        public Dictionary<string, string>? HashValues { get; init; }
        public TimeSpan? LastSetExpiry { get; private set; }

        public void EnqueueMiss() => _getResults.Enqueue((false, null));

        public void EnqueueHit(object? value) => _getResults.Enqueue((true, value));

        public Task<CacheReadResult<T>> GetAsync<T>(
            string key,
            CacheServiceOptions options,
            CancellationToken cancellationToken = default)
        {
            ObservedInstanceNames.Add(options.RedisInstanceName);
            if (GetException != null)
                return Task.FromException<CacheReadResult<T>>(GetException);

            var result = _getResults.Count > 0 ? _getResults.Dequeue() : (false, null);
            return Task.FromResult(result.Found
                ? CacheReadResult<T>.Hit((T?)result.Value)
                : CacheReadResult<T>.Miss);
        }

        public Task SetAsync<T>(
            string key,
            T value,
            CacheServiceOptions options,
            TimeSpan? expiry = null,
            CancellationToken cancellationToken = default)
        {
            LastSetExpiry = expiry;
            return Task.CompletedTask;
        }

        public Task<bool> RemoveAsync(
            string key,
            CacheServiceOptions options,
            CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<bool> ExistsAsync(
            string key,
            CacheServiceOptions options,
            CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<bool> SetExpirationAsync(
            string key,
            TimeSpan expiry,
            CacheServiceOptions options,
            CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<Dictionary<string, string>?> HashGetAsync(
            string key,
            IEnumerable<string> fields,
            CacheServiceOptions options,
            CancellationToken cancellationToken = default)
        {
            var requestedFields = fields.ToArray();
            ObservedHashFields.AddRange(requestedFields);
            if (HashValues == null)
                return Task.FromResult<Dictionary<string, string>?>(null);

            var result = requestedFields
                .Where(HashValues.ContainsKey)
                .ToDictionary(field => field, field => HashValues[field], StringComparer.Ordinal);
            return Task.FromResult<Dictionary<string, string>?>(result);
        }

        public Task HashSetAsync(
            string key,
            Dictionary<string, string> values,
            CacheServiceOptions options,
            TimeSpan? expiry = null,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class SuccessfulLockProvider : IDistributedLockProvider
    {
        public string? LastLockKey { get; private set; }
        public Task<IDistributedLock?> TryAcquireLockAsync(
            string lockKey,
            TimeSpan lockTimeout,
            TimeSpan acquireTimeout,
            string? redisInstanceName = null,
            CancellationToken cancellationToken = default)
        {
            LastLockKey = lockKey;
            return Task.FromResult<IDistributedLock?>(new FakeDistributedLock(lockKey));
        }

        public async Task<IDistributedLock> AcquireLockAsync(
            string lockKey,
            TimeSpan lockTimeout,
            TimeSpan acquireTimeout,
            string? redisInstanceName = null,
            CancellationToken cancellationToken = default) =>
            await TryAcquireLockAsync(lockKey, lockTimeout, acquireTimeout, redisInstanceName, cancellationToken)
            ?? throw new InvalidOperationException();

        public Task<bool> IsLockHeldAsync(
            string lockKey,
            string? redisInstanceName = null,
            CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<bool> ForceReleaseLockAsync(
            string lockKey,
            string? redisInstanceName = null,
            CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    private sealed class FakeDistributedLock(string lockKey) : IDistributedLock
    {
        public string LockKey { get; } = lockKey;
        public bool IsValid => true;
        public string LockValue => "test-lock";
        public DateTime ExpiresAt => DateTime.UtcNow.AddMinutes(1);
        public Task<bool> ReleaseAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> ExtendAsync(TimeSpan extendBy, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
