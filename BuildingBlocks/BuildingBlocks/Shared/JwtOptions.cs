namespace BuildingBlocks.Shared;

public class JwtOptions
{
    public const string Name = "Jwt";

    /// <summary>
    /// 颁发人
    /// </summary>
    public string Issuer { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string Audience { get; set; }

    /// <summary>
    /// 密钥
    /// </summary>
    public string Secret { get; set; }

    /// <summary>
    /// Token过期时间
    /// </summary>
    public int ExpireMinute { get; set; }

    /// <summary>
    /// 刷新Token过期时间
    /// </summary>
    public int RefreshExpireMinute { get; set; }
}