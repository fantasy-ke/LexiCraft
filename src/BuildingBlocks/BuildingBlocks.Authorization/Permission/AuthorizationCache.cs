using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Caching.Abstractions;
using BuildingBlocks.Caching.Configuration;

namespace BuildingBlocks.Authentication.Permission;

public sealed class AuthorizationCache(ICacheService cacheService) : IAuthorizationCache
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        return cacheService.GetAsync<T>(key, Configure, cancellationToken);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        return cacheService.SetAsync(key, value, cacheOptions =>
        {
            Configure(cacheOptions);
            cacheOptions.Expiry = expiration;
        }, cancellationToken);
    }

    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return cacheService.RemoveAsync(key, Configure, cancellationToken);
    }

    private void Configure(CacheServiceOptions cacheOptions)
    {
        cacheOptions.RedisInstanceName = AuthorizationExtensions.AuthorizationRedisInstanceName;
        cacheOptions.UseLocal = false;
        cacheOptions.UseDistributed = true;
        cacheOptions.EnableLock = false;
        cacheOptions.HideErrors = false;
    }
}
