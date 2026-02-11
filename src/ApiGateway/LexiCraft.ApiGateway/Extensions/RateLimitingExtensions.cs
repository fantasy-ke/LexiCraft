using System.Threading.RateLimiting;
using LexiCraft.ApiGateway.Configuration;
using LexiCraft.ApiGateway.Utilities;

namespace LexiCraft.ApiGateway.Extensions;

/// <summary>
///     速率限制扩展方法
///     提供API请求频率限制功能，防止滥用和过载
///     支持网关层基础限流和向后端服务传递限流信息
///     同时支持基于路径的个性化限流配置
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    ///     添加自定义速率限制服务
    ///     实现基于客户端IP的滑动窗口限流机制
    ///     同时支持向前端服务传递限流相关信息，以便后端服务实现更精细的业务级限流
    /// </summary>
    /// <param name="services">IServiceCollection实例</param>
    /// <param name="configuration">IConfiguration实例</param>
    /// <returns>更新后的IServiceCollection</returns>
    public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services,
        IConfiguration configuration)
    {
        var rateLimitingOptions = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
                                  ?? new RateLimitingOptions();

        // 注册配置选项，使其他组件可以访问
        services.Configure<RateLimitingOptions>(configuration.GetSection(RateLimitingOptions.SectionName));

        // 添加ASP.NET Core内置的限流服务
        services.AddRateLimiter(limiterOptions =>
        {
            limiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                // 获取客户端IP地址作为分区键
                var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                // 获取请求路径
                var requestPath = context.Request.Path.Value ?? string.Empty;

                // 查找匹配的路径特定配置
                var pathSpecificConfig = rateLimitingOptions.PathSpecificConfigs
                    .FirstOrDefault(config => PathMatcher.Matches(requestPath, config.PathPattern));

                // 使用路径特定配置，如果没有则使用默认配置
                var permitLimit = pathSpecificConfig?.PermitLimit ?? rateLimitingOptions.PermitLimit;
                var window = pathSpecificConfig?.Window ?? rateLimitingOptions.Window;

                // 使用System.Threading.RateLimiting中的滑动窗口限流器
                return RateLimitPartition.Get(clientIp, _ => new SlidingWindowRateLimiter(
                    new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit, // 许可限制：时间窗口内允许的最大请求数
                        Window = TimeSpan.FromSeconds(window), // 时间窗口：统计请求数的时间段长度
                        SegmentsPerWindow = 4, // 将时间窗口分割成的段数，用于提高精度
                        AutoReplenishment = true // 自动补充许可
                    }));
            });
        });

        return services;
    }
}