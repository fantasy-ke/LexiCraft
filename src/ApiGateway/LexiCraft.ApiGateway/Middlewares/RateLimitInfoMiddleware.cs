using BuildingBlocks.Extensions.System;
using LexiCraft.ApiGateway.Configuration;
using Microsoft.Extensions.Options;

namespace LexiCraft.ApiGateway.Middlewares;

/// <summary>
///     限流信息传递中间件
///     在请求转发到后端服务之前，将限流相关信息添加到请求头中
///     以便后端服务可以基于这些信息实现更精细的业务级限流
/// </summary>
public class RateLimitInfoMiddleware(
    RequestDelegate next,
    IOptions<RateLimitingOptions> rateLimitingOptions)
{
    private readonly RateLimitingOptions _rateLimitingOptions = rateLimitingOptions.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        // 如果启用了转发限流信息到后端服务
        if (_rateLimitingOptions.ForwardLimitInfoToBackend)
        {
            // 获取客户端IP
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // 构建限流信息
            var limitInfo = new Dictionary<string, string?>
            {
                ["client-ip"] = clientIp,
                ["gateway-permit-limit"] = _rateLimitingOptions.PermitLimit.ToString(),
                ["gateway-window"] = _rateLimitingOptions.Window.ToString(),
                ["gateway-queue-limit"] = _rateLimitingOptions.QueueLimit.ToString()
            };

            // 将限流信息序列化为JSON并添加到请求头
            var limitInfoJson = limitInfo.ToJson();
            context.Request.Headers.Append(_rateLimitingOptions.LimitInfoHeaderName, limitInfoJson);
        }

        // 继续处理请求
        await next(context);
    }
}