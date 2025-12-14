namespace LexiCraft.AuthServer.Domain;

public static class RoleConstant
{
    public const string Admin = "admin";
    public const string User = "user";
    
    /// <summary>
    /// 默认用户权限
    /// </summary>
    public static class DefaultUserPermissions
    {
        public static readonly string[] Permissions =
        [
            "Pages", 
            "Pages.User",
            "Pages.User.UploadAvatar"
        ];
    }
}