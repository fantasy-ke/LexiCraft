using BuildingBlocks.Authentication.Contract;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BuildingBlocks.Authentication.Permission;

/// <summary>
/// Redis 权限缓存服务实现（仅负责数据操作）
/// </summary>
public class RedisPermissionCacheService(
    IConnectionMultiplexer redis,
    ILogger<RedisPermissionCacheService> logger) : IPermissionCacheService
{
    private readonly IDatabase _database = redis.GetDatabase();
    private readonly IMemoryCache _localCache = new MemoryCache(new MemoryCacheOptions
    {
        SizeLimit = 1000
    });

    private static readonly TimeSpan DefaultLocalCacheExpiration = TimeSpan.FromMinutes(30);

    public async Task<HashSet<string>?> GetUserPermissionsAsync(Guid userId)
    {
        try
        {
            var cacheKey = GetCacheKey(userId);

            // 先查本地缓存
            if (_localCache.TryGetValue<HashSet<string>>(cacheKey, out var localPermissions))
            {
                logger.LogDebug("User permissions found in local cache: {UserId}", userId);
                return localPermissions;
            }

            // 查 Redis 缓存
            var redisValue = await _database.StringGetAsync(cacheKey);
            if (redisValue.HasValue)
            {
                var permissions = Newtonsoft.Json.JsonConvert.DeserializeObject<HashSet<string>>(redisValue!);
                if (permissions != null)
                {
                    // 回写到本地缓存
                    _localCache.Set(cacheKey, permissions, DefaultLocalCacheExpiration);
                    logger.LogDebug("User permissions found in Redis: {UserId}", userId);
                    return permissions;
                }
            }

            logger.LogDebug("User permissions not found: {UserId}", userId);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get user permissions: {UserId}", userId);
            return null;
        }
    }

    public async Task SetUserPermissionsAsync(Guid userId, HashSet<string> permissions, TimeSpan? expiration = null)
    {
        try
        {
            var cacheKey = GetCacheKey(userId);
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(permissions);
            var cacheExpiration = expiration ?? TimeSpan.FromHours(1);

            // 设置 Redis 缓存
            await _database.StringSetAsync(cacheKey, json, cacheExpiration);

            // 设置本地缓存
            _localCache.Set(cacheKey, permissions, DefaultLocalCacheExpiration);

            logger.LogDebug("User permissions set: {UserId}, Count: {Count}", userId, permissions.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to set user permissions: {UserId}", userId);
        }
    }

    public async Task RemoveUserPermissionsAsync(Guid userId)
    {
        try
        {
            var cacheKey = GetCacheKey(userId);

            // 删除 Redis 缓存
            await _database.KeyDeleteAsync(cacheKey);

            // 删除本地缓存
            _localCache.Remove(cacheKey);

            logger.LogDebug("User permissions removed: {UserId}", userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove user permissions: {UserId}", userId);
        }
    }

    public async Task AddPermissionAsync(Guid userId, string permissionName)
    {
        try
        {
            var permissions = await GetUserPermissionsAsync(userId) ?? new HashSet<string>();
            permissions.Add(permissionName);
            await SetUserPermissionsAsync(userId, permissions);

            logger.LogDebug("Permission added to user: {UserId}, Permission: {Permission}", userId, permissionName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add permission: {UserId}, {Permission}", userId, permissionName);
        }
    }

    public async Task RemovePermissionAsync(Guid userId, string permissionName)
    {
        try
        {
            var permissions = await GetUserPermissionsAsync(userId);
            if (permissions != null)
            {
                permissions.Remove(permissionName);
                await SetUserPermissionsAsync(userId, permissions);

                logger.LogDebug("Permission removed from user: {UserId}, Permission: {Permission}", userId, permissionName);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove permission: {UserId}, {Permission}", userId, permissionName);
        }
    }

    private static string GetCacheKey(Guid userId)
    {
        return $"permissions:user:{userId}";
    }
}

