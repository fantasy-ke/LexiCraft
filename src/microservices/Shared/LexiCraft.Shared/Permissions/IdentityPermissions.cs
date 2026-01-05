namespace LexiCraft.Shared.Permissions;

/// <summary>
/// 用户服务相关权限定义
/// </summary>
public static class IdentityPermissions
{
    /// <summary>
    /// 身份认证模块页面访问权限
    /// </summary>
    public const string Page = "Pages.Identity";

    /// <summary>
    /// 用户管理相关权限
    /// </summary>
    public static class Users
    {
        /// <summary>
        /// 用户管理页面访问权限
        /// </summary>
        public const string Default = "Pages.Identity.Users";
        
        /// <summary>
        /// 查询用户列表权限
        /// </summary>
        public const string Query = "Pages.Identity.Users.Query";
        
        /// <summary>
        /// 创建用户权限
        /// </summary>
        public const string Create = "Pages.Identity.Users.Create";
        
        /// <summary>
        /// 编辑用户权限
        /// </summary>
        public const string Edit = "Pages.Identity.Users.Edit";
        
        /// <summary>
        /// 删除用户权限
        /// </summary>
        public const string Delete = "Pages.Identity.Users.Delete";
        
        /// <summary>
        /// 上传用户头像权限
        /// </summary>
        public const string UploadAvatar = "Pages.Identity.Users.UploadAvatar";
    }

    /// <summary>
    /// 权限管理相关权限
    /// </summary>
    public static class Permissions
    {
        /// <summary>
        /// 权限管理页面访问权限
        /// </summary>
        public const string Default = "Pages.Identity.Permissions";
        
        /// <summary>
        /// 查询权限列表权限
        /// </summary>
        public const string Query = "Pages.Identity.Permissions.Query";
        
        /// <summary>
        /// 创建权限权限
        /// </summary>
        public const string Create = "Pages.Identity.Permissions.Create";
        
        /// <summary>
        /// 更新权限权限
        /// </summary>
        public const string Update = "Pages.Identity.Permissions.Update";
        
        /// <summary>
        /// 删除权限权限
        /// </summary>
        public const string Delete = "Pages.Identity.Permissions.Delete";
    }
}