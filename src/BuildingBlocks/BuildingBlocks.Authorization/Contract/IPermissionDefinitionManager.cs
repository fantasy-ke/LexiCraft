using System.Collections.Immutable;
using BuildingBlocks.Authentication.Permission;

namespace BuildingBlocks.Authentication.Contract;

/// <summary>
/// 权限定义管理器接口
/// </summary>
public interface IPermissionDefinitionManager
{
    /// <summary>
    /// 获取根权限列表
    /// </summary>
    /// <returns></returns>
    ImmutableList<PermissionDefinition> GetRootPermissions();
    
    /// <summary>
    /// 获取所有权限定义
    /// </summary>
    /// <returns></returns>
    ImmutableList<PermissionDefinition> GetPermissions();
    
    /// <summary>
    /// 根据名称获取权限定义
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    PermissionDefinition? GetPermission(string name);
    
    /// <summary>
    /// 尝试根据名称获取权限定义
    /// </summary>
    /// <param name="name"></param>
    /// <param name="permission"></param>
    /// <returns></returns>
    bool TryGetPermission(string name, out PermissionDefinition? permission);
}