using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Authentication.Permission;
using BuildingBlocks.Extensions.System;

namespace BuildingBlocks.Authentication;

/// <summary>
/// Redis权限检查实现（基于Redis缓存的权限）
/// </summary>
public class PermissionCheck(
    IUserContext userContext,
    IPermissionCache permissionCache,
    IPermissionDefinitionManager permissionDefinitionManager)
    : IPermissionCheck
{
    public async Task<bool> IsGranted(string permissionName)
    {
        // 如果用户未认证，直接拒绝
        if (!userContext.IsAuthenticated)
            return false;

        // 如果没有指定权限名称，默认允许
        if (permissionName.IsNullWhiteSpace() || userContext.UserName.ToLower() == "admin")
            return true;

        // 获取用户所有权限（包含继承的权限）
        var userPermissions = await GetUserAllPermissionsAsync(userContext.UserId);
        return userPermissions.Count != 0 &&
               // 检查权限
               CheckPermission(userPermissions, permissionName);
    }

    /// <summary>
    /// 获取用户所有权限（包含继承的权限）
    /// </summary>
    private async Task<HashSet<string>> GetUserAllPermissionsAsync(Guid userId)
    {
        var userPermissions = await permissionCache.GetUserPermissionsAsync(userId);
        if (userPermissions == null)
        {
            return new HashSet<string>();
        }

        // 添加继承的权限
        var allPermissions = new HashSet<string>(userPermissions);
        foreach (var permission in userPermissions)
        {
            AddInheritedPermissions(allPermissions, permission);
        }

        return allPermissions;
    }

    /// <summary>
    /// 添加继承的权限
    /// </summary>
    private void AddInheritedPermissions(HashSet<string> permissions, string permissionName)
    {
        var permission = permissionDefinitionManager.GetPermission(permissionName);
        if (permission?.Parent == null) return;

        permissions.Add(permission.Parent.Name);
        AddInheritedPermissions(permissions, permission.Parent.Name);
    }

    /// <summary>
    /// 检查用户是否拥有指定权限
    /// </summary>
    private bool CheckPermission(HashSet<string> userPermissions, string permissionName)
    {
        // 直接匹配用户权限
        if (userPermissions.Contains(permissionName))
            return true;

        // 检查是否有父权限（权限继承）
        var permission = permissionDefinitionManager.GetPermission(permissionName);
        if (permission == null)
            return false;

        return CheckParentPermission(userPermissions, permission);
    }

    /// <summary>
    /// 递归检查父权限
    /// </summary>
    private bool CheckParentPermission(HashSet<string> userPermissions, PermissionDefinition permission)
    {
        if (permission.Parent == null)
            return false;

        return userPermissions.Contains(permission.Parent.Name) ||
               CheckParentPermission(userPermissions, permission.Parent);
    }
}
