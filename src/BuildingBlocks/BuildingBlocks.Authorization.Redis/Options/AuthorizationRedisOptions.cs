namespace BuildingBlocks.Authentication.Redis.Options;

/// <summary>
///     仅供 Identity 授权 Redis 适配层使用的连接配置。
/// </summary>
public sealed class AuthorizationRedisOptions
{
    public const string SectionName = "OAuthOptions:OAuthRedis";

    public bool Enable { get; set; }
    public string? ConnectionString { get; set; }
    public int DefaultDatabase { get; set; }
    public int ConnectTimeout { get; set; } = 5000;
    public int SyncTimeout { get; set; } = 5000;
}
