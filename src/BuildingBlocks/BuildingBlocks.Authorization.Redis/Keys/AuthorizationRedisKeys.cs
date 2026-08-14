namespace BuildingBlocks.Authentication.Redis.Keys;

/// <summary>
///     Identity 会话写入方与验证方共享的授权 Redis 键模板。
/// </summary>
public static class AuthorizationRedisKeys
{
    public const string UserAllPermissions = "User_All_Permissions";
    public const string LegacyAccessToken = "user:login:token:{0}";
    public const string LegacyRefreshToken = "user:login:refreshtoken:{0}";
    public const string Session = "authorization:v2:session:{0}";
    public const string RefreshToken = "authorization:v2:refresh:{0}";
}
