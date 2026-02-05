using System.Text.Json;

namespace LexiCraft.ApiGateway.Configuration;

/// <summary>
/// 安全头策略配置
/// 用于配置各种HTTP安全头，增强API安全性
/// </summary>
public class SecurityHeadersPolicy
{
    public const string SectionName = "SecurityHeaders";

    /// <summary>
    /// 启用HSTS (HTTP Strict Transport Security)
    /// 强制浏览器仅使用HTTPS连接到服务器，防止协议降级攻击和cookie劫持
    /// 配置: max-age=63072000; includeSubDomains; preload
    /// - max-age: 告诉浏览器在指定时间内（这里是2年）始终使用HTTPS访问该网站
    /// - includeSubDomains: 对所有子域名也强制使用HTTPS
    /// - preload: 允许网站被加入浏览器的HSTS预加载列表
    /// </summary>
    public bool EnableHsts { get; set; } = true;

    /// <summary>
    /// 启用X-Content-Type-Options
    /// 防止浏览器MIME类型嗅探攻击，阻止浏览器尝试猜测响应内容的MIME类型
    /// 配置: nosniff - 强制浏览器严格按照响应头中的Content-Type来解析内容，而不是尝试猜测
    /// </summary>
    public bool EnableXContentTypeOptions { get; set; } = true;

    /// <summary>
    /// 启用X-Frame-Options
    /// 防止点击劫持攻击，控制页面是否可以在iframe中显示
    /// 配置: DENY - 完全禁止页面在任何frame中显示，防止恶意网站将你的页面嵌入到iframe中
    /// </summary>
    public bool EnableXFrameOptions { get; set; } = true;

    /// <summary>
    /// 启用X-XSS-Protection
    /// 启用浏览器内置的跨站脚本(XSS)过滤器
    /// 配置: 1; mode=block - 启用XSS过滤器，并在检测到XSS攻击时阻止页面渲染
    /// </summary>
    public bool EnableXssProtection { get; set; } = true;

    /// <summary>
    /// 内容安全策略 (Content Security Policy)
    /// 控制哪些资源可以被加载和执行，防止XSS、数据注入等攻击
    /// 配置: default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline';
    /// - default-src 'self': 默认只允许从同源加载资源
    /// - script-src 'self' 'unsafe-inline' 'unsafe-eval': 脚本可以从同源和内联加载，允许eval函数
    /// - style-src 'self' 'unsafe-inline': 样式可以从同源和内联加载
    /// </summary>
    public string? ContentSecurityPolicy { get; set; } = "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline';";

    /// <summary>
    /// 针对特定路径的安全头配置
    /// 允许为不同的路径或服务配置不同的安全头策略
    /// </summary>
    public List<PathSpecificSecurityHeaders> PathSpecificConfigs { get; set; } = [];
}