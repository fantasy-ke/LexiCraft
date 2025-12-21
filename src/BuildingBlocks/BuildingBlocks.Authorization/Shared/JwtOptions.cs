namespace BuildingBlocks.Shared;

public class JwtOptions
{
    public const string Name = "Jwt";

    /// <summary>
    /// 颁发人
    /// </summary>
    public string? Issuer { get; set; }

    public string? Authority { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// 密钥
    /// </summary>
    public string? Secret { get; set; }

    /// <summary>
    /// Token过期时间
    /// </summary>
    public int ExpireMinute { get; set; }

    /// <summary>
    /// 刷新Token过期时间
    /// </summary>
    public int RefreshExpireMinute { get; set; }
    public bool ValidateIssuer { get; set; } = true;
    public bool ValidateAudience { get; set; } = true;
    public bool ValidateLifetime { get; set; } = true;
    
    public IList<string> ValidAudiences { get; set; } = new List<string>();
    public IList<string> ValidIssuers { get; set; } = new List<string>();
    
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);
    
}