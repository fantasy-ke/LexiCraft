namespace BuildingBlocks.Authentication.Options;

/// <summary>
///     配置管理员旁路角色以及业务服务调用 Identity 权限验证端点的地址。
/// </summary>
public sealed class PermissionAuthorizationOptions
{
    /// <summary>
    ///     获取或设置可跳过显式权限分配的管理员角色名称；管理员旁路仅对已注册权限生效，未知权限始终拒绝。
    /// </summary>
    public string AdministratorRole { get; set; } = "admin";

    /// <summary>获取或设置 Identity API 的绝对基础地址，可使用 Aspire 服务发现地址。</summary>
    public string IdentityApiBaseAddress { get; set; } = "https+http://fantasy-identity-api";

    /// <summary>获取或设置 Identity 权限验证端点的相对路径。</summary>
    public string IdentityApiValidationPath { get; set; } = "/api/v1/identity/permissions/validate";
}
