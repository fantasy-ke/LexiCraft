namespace BuildingBlocks.Authentication.Redis.Keys;

/// <summary>
///     Identity 会话写入方与验证方共享的授权 Redis 键模板。
/// </summary>
/// <remarks>
///     新会话只使用 <c>authorization:v2:*</c> 键；旧格式常量仅用于迁移识别，不应作为新写入目标。
///     格式化用户键时应使用无连字符的 <c>N</c> 格式，令牌索引只接受令牌摘要，不得传入明文访问令牌或刷新令牌。
/// </remarks>
public static class AuthorizationRedisKeys
{
    /// <summary>历史全量用户权限键名称；保留用于兼容迁移，不用于当前按用户缓存。</summary>
    public const string UserAllPermissions = "User_All_Permissions";

    /// <summary>旧访问令牌会话键模板；参数为 <c>N</c> 格式用户标识，仅用于迁移或清理。</summary>
    public const string LegacyAccessToken = "user:login:token:{0}";

    /// <summary>旧刷新令牌会话键模板；参数为 <c>N</c> 格式用户标识，仅用于迁移或清理。</summary>
    public const string LegacyRefreshToken = "user:login:refreshtoken:{0}";

    /// <summary>当前用户会话摘要键模板；参数为 <c>N</c> 格式用户标识。</summary>
    public const string Session = "authorization:v2:session:{0}";

    /// <summary>当前刷新令牌索引键模板；参数必须为刷新令牌摘要，不能使用明文令牌。</summary>
    public const string RefreshToken = "authorization:v2:refresh:{0}";
}
