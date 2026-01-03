using BuildingBlocks.Authentication.Permission;
using LexiCraft.Shared.Permissions;

namespace LexiCraft.Services.Identity.Users.Authorization;

/// <summary>
/// 验证服务权限定义提供程序
/// </summary>
public class UsersPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(PermissionDefinitionContext context)
    {
        // 创建根页面权限
        var pages = context.GetPermissionOrNull(PermissionConstant.Pages) ??
                    context.CreatePermission(PermissionConstant.Pages, "页面访问", "所有页面访问权限");
                    
        // 创建用户服务页面权限
        var usersPage = pages.GetChildOrNull(UsersPermissions.Page) ??
                              pages.CreateChildPermission(UsersPermissions.Page, "用户服务", "用户服务相关权限");
                              
        // 创建具体的子权限
        usersPage.CreateChildPermission(UsersPermissions.Create, "创建用户", "允许生成用户");
        usersPage.CreateChildPermission(UsersPermissions.Edit, "编辑用户设置", "允许编辑用户设置");
        usersPage.CreateChildPermission(UsersPermissions.Query, "查询用户记录", "允许查询用户记录");
        usersPage.CreateChildPermission(UsersPermissions.Delete, "删除用户记录", "允许删除用户记录");
        usersPage.CreateChildPermission(UsersPermissions.UploadAvatar, "上传用户头像", "允许上传用户头像");
    }
}