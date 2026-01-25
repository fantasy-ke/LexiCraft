namespace BuildingBlocks.Authentication.Shared;

public class OAuthOptions
{
    /// <summary>
    ///     颁发人
    /// </summary>
    public string? Issuer { get; set; }

    public string? Authority { get; set; }

    /// <summary>
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    ///     密钥
    /// </summary>
    public string? Secret { get; set; }

    /// <summary>
    ///     Token过期时间
    /// </summary>
    public int ExpireMinute { get; set; }

    /// <summary>
    ///     刷新Token过期时间
    /// </summary>
    public int RefreshExpireMinute { get; set; }

    public bool ValidateIssuer { get; set; } = true;
    public bool ValidateAudience { get; set; } = true;
    public bool ValidateLifetime { get; set; } = true;

    public bool? RequireHttpsMetadata { get; set; }

    public IList<string> ValidAudiences { get; set; } = new List<string>();
    public IList<string> ValidIssuers { get; set; } = new List<string>();

    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);

    public OAuthRedisOptions OAuthRedis { get; set; } = new();
}

/// <summary>
///     Redis 配置选项
/// </summary>
public class OAuthRedisOptions
{
    /// <summary>
    ///     是否启用 Redis
    /// </summary>
    public bool Enable { get; set; } = false;

    /// <summary>
    ///     Redis 连接字符串
    ///     示例：localhost:6379,password=xxx
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    ///     默认数据库索引
    /// </summary>
    public int DefaultDatabase { get; set; } = 0;

    /// <summary>
    ///     连接超时时间（毫秒）
    /// </summary>
    public int ConnectTimeout { get; set; } = 5000;

    /// <summary>
    ///     同步超时时间（毫秒）
    /// </summary>
    public int SyncTimeout { get; set; } = 5000;
}