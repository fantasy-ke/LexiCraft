using BuildingBlocks.Caching.Internal;
using BuildingBlocks.Caching.Options;

namespace BuildingBlocks.Caching.Redis;

/// <summary>
///     CacheService 内部使用的 Redis 存储边界。
/// </summary>
internal interface IRedisCacheStore
{
    Task<CacheReadResult<T>> GetAsync<T>(
        string key,
        CacheServiceOptions options,
        CancellationToken cancellationToken = default);

    Task SetAsync<T>(
        string key,
        T value,
        CacheServiceOptions options,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveAsync(
        string key,
        CacheServiceOptions options,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string key,
        CacheServiceOptions options,
        CancellationToken cancellationToken = default);

    Task<bool> SetExpirationAsync(
        string key,
        TimeSpan expiry,
        CacheServiceOptions options,
        CancellationToken cancellationToken = default);

    Task<Dictionary<string, string>?> HashGetAsync(
        string key,
        IEnumerable<string> fields,
        CacheServiceOptions options,
        CancellationToken cancellationToken = default);

    Task HashSetAsync(
        string key,
        Dictionary<string, string> values,
        CacheServiceOptions options,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default);
}
