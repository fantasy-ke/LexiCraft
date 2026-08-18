namespace BuildingBlocks.Authentication.Options;

/// <summary>
///     配置 JWT Bearer 验证、访问令牌签发和刷新令牌有效期。
/// </summary>
/// <remarks>
///     配置节名称为 <c>OAuthOptions</c>。生产环境应保持签发方、受众和有效期验证开启，并通过环境变量、
///     用户机密或部署平台密钥管理提供 <see cref="Secret"/>；不得把对称签名密钥提交到仓库。
/// </remarks>
public class OAuthOptions
{
    /// <summary>获取或设置访问令牌的签发方。</summary>
    public string? Issuer { get; set; }

    /// <summary>
    ///     获取或设置外部 OpenID Connect/JWT Authority 地址；使用元数据发现时应为受信任的 HTTPS 地址。
    /// </summary>
    public string? Authority { get; set; }

    /// <summary>获取或设置访问令牌的目标受众。</summary>
    public string? Audience { get; set; }

    /// <summary>
    ///     获取或设置 HMAC 对称签名密钥。此值属于敏感配置，所有签发方和验证方必须安全地使用同一有效密钥。
    /// </summary>
    public string? Secret { get; set; }

    /// <summary>获取或设置访问令牌有效期（分钟）。</summary>
    public int ExpireMinute { get; set; }

    /// <summary>获取或设置刷新令牌有效期（分钟）。</summary>
    public int RefreshExpireMinute { get; set; }

    /// <summary>获取或设置是否验证 JWT 签发方；生产环境应保持为 <see langword="true"/>。</summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>获取或设置是否验证 JWT 受众；生产环境应保持为 <see langword="true"/>。</summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>获取或设置是否验证 JWT 有效期；生产环境应保持为 <see langword="true"/>。</summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    ///     获取或设置读取 Authority 元数据时是否要求 HTTPS；生产环境应设为 <see langword="true"/>。
    /// </summary>
    public bool? RequireHttpsMetadata { get; set; }

    /// <summary>获取可接受的 JWT 受众集合；用于平滑迁移或多受众验证。</summary>
    public IList<string> ValidAudiences { get; set; } = new List<string>();

    /// <summary>获取可接受的 JWT 签发方集合；用于平滑迁移或多签发方验证。</summary>
    public IList<string> ValidIssuers { get; set; } = new List<string>();

    /// <summary>
    ///     获取或设置验证令牌时间声明时允许的时钟偏差；值越大，被接受的过期令牌窗口越长。
    /// </summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);
}
