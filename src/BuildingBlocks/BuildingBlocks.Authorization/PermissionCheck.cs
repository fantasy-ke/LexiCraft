using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Authentication.Permission;
using BuildingBlocks.Extensions.System;

namespace BuildingBlocks.Authentication;

/// <summary>
/// 默认权限检查实现（基于JWT中存储的权限）
/// </summary>
public class PermissionCheck(IUserContext userContext, IPermissionDefinitionManager permissionDefinitionManager)
    : IPermissionCheck
{
    public Task<bool> IsGranted(string permissionName)
    {
        // 注意：这是原始的权限检查实现，基于JWT中存储的权限
        // 在实际应用中，如果JWT中不存储权限，应该使用DatabasePermissionCheck
        
        if (userContext.UserAllPermissions.Length == 0)
            return Task.FromResult(false);
        if (permissionName.IsNullWhiteSpace())
            return Task.FromResult(true);
        
        // 检查用户是否拥有该权限或其任何父权限
        return Task.FromResult(CheckPermission(permissionName));
    }
    
    private bool CheckPermission(string permissionName)
    {
        // 直接匹配用户权限
        if (userContext.UserAllPermissions.Contains(permissionName))
            return true;
            
        // 检查是否有父权限（权限继承）
        var permission = permissionDefinitionManager.GetPermission(permissionName);
        if (permission == null)
            return false;
            
        // 递归检查父权限
        return CheckParentPermission(permission);
    }
    
    private bool CheckParentPermission(PermissionDefinition permission)
    {
        if (permission.Parent == null)
            return false;
            
        // 检查用户是否拥有父权限
        if (userContext.UserAllPermissions.Contains(permission.Parent.Name))
            return true;
            
        // 递归检查祖父权限
        return CheckParentPermission(permission.Parent);
    }
}