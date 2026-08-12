using BuildingBlocks.Authentication.Contract;
using LexiCraft.Services.Identity.Shared.Contracts;
using LexiCraft.Shared.Models;

namespace LexiCraft.Services.Identity.Shared.Authorization;

public sealed class IdentityUserPermissionStore(
    IUserPermissionRepository userPermissionRepository,
    IPermissionCache permissionCache,
    IAuthorizationSynchronization authorizationSynchronization) : IUserPermissionStore
{
    public async Task<IReadOnlySet<string>> GetUserPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var cachedPermissions = await permissionCache.GetUserPermissionsAsync(userId, cancellationToken);
        if (cachedPermissions != null)
            return cachedPermissions;

        return await authorizationSynchronization.ExecuteAsync(
            $"permission:{userId:N}",
            async token =>
            {
                cachedPermissions = await permissionCache.GetUserPermissionsAsync(userId, token);
                if (cachedPermissions != null)
                    return cachedPermissions;

                var permissions = await userPermissionRepository.GetUserPermissionsAsync(
                    new UserId(userId),
                    token);
                var permissionSet = permissions.ToHashSet(StringComparer.Ordinal);

                await permissionCache.SetUserPermissionsAsync(
                    userId,
                    permissionSet,
                    cancellationToken: token);

                return permissionSet;
            },
            cancellationToken);
    }
}
