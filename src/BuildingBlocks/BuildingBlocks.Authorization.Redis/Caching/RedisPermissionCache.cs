using System.Net;
using BuildingBlocks.Authentication.Abstractions;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Authentication.Redis.Caching;

/// <summary>
///     在 Redis 中缓存用户完整权限快照，不使用进程内缓存；默认有效期为一分钟。
/// </summary>
internal sealed class RedisPermissionCache(
    IAuthorizationCache authorizationCache,
    ILogger<RedisPermissionCache> logger) : IPermissionCache
{
    private static readonly TimeSpan DefaultRedisCacheExpiration = TimeSpan.FromMinutes(1);

    /// <inheritdoc />
    public async Task<HashSet<string>?> GetUserPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var permissions = await authorizationCache.GetAsync<HashSet<string>>(
                GetCacheKey(userId),
                cancellationToken);

            return permissions == null
                ? null
                : new HashSet<string>(permissions, StringComparer.Ordinal);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to get user permissions from Redis: {UserId}", userId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetUserPermissionsAsync(
        Guid userId,
        HashSet<string> permissions,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await authorizationCache.SetAsync(
                GetCacheKey(userId),
                new HashSet<string>(permissions, StringComparer.Ordinal),
                expiration ?? DefaultRedisCacheExpiration,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to set user permissions in Redis: {UserId}", userId);
        }
    }

    /// <inheritdoc />
    /// <exception cref="HttpRequestException">Redis 缓存失效失败时抛出，状态码为 503；调用方必须中止权限变更。</exception>
    public async Task RemoveUserPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await authorizationCache.RemoveAsync(GetCacheKey(userId), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to remove user permissions from Redis: {UserId}", userId);
            throw new HttpRequestException(
                "Authorization Redis permission cache is unavailable",
                exception,
                HttpStatusCode.ServiceUnavailable);
        }
    }

    private static string GetCacheKey(Guid userId)
    {
        return $"permissions:user:{userId:N}";
    }
}
