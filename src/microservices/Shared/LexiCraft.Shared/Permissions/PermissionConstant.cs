namespace LexiCraft.Shared.Permissions;

public static class PermissionConstant
{
    public const string Admin = "admin";
    public const string User = "user";
    public const string Pages = "Pages";

    /// <summary>
    ///     默认用户权限
    /// </summary>
    public static class DefaultUserPermissions
    {
        public static readonly string[] Permissions =
        [
            Pages,
            IdentityPermissions.Page,
            IdentityPermissions.Users.Query,
            IdentityPermissions.Users.UploadAvatar
        ];
    }
}