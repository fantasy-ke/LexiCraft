using BuildingBlocks.Domain.Internal;

namespace LexiCraft.Services.Identity.Identity.Models;

/// <summary>
/// 用户权限实体
/// </summary>
public class UserPermission : AuditEntity<long>
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; private set; }
    
    /// <summary>
    /// 权限名称
    /// </summary>
    public string PermissionName { get; private set; } = string.Empty;
    
    
    private UserPermission() { } // EF Core需要的无参构造函数
    
    public UserPermission(Guid userId, string permissionName)
    {
        UserId = userId;
        PermissionName = permissionName;
    }
}
