namespace BuildingBlocks.Authentication.Redis.Options;

/// <summary>
///     配置 Identity 专用授权 Redis 连接。
/// </summary>
/// <remarks>
///     授权 Redis 保存当前会话摘要和权限快照，是 Identity 授权链路的硬依赖。启用适配层时必须提供连接字符串；
///     依赖不可用时授权应关闭式失败，不得降级为允许。连接凭据必须来自安全配置源，不得提交到仓库。
/// </remarks>
public sealed class AuthorizationRedisOptions
{
    /// <summary>授权 Redis 配置节路径。</summary>
    public const string SectionName = "OAuthOptions:OAuthRedis";

    /// <summary>
    ///     获取或设置是否启用授权 Redis；Identity 注册适配层时必须为 <see langword="true"/>。
    /// </summary>
    public bool Enable { get; set; }

    /// <summary>
    ///     获取或设置 Redis 连接字符串；可能包含凭据，应通过环境变量、用户机密或部署平台密钥管理注入。
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>获取或设置授权数据使用的 Redis 逻辑数据库编号。</summary>
    public int DefaultDatabase { get; set; }

    /// <summary>获取或设置建立 Redis 连接的超时时间（毫秒）。</summary>
    public int ConnectTimeout { get; set; } = 5000;

    /// <summary>
    ///     获取或设置同步命令超时时间（毫秒）；适配层也将此值用于异步命令超时。
    /// </summary>
    public int SyncTimeout { get; set; } = 5000;
}
