namespace BuildingBlocks.Authentication;

public class UserInfoConst
{
    /// <summary>
    ///     claim type for user id
    /// </summary>
    public const string UserId = "USER_ID";

    /// <summary>
    ///     claim type for username
    /// </summary>
    public const string UserName = "USER_NAME";

    /// <summary>
    ///     UserAllPermissions
    /// </summary>
    public const string UserAllPermissions = "User_All_Permissions";

    /// <summary>
    ///     claim type for user account
    /// </summary>
    public const string UserAccount = "USER_ACCOUNT";

    public const string RedisTokenKey = "user:login:token:{0}";

    public const string RedisRefreshTokenKey = "user:login:refreshtoken:{0}";

    public const string RedisAuthorizationSessionKey = "authorization:v2:session:{0}";

    public const string RedisAuthorizationRefreshTokenKey = "authorization:v2:refresh:{0}";

    /// <summary>
    ///     获取权限请求头
    /// </summary>
    public const string AuthorizationHeader = "Authorization";
}