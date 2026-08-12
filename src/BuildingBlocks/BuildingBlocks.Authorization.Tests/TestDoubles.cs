using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Caching.DistributedLock;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Authorization.Tests;

internal sealed class TestOptionsMonitor<T>(T currentValue) : IOptionsMonitor<T>
{
    public T CurrentValue { get; } = currentValue;

    public T Get(string? name) => CurrentValue;

    public IDisposable? OnChange(Action<T, string?> listener) => null;
}

internal sealed class TestUserContext : IUserContext
{
    public Guid UserId { get; init; }

    public string UserName { get; init; } = string.Empty;

    public string UserAccount { get; init; } = string.Empty;

    public bool IsAuthenticated { get; init; }

    public string[] Roles { get; init; } = [];
}

internal sealed class TestUserPermissionStore(params string[] permissions) : IUserPermissionStore
{
    private readonly IReadOnlySet<string> _permissions = permissions.ToHashSet(StringComparer.Ordinal);

    public Task<IReadOnlySet<string>> GetUserPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_permissions);
    }
}

internal sealed class TestAuthorizationCache : IAuthorizationCache
{
    private readonly Dictionary<string, object?> _values = new(StringComparer.Ordinal);

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_values.TryGetValue(key, out var value) ? (T?)value : default);
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        _values[key] = value;
        return Task.CompletedTask;
    }

    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_values.Remove(key));
    }
}

internal sealed class TestDistributedLockProvider : IDistributedLockProvider
{
    public string? LastLockKey { get; private set; }

    public string? LastRedisInstanceName { get; private set; }

    public Task<IDistributedLock?> TryAcquireLockAsync(
        string lockKey,
        TimeSpan lockTimeout,
        TimeSpan acquireTimeout,
        string? redisInstanceName = null,
        CancellationToken cancellationToken = default)
    {
        LastLockKey = lockKey;
        LastRedisInstanceName = redisInstanceName;
        return Task.FromResult<IDistributedLock?>(new TestDistributedLock(lockKey, lockTimeout));
    }

    public async Task<IDistributedLock> AcquireLockAsync(
        string lockKey,
        TimeSpan lockTimeout,
        TimeSpan acquireTimeout,
        string? redisInstanceName = null,
        CancellationToken cancellationToken = default)
    {
        return (await TryAcquireLockAsync(
            lockKey,
            lockTimeout,
            acquireTimeout,
            redisInstanceName,
            cancellationToken))!;
    }

    public Task<bool> IsLockHeldAsync(
        string lockKey,
        string? redisInstanceName = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public Task<bool> ForceReleaseLockAsync(
        string lockKey,
        string? redisInstanceName = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}

internal sealed class FailingDistributedLockProvider : IDistributedLockProvider
{
    public Task<IDistributedLock?> TryAcquireLockAsync(
        string lockKey,
        TimeSpan lockTimeout,
        TimeSpan acquireTimeout,
        string? redisInstanceName = null,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Redis is unavailable");
    }

    public Task<IDistributedLock> AcquireLockAsync(
        string lockKey,
        TimeSpan lockTimeout,
        TimeSpan acquireTimeout,
        string? redisInstanceName = null,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Redis is unavailable");
    }

    public Task<bool> IsLockHeldAsync(
        string lockKey,
        string? redisInstanceName = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<bool> ForceReleaseLockAsync(
        string lockKey,
        string? redisInstanceName = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }
}

internal sealed class TestDistributedLock(string lockKey, TimeSpan timeout) : IDistributedLock
{
    public string LockKey { get; } = lockKey;

    public bool IsValid { get; private set; } = true;

    public string LockValue { get; } = Guid.NewGuid().ToString("N");

    public DateTime ExpiresAt { get; } = DateTime.UtcNow.Add(timeout);

    public Task<bool> ReleaseAsync()
    {
        IsValid = false;
        return Task.FromResult(true);
    }

    public Task<bool> ExtendAsync(TimeSpan extendBy) => Task.FromResult(true);

    public async ValueTask DisposeAsync()
    {
        await ReleaseAsync();
    }
}
