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

    public const string RedisTokenKey = "RedisToken_{0}";

    public const string RedisRefreshTokenKey = "RedisRefreshToken_{0}";

    /// <summary>
    ///     获取权限请求头
    /// </summary>
    public const string AuthorizationHeader = "Authorization";
}