using BuildingBlocks.Caching.Redis.Connections;
using BuildingBlocks.Idempotency.Abstractions;
using BuildingBlocks.Idempotency.Internal;
using BuildingBlocks.Idempotency.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.Idempotency.Extensions;

/// <summary>
///     幂等处理组件注册入口。
/// </summary>
public static class IdempotencyExtensions
{
    /// <summary>
    ///     注册幂等配置与 Redis 存储。
    /// </summary>
    /// <remarks>
    ///     默认 Redis 实现依赖 <c>AddCaching</c>，因此必须先注册缓存组件。
    ///     如需自定义存储，请在调用本方法前注册 <see cref="IIdempotencyStore"/>。
    /// </remarks>
    public static IHostApplicationBuilder AddIdempotency(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var hasCustomStore = builder.Services.Any(descriptor => descriptor.ServiceType == typeof(IIdempotencyStore));
        var hasRedisConnection = builder.Services.Any(
            descriptor => descriptor.ServiceType == typeof(IRedisConnectionFactory));

        if (!hasCustomStore && !hasRedisConnection)
            throw new InvalidOperationException("AddCaching must be registered before AddIdempotency");

        builder.Services
            .AddOptions<IdempotencyOptions>()
            .Bind(builder.Configuration.GetSection(IdempotencyOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.HeaderName),
                "Idempotency:HeaderName 不能为空")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Prefix),
                "Idempotency:Prefix 不能为空")
            .Validate(options => options.Retention >= TimeSpan.FromMilliseconds(1),
                "Idempotency:Retention 必须至少为 1 毫秒")
            .Validate(options => options.ProcessingTimeout >= TimeSpan.FromMilliseconds(1),
                "Idempotency:ProcessingTimeout 必须至少为 1 毫秒")
            .Validate(options => options.ReplayWaitTimeout >= TimeSpan.Zero,
                "Idempotency:ReplayWaitTimeout 不能为负数")
            .Validate(options => options.ReplayPollInterval >= TimeSpan.FromMilliseconds(1),
                "Idempotency:ReplayPollInterval 必须至少为 1 毫秒")
            .Validate(options => options.MaxRequestBodyBytes > 0,
                "Idempotency:MaxRequestBodyBytes 必须大于 0")
            .Validate(options => options.MaxResponseBodyBytes is > 0 and <= int.MaxValue,
                $"Idempotency:MaxResponseBodyBytes 必须介于 1 和 {int.MaxValue} 之间")
            .Validate(options => options.MaxKeyLength > 0,
                "Idempotency:MaxKeyLength 必须大于 0")
            .ValidateOnStart();

        builder.Services.TryAddSingleton<IIdempotencyStore, RedisIdempotencyStore>();
        return builder;
    }

    /// <summary>
    ///     启用幂等处理中间件。必须在路由匹配之后、端点执行之前调用。
    /// </summary>
    public static IApplicationBuilder UseIdempotency(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<IdempotencyMiddleware>();
    }
}