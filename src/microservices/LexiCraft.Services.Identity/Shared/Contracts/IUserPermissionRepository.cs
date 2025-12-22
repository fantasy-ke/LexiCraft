using BuildingBlocks.Domain;
using LexiCraft.Services.Identity.Identity.Models;

namespace LexiCraft.Services.Identity.Shared.Contracts;

/// <summary>
/// 用户权限仓储接口
/// </summary>
public interface IUserPermissionRepository : IRepository<UserPermission>
{
    /// <summary>
    /// 获取用户的所有权限
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<List<string>> GetUserPermissionsAsync(Guid userId);
    
    /// <summary>
    /// 为用户添加权限
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="permissionName"></param>
    /// <returns></returns>
    Task AddUserPermissionAsync(Guid userId, string permissionName);
    
    /// <summary>
    /// 为用户批量添加权限
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="permissionNames"></param>
    /// <returns></returns>
    Task AddUserPermissionsAsync(Guid userId, IEnumerable<string> permissionNames);
    
    /// <summary>
    /// 移除用户的权限
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="permissionName"></param>
    /// <returns></returns>
    Task RemoveUserPermissionAsync(Guid userId, string permissionName);
    
    /// <summary>
    /// 移除用户的所有权限
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task RemoveAllUserPermissionsAsync(Guid userId);
}