namespace LexiCraft.AuthServer.Application.Contract.Users.Authorization;

/// <summary>
/// 验证服务相关权限定义
/// </summary>
public static class UsersPermissions
{
    /// <summary>
    /// 页面访问权限
    /// </summary>
    public const string Page = "Pages.Users";
    
    /// <summary>
    /// 创建验证码权限
    /// </summary>
    public const string Create = "Pages.Users.Create";
    
    /// <summary>
    /// 编辑验证设置权限
    /// </summary>
    public const string Edit = "Pages.Users.Edit";
    
    /// <summary>
    /// 查询权限
    /// </summary>
    public const string Query = "Pages.Users.Query";
    
    /// <summary>
    /// 删除权限
    /// </summary>
    public const string Delete = "Pages.Users.Delete";
    
    /// <summary>
    /// 上传头像权限
    /// </summary>
    public const string UploadAvatar = "Pages.Users.UploadAvatar";
}