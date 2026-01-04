using BuildingBlocks.Authentication.Permission;
using LexiCraft.Shared.Permissions;

namespace LexiCraft.Services.Identity.Permissions.Authorization;

/// <summary>
/// 权限管理权限定义提供程序
/// </summary>
public class PermissionsPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(PermissionDefinitionContext context)
    {
        // 创建根页面权限
        var pages = context.GetPermissionOrNull(PermissionConstant.Pages) ??
                    context.CreatePermission(PermissionConstant.Pages, "页面访问", "所有页面访问权限");

        // 创建权限管理页面权限
        var permissionsPage = pages.GetChildOrNull(PermissionsPermissions.Page) ??
                              pages.CreateChildPermission(PermissionsPermissions.Page, "权限管理", "权限管理相关权限");

        // 创建具体的子权限
        permissionsPage.CreateChildPermission(PermissionsPermissions.Query, "查询权限", "允许查询用户权限");
        permissionsPage.CreateChildPermission(PermissionsPermissions.Create, "新增权限", "允许为用户添加权限");
        permissionsPage.CreateChildPermission(PermissionsPermissions.Delete, "删除权限", "允许删除用户权限");
        permissionsPage.CreateChildPermission(PermissionsPermissions.Update, "修改权限", "允许批量修改用户权限");
    }
}
