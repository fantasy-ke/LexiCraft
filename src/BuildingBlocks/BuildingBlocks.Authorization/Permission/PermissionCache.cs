using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Caching.Abstractions;
using BuildingBlocks.Caching.Configuration;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Authentication.Permission;

/// <summary>
///     Redis 权限缓存服务实现（使用通用缓存服务）
/// </summary>
public class RedisPermissionCache(
    ICacheService cacheService,
    ILogger<RedisPermissionCache> logger)
    : IPermissionCache
{
    private static readonly TimeSpan DefaultLocalCacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan DefaultRedisCacheExpiration = TimeSpan.FromHours(1);

    public async Task<HashSet<string>?> GetUserPermissionsAsync(Guid userId)
    {
        var cacheKey = GetCacheKey(userId);

        try
        {
            var permissions = await cacheService.GetAsync<HashSet<string>>(cacheKey, ConfigureOptions);

            if (permissions != null)
                logger.LogDebug("User permissions found in cache: {UserId}", userId);
            else
                logger.LogDebug("User permissions not found in cache: {UserId}", userId);

            return permissions;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get user permissions: {UserId}", userId);
            return null;
        }
    }

    public async Task SetUserPermissionsAsync(Guid userId, HashSet<string> permissions, TimeSpan? expiration = null)
    {
        var cacheKey = GetCacheKey(userId);
        var cacheExpiration = expiration ?? DefaultRedisCacheExpiration;

        try
        {
            await cacheService.SetAsync(cacheKey, permissions, options =>
            {
                ConfigureOptions(options);
                options.Expiry = cacheExpiration;
            });

            logger.LogDebug("User permissions set: {UserId}, Count: {Count}", userId, permissions.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to set user permissions: {UserId}", userId);
            throw;
        }
    }

    public async Task RemoveUserPermissionsAsync(Guid userId)
    {
        var cacheKey = GetCacheKey(userId);

        try
        {
            await cacheService.RemoveAsync(cacheKey, ConfigureOptions);
            logger.LogDebug("User permissions removed: {UserId}", userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove user permissions: {UserId}", userId);
            throw;
        }
    }

    public async Task AddPermissionAsync(Guid userId, string permissionName)
    {
        try
        {
            var permissions = await GetUserPermissionsAsync(userId) ?? new HashSet<string>();
            if (permissions.Add(permissionName))
            {
                await SetUserPermissionsAsync(userId, permissions);
                logger.LogDebug("Permission added to user: {UserId}, Permission: {Permission}", userId, permissionName);
            }
            else
            {
                logger.LogDebug("Permission already exists: {UserId}, Permission: {Permission}", userId,
                    permissionName);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add permission: {UserId}, {Permission}", userId, permissionName);
            throw;
        }
    }

    public async Task AddPermissionsAsync(Guid userId, IEnumerable<string> permissionNames)
    {
        try
        {
            var permissions = await GetUserPermissionsAsync(userId) ?? new HashSet<string>();
            var permissionsArray = permissionNames as string[] ?? permissionNames.ToArray();

            var addedCount = 0;
            foreach (var permissionName in permissionsArray)
                if (permissions.Add(permissionName))
                    addedCount++;

            if (addedCount > 0)
            {
                await SetUserPermissionsAsync(userId, permissions);
                logger.LogDebug("Permissions added to user: {UserId}, Added: {AddedCount}, Total: {TotalCount}",
                    userId, addedCount, permissionsArray.Length);
            }
            else
            {
                logger.LogDebug("No new permissions to add: {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add permissions: {UserId}", userId);
            throw;
        }
    }

    public async Task RemovePermissionAsync(Guid userId, string permissionName)
    {
        try
        {
            var permissions = await GetUserPermissionsAsync(userId);
            if (permissions != null && permissions.Remove(permissionName))
            {
                await SetUserPermissionsAsync(userId, permissions);
                logger.LogDebug("Permission removed from user: {UserId}, Permission: {Permission}", userId,
                    permissionName);
            }
            else
            {
                logger.LogDebug("Permission not found or already removed: {UserId}, Permission: {Permission}", userId,
                    permissionName);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove permission: {UserId}, {Permission}", userId, permissionName);
            throw;
        }
    }

    public async Task RemovePermissionsAsync(Guid userId, List<string> permissionNames)
    {
        try
        {
            var permissions = await GetUserPermissionsAsync(userId);
            if (permissions != null)
            {
                var removedCount = permissionNames.Count(permissions.Remove);

                if (removedCount > 0)
                {
                    await SetUserPermissionsAsync(userId, permissions);
                    logger.LogDebug(
                        "Permissions removed from user: {UserId}, Removed: {RemovedCount}, Total: {TotalCount}",
                        userId, removedCount, permissionNames.Count);
                }
                else
                {
                    logger.LogDebug("No permissions to remove: {UserId}", userId);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove permissions: {UserId}", userId);
            throw;
        }
    }

    private static string GetCacheKey(Guid userId)
    {
        return $"permissions:user:{userId:N}";
    }

    private void ConfigureOptions(CacheServiceOptions options)
    {
        options.RedisInstanceName = "OAuthRedis";
        options.UseLocal = true;
        options.UseDistributed = true;
        options.Expiry = DefaultRedisCacheExpiration;
        options.LocalExpiry = DefaultLocalCacheExpiration;
        options.HideErrors = false; // 不隐藏异常，由上层处理
    }
}