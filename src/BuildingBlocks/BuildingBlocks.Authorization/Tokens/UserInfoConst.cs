namespace BuildingBlocks.Authentication.Tokens;

/// <summary>
///     JWT Claim 名称和标准授权请求头名称。
/// </summary>
public static class UserInfoConst
{
    /// <summary>JWT 中用户标识的兼容 Claim 名称。</summary>
    public const string UserId = "USER_ID";

    /// <summary>JWT 中用户名的 Claim 名称。</summary>
    public const string UserName = "USER_NAME";

    /// <summary>JWT 中登录账号的 Claim 名称。</summary>
    public const string UserAccount = "USER_ACCOUNT";

    /// <summary>标准 HTTP Authorization 请求头名称。</summary>
    public const string AuthorizationHeader = "Authorization";
}
