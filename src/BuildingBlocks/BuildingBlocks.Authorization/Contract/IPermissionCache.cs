namespace BuildingBlocks.Authentication.Contract;

/// <summary>
///     Distributed cache for Identity.Api user-permission snapshots.
/// </summary>
public interface IPermissionCache
{
    Task<HashSet<string>?> GetUserPermissionsAsync(Guid userId,
        CancellationToken cancellationToken = default);

    Task SetUserPermissionsAsync(Guid userId, HashSet<string> permissions,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    Task RemoveUserPermissionsAsync(Guid userId,
        CancellationToken cancellationToken = default);
}