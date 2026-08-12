using System.Net;
using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Caching.DistributedLock;

namespace BuildingBlocks.Authentication.Permission;

public sealed class RedisAuthorizationSynchronization(
    IDistributedLockProvider distributedLockProvider) : IAuthorizationSynchronization
{
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan AcquireTimeout = TimeSpan.FromSeconds(5);

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