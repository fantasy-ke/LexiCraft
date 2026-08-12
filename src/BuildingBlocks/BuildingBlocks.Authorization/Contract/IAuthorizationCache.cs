namespace BuildingBlocks.Authentication.Contract;

/// <summary>
///     Shared distributed cache used for access-token sessions and permission snapshots.
/// </summary>
public interface IAuthorizationCache
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    Task SetAsync<T>(string key, T value, TimeSpan expiration,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);
}

public sealed record AccessTokenCacheEntry(string AccessTokenHash, string RefreshTokenHash);