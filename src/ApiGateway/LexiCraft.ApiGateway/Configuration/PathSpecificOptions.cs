using System.Text.Json;

namespace LexiCraft.ApiGateway.Configuration;

/// <summary>
/// 针对特定路径或服务的个性化安全头配置
/// </summary>
public class PathSpecificSecurityHeaders
{
    /// <summary>
    /// 路径模式，例如 "/identity/*", "/vocabulary/api/*"
    /// </summary>
    public string PathPattern { get; set; } = string.Empty;

    /// <summary>
    /// 启用HSTS (HTTP Strict Transport Security)
    /// </summary>
    public bool? EnableHsts { get; set; }

    /// <summary>
    /// 启用X-Content-Type-Options
    /// </summary>
    public bool? EnableXContentTypeOptions { get; set; }

    /// <summary>
    /// 启用X-Frame-Options
    /// </summary>
    public bool? EnableXFrameOptions { get; set; }

    /// <summary>
    /// 启用X-XSS-Protection
    /// </summary>
    public bool? EnableXssProtection { get; set; }

    /// <summary>
    /// 内容安全策略 (Content Security Policy)
    /// </summary>
    public string? ContentSecurityPolicy { get; set; }
}

/// <summary>
/// 针对特定路径或服务的个性化限流配置
/// </summary>
public class PathSpecificRateLimiting
{
    /// <summary>
    /// 路径模式，例如 "/identity/*", "/vocabulary/api/*"
    /// </summary>
    public string PathPattern { get; set; } = string.Empty;

    /// <summary>
    /// 许可限制 - 在指定时间窗口内允许的最大请求数
    /// </summary>
    public int? PermitLimit { get; set; }

    /// <summary>
    /// 时间窗口 - 统计请求数的时间段长度（以秒为单位）
    /// </summary>
    public int? Window { get; set; }

    /// <summary>
    /// 排队限制 - 当达到许可限制时，排队等待的请求数量
    /// </summary>
    public int? QueueLimit { get; set; }
}