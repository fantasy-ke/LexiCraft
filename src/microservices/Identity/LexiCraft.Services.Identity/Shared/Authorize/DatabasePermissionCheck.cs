using BuildingBlocks.Authentication;
using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Authentication.Permission;
using BuildingBlocks.Extensions.System;
using BuildingBlocks.Caching.Redis;
using LexiCraft.Services.Identity.Shared.Contracts;

namespace LexiCraft.Services.Identity.Shared.Authorize;

/// <summary>
/// 基于数据库的权限检查实现
/// </summary>
public class DatabasePermissionCheck(
    IUserContext userContext,
    IPermissionDefinitionManager permissionDefinitionManager,
    IUserPermissionRepository userPermissionRepository,
    ICacheManager cacheManager)
    : IPermissionCheck
{
    public async Task<bool> IsGranted(string permissionName)
    {
        // 如果用户未认证，直接拒绝
        if (!userContext.IsAuthenticated)
            return false;

        // 如果没有指定权限名称，默认允许
        if (permissionName.IsNullWhiteSpace())
            return true;

        // 获取用户所有权限（包括继承的权限）
        var userPermissions = await GetUserAllPermissionsAsync(userContext.UserId);
        
        // 检查用户是否拥有该权限或其任何父权限
        return CheckPermission(userPermissions, permissionName);
    }

    private async Task<HashSet<string>> GetUserAllPermissionsAsync(Guid userId)
    {
        var cacheKey = CacheKeys.GetUserPermissionByUserId(userId);
        // 尝试从缓存获取
        var cachedPermissions = await cacheManager.GetAsync<string[]>(cacheKey);
        
        if (cachedPermissions != null && cachedPermissions.Any())
            return [..cachedPermissions];

        // 缓存未命中，从数据库获取
        var directPermissions = await userPermissionRepository.GetUserPermissionsAsync(userId);
        var allPermissions = new HashSet<string>(directPermissions);

        // 添加继承的权限
        foreach (var permission in directPermissions)
        {
            AddInheritedPermissions(allPermissions, permission);
        }

        // 存储到缓存，有效期1小时
        await cacheManager.SetAsync(cacheKey, allPermissions.ToArray(), TimeSpan.FromHours(1));

        return allPermissions;
    }

    private void AddInheritedPermissions(HashSet<string> permissions, string permissionName)
    {
        var permission = permissionDefinitionManager.GetPermission(permissionName);
        if (permission?.Parent == null) return;
        permissions.Add(permission.Parent.Name);
        AddInheritedPermissions(permissions, permission.Parent.Name);
    }

    private bool CheckPermission(HashSet<string> userPermissions, string permissionName)
    {
        // 直接匹配用户权限
        if (userPermissions.Contains(permissionName))
            return true;

        // 检查是否有父权限（权限继承）
        var permission = permissionDefinitionManager.GetPermission(permissionName);
        return permission != null &&
               // 递归检查父权限
               CheckParentPermission(userPermissions, permission);
    }

    private bool CheckParentPermission(HashSet<string> userPermissions, PermissionDefinition permission)
    {
        if (permission.Parent == null)
            return false;

        // 检查用户是否拥有父权限
        return userPermissions.Contains(permission.Parent.Name) ||
               // 递归检查祖父权限
               CheckParentPermission(userPermissions, permission.Parent);
    }
}