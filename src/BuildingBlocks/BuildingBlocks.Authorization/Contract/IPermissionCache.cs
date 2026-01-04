namespace BuildingBlocks.Authentication.Contract;

/// <summary>
/// 权限缓存服务接口（仅负责数据操作）
/// </summary>
public interface IPermissionCache
{
    /// <summary>
    /// 获取用户权限集合
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>权限集合</returns>
    Task<HashSet<string>?> GetUserPermissionsAsync(Guid userId);

    /// <summary>
    /// 设置用户权限集合
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissions">权限集合</param>
    /// <param name="expiration">过期时间</param>
    Task SetUserPermissionsAsync(Guid userId, HashSet<string> permissions, TimeSpan? expiration = null);

    /// <summary>
    /// 删除用户权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    Task RemoveUserPermissionsAsync(Guid userId);

    /// <summary>
    /// 添加单个权限到用户权限集合
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionName">权限名称</param>
    Task AddPermissionAsync(Guid userId, string permissionName);

    /// <summary>
    /// 批量添加权限到用户权限集合
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionNames">权限名称列表</param>
    Task AddPermissionsAsync(Guid userId, IEnumerable<string> permissionNames);

    /// <summary>
    /// 从用户权限集合中移除单个权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionName">权限名称</param>
    Task RemovePermissionAsync(Guid userId, string permissionName);

    /// <summary>
    /// 从用户权限集合中批量移除权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionNames">权限名称列表</param>
    Task RemovePermissionsAsync(Guid userId, List<string> permissionNames);
}

