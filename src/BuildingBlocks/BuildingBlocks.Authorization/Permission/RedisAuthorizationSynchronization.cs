using System.Net;
using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Caching.DistributedLock;

namespace BuildingBlocks.Authentication.Permission;

/// <summary>
///     使用授权 Redis 的分布式锁串行化同一用户的会话切换和权限缓存填充/失效操作。
/// </summary>
public sealed class RedisAuthorizationSynchronization(
    IDistributedLockProvider distributedLockProvider) : IAuthorizationSynchronization
{
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan AcquireTimeout = TimeSpan.FromSeconds(5);

    /// <inheritdoc />
    public async Task<TResult> ExecuteAsync<TResult>(
        string resource,
        Func<CancellationToken, Task<TResult>> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);
        ArgumentNullException.ThrowIfNull(action);

        IDistributedLock distributedLock;
        try
        {
            distributedLock = await distributedLockProvider.AcquireLockAsync(
                $"authorization:{resource}",
                LockTimeout,
                AcquireTimeout,
                AuthorizationExtensions.AuthorizationRedisInstanceName,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new HttpRequestException(
                "Authorization Redis synchronization is unavailable",
                exception,
                HttpStatusCode.ServiceUnavailable);
        }

        await using (distributedLock)
            return await action(cancellationToken);
    }
}