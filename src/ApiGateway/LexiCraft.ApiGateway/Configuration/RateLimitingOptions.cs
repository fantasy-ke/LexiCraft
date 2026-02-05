namespace LexiCraft.ApiGateway.Configuration;

/// <summary>
/// 速率限制配置选项
/// 用于控制API请求频率限制，防止滥用和过载
/// </summary>
public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// 网关层基础限流 - 许可限制 - 在指定时间窗口内允许的最大请求数
    /// 例如：设置为100，表示在时间窗口内最多允许100个请求
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// 网关层基础限流 - 时间窗口 - 统计请求数的时间段长度（以秒为单位）
    /// 例如：设置为10，表示统计过去10秒内的请求数
    /// </summary>
    public int Window { get; set; } = 10;

    /// <summary>
    /// 网关层基础限流 - 排队限制 - 当达到许可限制时，排队等待的请求数量
    /// 例如：设置为10，表示最多允许10个额外请求排队等待处理
    /// </summary>
    public int QueueLimit { get; set; } = 10;

    /// <summary>
    /// 是否排除匿名客户端 - 如果为true，则不对未认证的客户端应用限流
    /// </summary>
    public bool ExcludeAnonymousClients { get; set; } = true;

    /// <summary>
    /// 是否启用转发限流信息到后端服务
    /// 如果启用，网关会将限流相关信息添加到请求头中传递给后端服务
    /// </summary>
    public bool ForwardLimitInfoToBackend { get; set; } = true;

    /// <summary>
    /// 限流信息请求头名称
    /// 用于向前端服务传递限流相关信息
    /// </summary>
    public string LimitInfoHeaderName { get; set; } = "X-RateLimit-Info";

    /// <summary>
    /// 针对特定路径的限流配置
    /// 允许为不同的路径或服务配置不同的限流策略
    /// </summary>
    public List<PathSpecificRateLimiting> PathSpecificConfigs { get; set; } = new List<PathSpecificRateLimiting>();
}