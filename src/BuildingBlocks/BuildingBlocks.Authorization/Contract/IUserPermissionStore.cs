namespace BuildingBlocks.Authentication.Contract;

/// <summary>
///     Provides Identity.Api with the authoritative permissions assigned to a user.
/// </summary>
public interface IUserPermissionStore
{
    Task<IReadOnlySet<string>> GetUserPermissionsAsync(Guid userId,
        CancellationToken cancellationToken = default);
}