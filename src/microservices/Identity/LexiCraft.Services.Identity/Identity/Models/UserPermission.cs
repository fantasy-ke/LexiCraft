using BuildingBlocks.Domain.Internal;
using LexiCraft.Shared.Models;

namespace LexiCraft.Services.Identity.Identity.Models;

/// <summary>
///     用户权限实体
/// </summary>
public class UserPermission : AuditEntity<long>
{
    private UserPermission()
    {
        UserId = UserId.Empty;
    } // EF Core需要的无参构造函数

    public UserPermission(UserId userId, string permissionName)
    {
        UserId = userId;
        PermissionName = permissionName;
    }

    /// <summary>
    ///     用户ID
    /// </summary>
    public UserId UserId { get; private set; }

    /// <summary>
    ///     权限名称
    /// </summary>
    public string PermissionName { get; private set; } = string.Empty;
}