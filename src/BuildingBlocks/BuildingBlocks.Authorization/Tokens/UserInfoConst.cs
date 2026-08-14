namespace BuildingBlocks.Authentication.Tokens;

/// <summary>
///     JWT Claim 名称和标准授权请求头名称。
/// </summary>
public static class UserInfoConst
{
    public const string UserId = "USER_ID";
    public const string UserName = "USER_NAME";
    public const string UserAccount = "USER_ACCOUNT";
    public const string AuthorizationHeader = "Authorization";
}
