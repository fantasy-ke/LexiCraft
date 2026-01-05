using BuildingBlocks.Authentication.Permission;
using LexiCraft.Shared.Permissions;

namespace LexiCraft.Services.Identity.Shared.Authorization;

/// <summary>
/// 权限管理权限定义提供程序
/// </summary>
public class IdentityDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(PermissionDefinitionContext context)
    {
        // 获取用户页面权限节点
        var usersPage = context.GetPermissionOrNull(IdentityPermissions.Page) ??
                        context.CreatePermission(IdentityPermissions.Page, "用户管理", "用户管理相关权限");

        // 创建用户管理子节点
        var usersNode = usersPage.GetChildOrNull(IdentityPermissions.Users.Default) ??
                        usersPage.CreateChildPermission(IdentityPermissions.Users.Default, "用户管理", "用户管理相关权限");

        // 创建权限管理子节点
        var permissionsNode = usersPage.GetChildOrNull(IdentityPermissions.Permissions.Default) ??
                              usersPage.CreateChildPermission(IdentityPermissions.Permissions.Default, "权限管理", "权限管理相关权限");

        // 为用户管理节点添加子权限
        usersNode.CreateChildPermission(IdentityPermissions.Users.Query, "查询用户", "允许查询用户信息");
        usersNode.CreateChildPermission(IdentityPermissions.Users.Create, "创建用户", "允许创建新用户");
        usersNode.CreateChildPermission(IdentityPermissions.Users.Edit, "编辑用户", "允许编辑用户信息");
        usersNode.CreateChildPermission(IdentityPermissions.Users.Delete, "删除用户", "允许删除用户");
        usersNode.CreateChildPermission(IdentityPermissions.Users.UploadAvatar, "上传头像", "允许上传用户头像");

        // 为权限管理节点添加子权限
        permissionsNode.CreateChildPermission(IdentityPermissions.Permissions.Query, "查询权限", "允许查询用户权限");
        permissionsNode.CreateChildPermission(IdentityPermissions.Permissions.Create, "新增权限", "允许为用户添加权限");
        permissionsNode.CreateChildPermission(IdentityPermissions.Permissions.Delete, "删除权限", "允许删除用户权限");
        permissionsNode.CreateChildPermission(IdentityPermissions.Permissions.Update, "修改权限", "允许批量修改用户权限");
    }
}