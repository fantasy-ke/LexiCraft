using BuildingBlocks.Caching.Internal;
using BuildingBlocks.Caching.Locking;
using BuildingBlocks.Caching.Options;
using BuildingBlocks.Caching.Redis;
using BuildingBlocks.Caching.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
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

    [Fact]
    public async Task GetAsync_DistributedHit_PopulatesLocalCacheWithLocalExpiry()
    {
        var store = new FakeRedisCacheStore();
        store.EnqueueHit("distributed");
        var memoryCache = new RecordingMemoryCache();
        var service = CreateService(store, new SuccessfulLockProvider(), memoryCache);
        var localExpiry = TimeSpan.FromMinutes(4);

        var configure = HybridOptions("secondary", TimeSpan.FromHours(1), localExpiry);
        var first = await service.GetAsync<string>("cache-key", configure);
        var second = await service.GetAsync<string>("cache-key", configure);

        Assert.Equal("distributed", first);
        Assert.Equal("distributed", second);
        Assert.Equal(1, store.GetCalls);
        Assert.Equal(localExpiry, memoryCache.GetExpiry("local:secondary:cache-key"));
    }

    [Fact]
    public async Task SetAsync_WritesDistributedAndLocalCachesWithIndependentExpiry()
    {
        var store = new FakeRedisCacheStore();
        var memoryCache = new RecordingMemoryCache();
        var service = CreateService(store, new SuccessfulLockProvider(), memoryCache);
        var distributedExpiry = TimeSpan.FromMinutes(30);
        var localExpiry = TimeSpan.FromMinutes(3);

        await service.SetAsync(
            "cache-key",
            "value",
            HybridOptions("secondary", distributedExpiry, localExpiry));

        Assert.Equal(1, store.SetCalls);
        Assert.Equal("value", store.LastSetValue);
        Assert.Equal(distributedExpiry, store.LastSetExpiry);
        Assert.Equal(localExpiry, memoryCache.GetExpiry("local:secondary:cache-key"));
    }

    [Fact]
    public async Task RemoveAsync_RemovesLocalCacheWhenDistributedRemovalMisses()
    {
        var store = new FakeRedisCacheStore { RemoveResult = false };
        var memoryCache = new RecordingMemoryCache();
        var service = CreateService(store, new SuccessfulLockProvider(), memoryCache);
        await service.SetAsync("cache-key", "value", LocalOptions("secondary"));

        var removed = await service.RemoveAsync(
            "cache-key",
            HybridOptions("secondary", TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(3)));

        Assert.False(removed);
        Assert.Equal(1, store.RemoveCalls);
        Assert.Contains("local:secondary:cache-key", memoryCache.RemovedKeys);
        Assert.False(memoryCache.Contains("local:secondary:cache-key"));
    }

    [Fact]
    public async Task ExistsAsync_LocalHit_DoesNotQueryDistributedCache()
    {
        var store = new FakeRedisCacheStore { ExistsResult = false };
        var memoryCache = new RecordingMemoryCache();
        var service = CreateService(store, new SuccessfulLockProvider(), memoryCache);
        await service.SetAsync("cache-key", "value", LocalOptions("secondary"));

        var exists = await service.ExistsAsync(
            "cache-key",
            HybridOptions("secondary", TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(3)));

        Assert.True(exists);
        Assert.Equal(0, store.ExistsCalls);
    }

    [Fact]
    public async Task SetExpirationAsync_ForwardsExplicitExpiryAndRedisInstance()
    {
        var store = new FakeRedisCacheStore { SetExpirationResult = true };
        var service = CreateService(store, new SuccessfulLockProvider());
        var expiry = TimeSpan.FromMinutes(12);

        var result = await service.SetExpirationAsync(
            "cache-key",
            expiry,
            options =>
            {
                options.UseDistributed = true;
                options.RedisInstanceName = "secondary";
            });

        Assert.True(result);
        Assert.Equal(expiry, store.LastExpiration);
        Assert.Equal("secondary", store.LastExpirationInstanceName);
    }

    [Fact]
    public async Task GetOrSetAsync_LockFailure_FallsBackToFactoryAndCachesValue()
    {
        var store = new FakeRedisCacheStore();
        store.EnqueueMiss();
        var service = CreateService(store, new FailingLockProvider());
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
                options.FallbackToFactory = true;
            });

        Assert.Equal("factory", result);
        Assert.Equal(1, factoryCalls);
        Assert.Equal(1, store.SetCalls);
        Assert.Equal("factory", store.LastSetValue);
    }

    [Fact]
    public async Task GetOrSetAsync_LockFailure_UsesDefaultFallbackWithoutCallingFactory()
    {
        var store = new FakeRedisCacheStore();
        store.EnqueueMiss();
        var service = CreateService(store, new FailingLockProvider());
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
                options.FallbackToFactory = false;
                options.FallbackToDefault = true;
                options.DefaultValue = "fallback";
            });

        Assert.Equal("fallback", result);
        Assert.Equal(0, factoryCalls);
        Assert.Equal(0, store.SetCalls);
    }

    [Fact]
    public async Task GetAsync_WhenErrorsAreVisible_WrapsStoreException()
    {
        var dependencyException = new InvalidOperationException("redis unavailable");
        var store = new FakeRedisCacheStore { GetException = dependencyException };
        var service = CreateService(store, new SuccessfulLockProvider());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAsync<string>(
            "cache-key",
            options =>
            {
                options.UseLocal = false;
                options.UseDistributed = true;
                options.HideErrors = false;
            }));

        Assert.Same(dependencyException, exception.InnerException);
    }

    private static CacheService CreateService(
        IRedisCacheStore store,
        IDistributedLockProvider lockProvider,
        IMemoryCache? memoryCache = null)
    {
        return new CacheService(
            store,
            memoryCache ?? new MemoryCache(new MemoryCacheOptions()),
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


    private static Action<CacheServiceOptions> HybridOptions(
        string instanceName,
        TimeSpan distributedExpiry,
        TimeSpan localExpiry)
    {
        return options =>
        {
            options.UseDistributed = true;
            options.UseLocal = true;
            options.RedisInstanceName = instanceName;
            options.Expiry = distributedExpiry;
            options.LocalExpiry = localExpiry;
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
        public object? LastSetValue { get; private set; }
        public int GetCalls { get; private set; }
        public int SetCalls { get; private set; }
        public int RemoveCalls { get; private set; }
        public int ExistsCalls { get; private set; }
        public bool RemoveResult { get; init; }
        public bool ExistsResult { get; init; }
        public bool SetExpirationResult { get; init; }
        public TimeSpan? LastExpiration { get; private set; }
        public string? LastExpirationInstanceName { get; private set; }

        public void EnqueueMiss() => _getResults.Enqueue((false, null));

        public void EnqueueHit(object? value) => _getResults.Enqueue((true, value));

        public Task<CacheReadResult<T>> GetAsync<T>(
            string key,
            CacheServiceOptions options,
            CancellationToken cancellationToken = default)
        {
            GetCalls++;
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
            SetCalls++;
            LastSetValue = value;
            LastSetExpiry = expiry;
            return Task.CompletedTask;
        }

        public Task<bool> RemoveAsync(
            string key,
            CacheServiceOptions options,
            CancellationToken cancellationToken = default)
        {
            RemoveCalls++;
            return Task.FromResult(RemoveResult);
        }

        public Task<bool> ExistsAsync(
            string key,
            CacheServiceOptions options,
            CancellationToken cancellationToken = default)
        {
            ExistsCalls++;
            return Task.FromResult(ExistsResult);
        }

        public Task<bool> SetExpirationAsync(
            string key,
            TimeSpan expiry,
            CacheServiceOptions options,
            CancellationToken cancellationToken = default)
        {
            LastExpiration = expiry;
            LastExpirationInstanceName = options.RedisInstanceName;
            return Task.FromResult(SetExpirationResult);
        }

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

    private sealed class FailingLockProvider : IDistributedLockProvider
    {
        public Task<IDistributedLock?> TryAcquireLockAsync(
            string lockKey,
            TimeSpan lockTimeout,
            TimeSpan acquireTimeout,
            string? redisInstanceName = null,
            CancellationToken cancellationToken = default) => Task.FromResult<IDistributedLock?>(null);

        public Task<IDistributedLock> AcquireLockAsync(
            string lockKey,
            TimeSpan lockTimeout,
            TimeSpan acquireTimeout,
            string? redisInstanceName = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> IsLockHeldAsync(
            string lockKey,
            string? redisInstanceName = null,
            CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<bool> ForceReleaseLockAsync(
            string lockKey,
            string? redisInstanceName = null,
            CancellationToken cancellationToken = default) => Task.FromResult(false);
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

    private sealed class RecordingMemoryCache : IMemoryCache
    {
        private readonly Dictionary<object, RecordedEntry> _entries = new();

        public List<object> RemovedKeys { get; } = new();

        public ICacheEntry CreateEntry(object key) => new RecordingCacheEntry(key, Save);

        public void Remove(object key)
        {
            RemovedKeys.Add(key);
            _entries.Remove(key);
        }

        public bool TryGetValue(object key, out object? value)
        {
            if (_entries.TryGetValue(key, out var entry))
            {
                value = entry.Value;
                return true;
            }

            value = null;
            return false;
        }

        public TimeSpan? GetExpiry(object key) => _entries[key].AbsoluteExpirationRelativeToNow;

        public bool Contains(object key) => _entries.ContainsKey(key);

        public void Dispose()
        {
        }

        private void Save(RecordingCacheEntry entry)
        {
            _entries[entry.Key] = new RecordedEntry(entry.Value, entry.AbsoluteExpirationRelativeToNow);
        }

        private sealed record RecordedEntry(object? Value, TimeSpan? AbsoluteExpirationRelativeToNow);
    }

    private sealed class RecordingCacheEntry(object key, Action<RecordingCacheEntry> save) : ICacheEntry
    {
        public object Key { get; } = key;
        public object? Value { get; set; }
        public DateTimeOffset? AbsoluteExpiration { get; set; }
        public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }
        public IList<IChangeToken> ExpirationTokens { get; } = new List<IChangeToken>();
        public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; } = new List<PostEvictionCallbackRegistration>();
        public CacheItemPriority Priority { get; set; }
        public long? Size { get; set; }

        public void Dispose() => save(this);
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
