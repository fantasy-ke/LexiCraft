using BuildingBlocks.Authentication.Abstractions;
using BuildingBlocks.Authentication.Options;
using Microsoft.Extensions.Options;
using BuildingBlocks.Contexts;

namespace BuildingBlocks.Authentication.Permissions;

/// <summary>
///     Identity 服务的本地权限验证器，使用注册定义和权威用户权限快照完成精确匹配。
/// </summary>
internal sealed class PermissionCheck(
    IUserContext userContext,
    IUserPermissionStore permissionStore,
    IPermissionDefinitionManager permissionDefinitionManager,
    IOptionsMonitor<PermissionAuthorizationOptions> options) : IPermissionCheck
{
    /// <inheritdoc />
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

        if (requiredPermissions.Length == 0)
            return PermissionValidationResult.Allowed;

        var unknownPermissions = requiredPermissions
            .Where(permission => !permissionDefinitionManager.TryGetPermission(permission, out _))
            .ToArray();
        if (unknownPermissions.Length > 0)
            return PermissionValidationResult.Denied(unknownPermissions);

        var currentOptions = options.CurrentValue;
        if (userContext.Roles.Contains(currentOptions.AdministratorRole, StringComparer.OrdinalIgnoreCase))
            return PermissionValidationResult.Allowed;

        var userPermissions = await permissionStore.GetUserPermissionsAsync(
            userContext.UserId,
            cancellationToken);

        var missingPermissions = requiredPermissions
            .Where(permission => !userPermissions.Contains(permission))
            .ToArray();

        return missingPermissions.Length == 0
            ? PermissionValidationResult.Allowed
            : PermissionValidationResult.Denied(missingPermissions);
    }
}
