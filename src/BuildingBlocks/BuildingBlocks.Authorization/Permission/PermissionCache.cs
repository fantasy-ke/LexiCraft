using System.Text.Json;
using BuildingBlocks.Authentication.Contract;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BuildingBlocks.Authentication.Permission;

/// <summary>
/// Redis 权限缓存服务实现（仅负责数据操作）
/// </summary>
public class RedisPermissionCache(
    IConnectionMultiplexer redis,
    ILogger<RedisPermissionCache> logger) : IPermissionCache
{
    private readonly IDatabase _database = redis.GetDatabase();
    private readonly IMemoryCache _localCache = new MemoryCache(new MemoryCacheOptions
    {
        SizeLimit = 1000,
        CompactionPercentage = 0.25 // 当达到限制时压缩 25%
    });

    private static readonly TimeSpan DefaultLocalCacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan DefaultRedisCacheExpiration = TimeSpan.FromHours(1);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<HashSet<string>?> GetUserPermissionsAsync(Guid userId)
    {
        var cacheKey = GetCacheKey(userId);

        try
        {
            // 使用 GetOrCreateAsync 简化本地缓存逻辑
            return await _localCache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SetAbsoluteExpiration(DefaultLocalCacheExpiration);
                entry.SetSize(1);

                // 查询 Redis 缓存
                var redisValue = await _database.StringGetAsync(cacheKey);
                if (redisValue.HasValue)
                {
                    var permissions = JsonSerializer.Deserialize<HashSet<string>>(redisValue.ToString(), JsonOptions);
                    if (permissions != null)
                    {
                        logger.LogDebug("User permissions found in Redis: {UserId}", userId);
                        return permissions;
                    }
                }

                logger.LogDebug("User permissions not found: {UserId}", userId);
                return null;
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get user permissions: {UserId}", userId);
            
            // 发生异常时清除可能损坏的本地缓存
            _localCache.Remove(cacheKey);
            return null;
        }
    }

    public async Task SetUserPermissionsAsync(Guid userId, HashSet<string> permissions, TimeSpan? expiration = null)
    {
        var cacheKey = GetCacheKey(userId);
        var cacheExpiration = expiration ?? DefaultRedisCacheExpiration;

        try
        {
            var json = JsonSerializer.Serialize(permissions, JsonOptions);

            // 并行设置 Redis 和本地缓存
            var redisTask = _database.StringSetAsync(cacheKey, json, cacheExpiration);
            
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(DefaultLocalCacheExpiration)
                .SetSize(1)
                .SetPriority(CacheItemPriority.Normal);
            
            _localCache.Set(cacheKey, permissions, cacheEntryOptions);

            await redisTask;

            logger.LogDebug("User permissions set: {UserId}, Count: {Count}", userId, permissions.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to set user permissions: {UserId}", userId);
            
            // 发生异常时清除可能不一致的缓存
            _localCache.Remove(cacheKey);
            throw;
        }
    }

    public async Task RemoveUserPermissionsAsync(Guid userId)
    {
        var cacheKey = GetCacheKey(userId);

        try
        {
            // 先删除本地缓存，避免读取到旧数据
            _localCache.Remove(cacheKey);

            // 删除 Redis 缓存
            await _database.KeyDeleteAsync(cacheKey);

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
            var permissions = await GetUserPermissionsAsync(userId) ?? [];
            if (permissions.Add(permissionName))
            {
                await SetUserPermissionsAsync(userId, permissions);
                logger.LogDebug("Permission added to user: {UserId}, Permission: {Permission}", userId, permissionName);
            }
            else
            {
                logger.LogDebug("Permission already exists: {UserId}, Permission: {Permission}", userId, permissionName);
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
            var permissions = await GetUserPermissionsAsync(userId) ?? [];
            var permissionsArray = permissionNames as string[] ?? permissionNames.ToArray();
            
            var addedCount = 0;
            foreach (var permissionName in permissionsArray)
            {
                if (permissions.Add(permissionName))
                {
                    addedCount++;
                }
            }

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
                logger.LogDebug("Permission removed from user: {UserId}, Permission: {Permission}", userId, permissionName);
            }
            else
            {
                logger.LogDebug("Permission not found or already removed: {UserId}, Permission: {Permission}", userId, permissionName);
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
                    logger.LogDebug("Permissions removed from user: {UserId}, Removed: {RemovedCount}, Total: {TotalCount}", 
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
}

