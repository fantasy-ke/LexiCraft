using BuildingBlocks.Authentication.Abstractions;
using BuildingBlocks.Caching.Abstractions;
using BuildingBlocks.Caching.Options;

namespace BuildingBlocks.Authentication.Redis.Caching;

/// <summary>
///     基于 <c>BuildingBlocks.Caching</c> 的纯分布式授权缓存，禁用进程内缓存并显式暴露 Redis 错误。
/// </summary>
internal sealed class AuthorizationCache(ICacheService cacheService) : IAuthorizationCache
{
    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        return cacheService.GetAsync<T>(key, Configure, cancellationToken);
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        return cacheService.SetAsync(key, value, cacheOptions =>
        {
            Configure(cacheOptions);
            cacheOptions.Expiry = expiration;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return cacheService.RemoveAsync(key, Configure, cancellationToken);
    }

    private void Configure(CacheServiceOptions cacheOptions)
    {
        cacheOptions.RedisInstanceName = AuthorizationRedisExtensions.AuthorizationRedisInstanceName;
        cacheOptions.UseLocal = false;
        cacheOptions.UseDistributed = true;
        cacheOptions.EnableLock = false;
        cacheOptions.HideErrors = false;
    }
}
