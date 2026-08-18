using BuildingBlocks.Authentication.Abstractions;
using BuildingBlocks.Authentication.Redis.Caching;
using BuildingBlocks.Authentication.Redis.Options;
using BuildingBlocks.Authentication.Redis.Sessions;
using BuildingBlocks.Authentication.Redis.Synchronization;
using BuildingBlocks.Caching.Abstractions;
using BuildingBlocks.Caching.Locking;
using BuildingBlocks.Caching.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace BuildingBlocks.Authentication;

/// <summary>
///     提供 Identity 授权状态所需的 Redis 适配层注册入口。
/// </summary>
public static class AuthorizationRedisExtensions
{
    /// <summary>
    ///     授权缓存与分布式锁使用的命名 Redis 实例。
    /// </summary>
    public const string AuthorizationRedisInstanceName = "OAuthRedis";

    /// <summary>
    ///     为 Identity 注册授权缓存、会话校验与同步实现。
    /// </summary>
    /// <param name="builder">应用宿主构建器。</param>
    /// <returns>当前应用宿主构建器。</returns>
    /// <remarks>
    ///     必须按 <c>AddCaching</c>、<c>RegisterAuthorization</c>、<c>AddAuthorizationRedis</c>、
    ///     <c>AddLocalPermissionValidation</c> 的顺序注册。业务服务不应引用此 Identity 专用适配层。
    ///     Redis 会话或权限依赖不可用时，授权链路按关闭式失败返回 503，不会放行请求。
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///     配置未启用、连接字符串缺失，或尚未注册 <c>ICacheService</c>/<c>IDistributedLockProvider</c> 时抛出。
    /// </exception>
    public static IHostApplicationBuilder AddAuthorizationRedis(this IHostApplicationBuilder builder)
    {
        var redisSection = builder.Configuration.GetSection(AuthorizationRedisOptions.SectionName);
        var redisOptions = redisSection.Get<AuthorizationRedisOptions>() ?? new AuthorizationRedisOptions();
        builder.Services.Configure<AuthorizationRedisOptions>(redisSection);

        if (!redisOptions.Enable)
            throw new InvalidOperationException("OAuthOptions:OAuthRedis must be enabled for Identity authorization");

        if (string.IsNullOrWhiteSpace(redisOptions.ConnectionString))
            throw new InvalidOperationException("OAuthOptions:OAuthRedis:ConnectionString is required");

        if (builder.Services.All(descriptor => descriptor.ServiceType != typeof(ICacheService)) 
            || builder.Services.All(descriptor => descriptor.ServiceType != typeof(IDistributedLockProvider)))
        {
            throw new InvalidOperationException("AddCaching must be registered before AddAuthorizationRedis");
        }

        var redisConfiguration = ConfigurationOptions.Parse(redisOptions.ConnectionString);
        redisConfiguration.DefaultDatabase = redisOptions.DefaultDatabase;
        redisConfiguration.ConnectTimeout = redisOptions.ConnectTimeout;
        redisConfiguration.SyncTimeout = redisOptions.SyncTimeout;
        redisConfiguration.AsyncTimeout = redisOptions.SyncTimeout;

        builder.Services.Configure<RedisConnectionOptions>(options =>
        {
            options.Instances[AuthorizationRedisInstanceName] = redisConfiguration.ToString(true);
        });

        builder.Services.AddSingleton<IAuthorizationCache, AuthorizationCache>();
        builder.Services.AddSingleton<IPermissionCache, RedisPermissionCache>();
        builder.Services.AddSingleton<IAuthorizationSynchronization, RedisAuthorizationSynchronization>();
        builder.Services.AddScoped<IAccessTokenValidator, RedisAccessTokenValidator>();
        return builder;
    }
}
