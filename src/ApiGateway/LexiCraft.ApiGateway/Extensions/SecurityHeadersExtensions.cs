using LexiCraft.ApiGateway.Configuration;
using LexiCraft.ApiGateway.Middlewares;

namespace LexiCraft.ApiGateway.Extensions;

/// <summary>
///     安全头扩展方法
///     提供安全头中间件的注册和使用功能
/// </summary>
public static class SecurityHeadersExtensions
{
    /// <summary>
    ///     添加安全头策略服务
    ///     注册SecurityHeadersPolicy单例服务，用于配置各种安全头
    /// </summary>
    /// <param name="services">IServiceCollection实例</param>
    /// <param name="configuration">IConfiguration实例</param>
    /// <returns>更新后的IServiceCollection</returns>
    public static IServiceCollection AddSecurityHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        var securityHeadersPolicy =
            configuration.GetSection(SecurityHeadersPolicy.SectionName).Get<SecurityHeadersPolicy>()
            ?? new SecurityHeadersPolicy();

        // 注册安全头策略为单例服务，使整个应用程序共享同一份配置
        services.AddSingleton(securityHeadersPolicy);
        return services;
    }

    /// <summary>
    ///     使用安全头中间件
    ///     在HTTP请求管道中添加安全头中间件，为所有响应添加安全头
    /// </summary>
    /// <param name="builder">IApplicationBuilder实例</param>
    /// <returns>更新后的IApplicationBuilder</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        // 使用中间件模式添加安全头功能
        // SecurityHeadersMiddleware会在每个HTTP响应中添加各种安全头
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}