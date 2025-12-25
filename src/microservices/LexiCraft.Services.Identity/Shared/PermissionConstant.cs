using LexiCraft.Services.Identity.Users.Authorization;

namespace LexiCraft.Services.Identity.Shared;

public static class PermissionConstant
{
    public const string Admin = "admin";
    public const string User = "user";
    public const string Pages = "Pages";
    
    /// <summary>
    /// 默认用户权限
    /// </summary>
    public static class DefaultUserPermissions
    {
        public static readonly string[] Permissions =
        [
            Pages, 
            UsersPermissions.Page
        ];
    }
}