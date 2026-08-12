using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Authentication.Shared;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Authentication;

/// <summary>
///     Local permission validation used by Identity.Api.
/// </summary>
public sealed class PermissionCheck(
    IUserContext userContext,
    IUserPermissionStore permissionStore,
    IPermissionDefinitionManager permissionDefinitionManager,
    IOptionsMonitor<PermissionAuthorizationOptions> options) : IPermissionCheck
{
    public async Task<PermissionValidationResult> CheckAsync(
        IReadOnlyCollection<string> permissionNames,
        CancellationToken cancellationToken = default)
    {
        var requiredPermissions = permissionNames
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (!userContext.IsAuthenticated || userContext.UserId == Guid.Empty)
            return PermissionValidationResult.InvalidSession;

        var currentOptions = options.CurrentValue;
        if (requiredPermissions.Length == 0 ||
            userContext.Roles.Contains(currentOptions.AdministratorRole, StringComparer.OrdinalIgnoreCase))
        {
            return PermissionValidationResult.Allowed;
        }

        var userPermissions = await permissionStore.GetUserPermissionsAsync(
            userContext.UserId,
            cancellationToken);

        var missingPermissions = requiredPermissions
            .Where(permission => !permissionDefinitionManager.TryGetPermission(permission, out _) ||
                                 !userPermissions.Contains(permission))
            .ToArray();

        return missingPermissions.Length == 0
            ? PermissionValidationResult.Allowed
            : PermissionValidationResult.Denied(missingPermissions);
    }
}
