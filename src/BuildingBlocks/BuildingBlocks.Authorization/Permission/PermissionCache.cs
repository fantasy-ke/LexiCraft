using System.Net;
using BuildingBlocks.Authentication.Contract;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Authentication.Permission;

/// <summary>
///     Stores complete permission snapshots in Redis without a process-local cache.
/// </summary>
public sealed class RedisPermissionCache(
    IAuthorizationCache authorizationCache,
    ILogger<RedisPermissionCache> logger) : IPermissionCache
{
    private static readonly TimeSpan DefaultRedisCacheExpiration = TimeSpan.FromMinutes(1);

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
