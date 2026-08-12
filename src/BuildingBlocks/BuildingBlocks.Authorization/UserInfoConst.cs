namespace BuildingBlocks.Authentication;

/// <summary>
///     定义 JWT Claim、授权请求头以及授权 Redis 键模板。
/// </summary>
public class UserInfoConst
{
    /// <summary>用户标识 Claim 类型。</summary>
    public const string UserId = "USER_ID";

    /// <summary>用户名 Claim 类型。</summary>
    public const string UserName = "USER_NAME";

    /// <summary>旧版用户全部权限缓存键名，仅用于兼容历史契约。</summary>
    public const string UserAllPermissions = "User_All_Permissions";

    /// <summary>用户账号 Claim 类型。</summary>
    public const string UserAccount = "USER_ACCOUNT";

    /// <summary>旧版访问令牌 Redis 键模板；v2 会话校验不读取此键。</summary>
    public const string RedisTokenKey = "user:login:token:{0}";

    /// <summary>旧版刷新令牌 Redis 键模板；v2 刷新流程不读取此键。</summary>
    public const string RedisRefreshTokenKey = "user:login:refreshtoken:{0}";

    /// <summary>v2 当前会话键模板，参数为无连字符的用户标识。</summary>
    public const string RedisAuthorizationSessionKey = "authorization:v2:session:{0}";

    /// <summary>v2 刷新令牌索引键模板，参数为刷新令牌 SHA-256 摘要。</summary>
    public const string RedisAuthorizationRefreshTokenKey = "authorization:v2:refresh:{0}";

    /// <summary>Bearer Token 使用的标准授权请求头名称。</summary>
    public const string AuthorizationHeader = "Authorization";
}