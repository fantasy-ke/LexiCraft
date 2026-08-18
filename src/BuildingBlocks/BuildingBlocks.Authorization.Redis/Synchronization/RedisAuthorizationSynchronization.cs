using System.Net;
using BuildingBlocks.Authentication.Abstractions;
using BuildingBlocks.Caching.Locking;

namespace BuildingBlocks.Authentication.Redis.Synchronization;

/// <summary>
///     使用授权 Redis 的分布式锁串行化同一用户的会话切换和权限缓存填充/失效操作。
/// </summary>
internal sealed class RedisAuthorizationSynchronization(
    IDistributedLockProvider distributedLockProvider) : IAuthorizationSynchronization
{
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan AcquireTimeout = TimeSpan.FromSeconds(5);

    /// <inheritdoc />
    /// <exception cref="ArgumentException"><paramref name="resource"/> 为空或仅包含空白字符时抛出。</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="HttpRequestException">Redis 锁依赖不可用或在限定时间内无法获取锁时抛出，状态码为 503。</exception>
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
                AuthorizationRedisExtensions.AuthorizationRedisInstanceName,
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