namespace BuildingBlocks.Authentication.Contract;

/// <summary>
///     业务服务发给 Identity API 的权限验证请求。
/// </summary>
/// <param name="Permissions">需要同时满足的权限名称。</param>
public sealed record PermissionValidationRequest(string[] Permissions);

/// <summary>
///     区分“无权限”“会话失效”和“验证服务不可用”的权限验证结果。
/// </summary>
/// <param name="Granted">是否授予访问。</param>
/// <param name="SessionValid">当前登录会话是否有效。</param>
/// <param name="ServiceAvailable">权限验证依赖是否可用。</param>
/// <param name="MissingPermissions">拒绝访问时缺少的权限。</param>
public sealed record PermissionValidationResult(
    bool Granted,
    bool SessionValid,
    bool ServiceAvailable,
    string[] MissingPermissions)
{
    /// <summary>会话有效且满足全部权限。</summary>
    public static PermissionValidationResult Allowed { get; } = new(true, true, true, []);

    /// <summary>访问令牌已不属于当前有效会话。</summary>
    public static PermissionValidationResult InvalidSession { get; } = new(false, false, true, []);

    /// <summary>验证依赖不可用，调用方应返回 503 而不是 403。</summary>
    public static PermissionValidationResult Unavailable { get; } = new(false, true, false, []);

    /// <summary>
    ///     创建缺少指定权限的拒绝结果。
    /// </summary>
    /// <param name="permissions">缺少的权限名称。</param>
    /// <returns>规范化后的拒绝结果。</returns>
    public static PermissionValidationResult Denied(IEnumerable<string> permissions)
    {
        return new PermissionValidationResult(
            false,
            true,
            true,
            permissions.Where(permission => !string.IsNullOrWhiteSpace(permission))
                .Distinct(StringComparer.Ordinal)
                .ToArray());
    }
}