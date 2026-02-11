using LexiCraft.ApiGateway.Configuration;
using LexiCraft.ApiGateway.Utilities;

namespace LexiCraft.ApiGateway.Middlewares;

/// <summary>
///     安全头中间件
///     为HTTP响应添加各种安全头，增强API安全性
///     支持基于路径的个性化安全头配置
/// </summary>
public class SecurityHeadersMiddleware(RequestDelegate next, SecurityHeadersPolicy policy)
{
    /// <summary>
    ///     处理HTTP请求，添加安全头
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var requestPath = context.Request.Path.Value ?? string.Empty;

        // 查找匹配的路径特定配置
        var pathSpecificConfig = policy.PathSpecificConfigs
            .FirstOrDefault(config => PathMatcher.Matches(requestPath, config.PathPattern));

        // 应用安全头 - 优先使用路径特定配置，如果没有则使用默认配置
        if (pathSpecificConfig?.EnableHsts ?? policy.EnableHsts)
        {
            // HSTS (HTTP Strict Transport Security)
            // 强制浏览器仅使用HTTPS连接到服务器，防止协议降级攻击和cookie劫持
            // 配置: max-age=63072000; includeSubDomains; preload
            // - max-age: 告诉浏览器在指定时间内（这里是2年）始终使用HTTPS访问该网站
            // - includeSubDomains: 对所有子域名也强制使用HTTPS
            // - preload: 允许网站被加入浏览器的HSTS预加载列表
            var hstsValue = "max-age=63072000; includeSubDomains; preload";
            context.Response.Headers.Append("Strict-Transport-Security", hstsValue);
        }

        if (pathSpecificConfig?.EnableXContentTypeOptions ?? policy.EnableXContentTypeOptions)
            // X-Content-Type-Options
            // 防止浏览器MIME类型嗅探攻击，阻止浏览器尝试猜测响应内容的MIME类型
            // 配置: nosniff - 强制浏览器严格按照响应头中的Content-Type来解析内容，而不是尝试猜测
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        if (pathSpecificConfig?.EnableXFrameOptions ?? policy.EnableXFrameOptions)
            // X-Frame-Options
            // 防止点击劫持攻击，控制页面是否可以在iframe中显示
            // 配置: DENY - 完全禁止页面在任何frame中显示，防止恶意网站将你的页面嵌入到iframe中
            context.Response.Headers.Append("X-Frame-Options", "DENY");

        if (pathSpecificConfig?.EnableXssProtection ?? policy.EnableXssProtection)
            // X-XSS-Protection
            // 启用浏览器内置的跨站脚本(XSS)过滤器
            // 配置: 1; mode=block - 启用XSS过滤器，并在检测到XSS攻击时阻止页面渲染
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

        // 使用路径特定的CSP配置，如果没有则使用默认配置
        var cspValue = pathSpecificConfig?.ContentSecurityPolicy ?? policy.ContentSecurityPolicy;
        if (!string.IsNullOrEmpty(cspValue))
            // Content Security Policy (CSP)
            // 控制哪些资源可以被加载和执行，防止XSS、数据注入等攻击
            // 配置: default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline';
            // - default-src 'self': 默认只允许从同源加载资源
            // - script-src 'self' 'unsafe-inline' 'unsafe-eval': 脚本可以从同源和内联加载，允许eval函数
            // - style-src 'self' 'unsafe-inline': 样式可以从同源和内联加载
            context.Response.Headers.Append("Content-Security-Policy", cspValue);

        // 继续处理请求
        await next(context);
    }
}