using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Authentication.Permission;
using BuildingBlocks.Authentication.Shared;
using BuildingBlocks.Caching.Abstractions;
using BuildingBlocks.Caching.Configuration;
using BuildingBlocks.Caching.DistributedLock;
using BuildingBlocks.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace BuildingBlocks.Authentication;

/// <summary>
///     提供权限定义、策略解析、鉴权处理以及 Identity/业务服务验证模式的注册入口。
/// </summary>
public static class AuthorizationExtensions
{
    /// <summary>
    ///     授权会话、权限快照和分布式锁共用的 Redis 实例名称。
    /// </summary>
    public const string AuthorizationRedisInstanceName = "OAuthRedis";

    /// <summary>
    ///     注册所有服务共用的授权基础设施。调用方还必须选择本地或 Identity API 权限验证模式。
    /// </summary>
    /// <param name="builder">应用宿主构建器。</param>
    /// <returns>当前应用宿主构建器。</returns>
    public static IHostApplicationBuilder RegisterAuthorization(this IHostApplicationBuilder builder)
    {
        builder.Services.AddAuthorization();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddConfigurationOptions<OAuthOptions>();
        builder.Services.AddOptions<PermissionAuthorizationOptions>()
            .BindConfiguration(nameof(PermissionAuthorizationOptions))
            .Validate(options => Uri.TryCreate(options.IdentityApiBaseAddress, UriKind.Absolute, out _),
                "IdentityApiBaseAddress must be an absolute URI")
            .Validate(options => options.IdentityApiValidationPath.StartsWith("/", StringComparison.Ordinal),
                "IdentityApiValidationPath must start with '/'")
            .ValidateOnStart();
        builder.Services.AddSingleton<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();
        builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizeResultHandle>();
        builder.Services.AddScoped<IAuthorizationHandler, AuthorizeHandler>();
        builder.Services.AddSingleton<IJwtTokenProvider, JwtTokenProvider>();
        builder.Services.AddScoped<IUserContext, UserContext>();
        builder.Services.AddSingleton<IPermissionDefinitionManager, PermissionDefinitionManager>();

        return builder;
    }

    /// <summary>
    ///     为 Identity 服务注册 Redis 会话校验、权限快照缓存和分布式同步能力。
    /// </summary>
    /// <remarks>
    ///     必须先注册 <c>BuildingBlocks.Caching</c>；业务服务不应调用此方法，而应通过 Identity API 验证权限。
    /// </remarks>
    /// <param name="builder">应用宿主构建器。</param>
    /// <returns>当前应用宿主构建器。</returns>
    /// <exception cref="InvalidOperationException">Redis 未启用、连接字符串为空或缓存基础设施未注册。</exception>
    public static IHostApplicationBuilder AddAuthorizationRedis(this IHostApplicationBuilder builder)
    {
        var oauthOptions = builder.Configuration.BindOptions<OAuthOptions>();
        builder.Services.AddConfigurationOptions<OAuthOptions>();

        if (!oauthOptions.OAuthRedis.Enable)
            throw new InvalidOperationException("OAuthOptions:OAuthRedis must be enabled for Identity authorization");

        if (string.IsNullOrWhiteSpace(oauthOptions.OAuthRedis.ConnectionString))
            throw new InvalidOperationException("OAuthOptions:OAuthRedis:ConnectionString is required");

        if (!builder.Services.Any(descriptor => descriptor.ServiceType == typeof(ICacheService)) ||
            !builder.Services.Any(descriptor => descriptor.ServiceType == typeof(IDistributedLockProvider)))
        {
            throw new InvalidOperationException(
                "AddCaching must be registered before AddAuthorizationRedis");
        }

        var redisConfiguration = ConfigurationOptions.Parse(oauthOptions.OAuthRedis.ConnectionString);
        redisConfiguration.DefaultDatabase = oauthOptions.OAuthRedis.DefaultDatabase;
        redisConfiguration.ConnectTimeout = oauthOptions.OAuthRedis.ConnectTimeout;
        redisConfiguration.SyncTimeout = oauthOptions.OAuthRedis.SyncTimeout;
        redisConfiguration.AsyncTimeout = oauthOptions.OAuthRedis.SyncTimeout;

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

    /// <summary>
    ///     注册 Identity 服务使用的本地权限验证器，并从权威权限存储读取当前用户权限。
    /// </summary>
    /// <typeparam name="TPermissionStore">实现用户权限查询的 Identity 权限存储。</typeparam>
    /// <param name="services">服务集合。</param>
    /// <returns>当前服务集合。</returns>
    public static IServiceCollection AddLocalPermissionValidation<TPermissionStore>(
        this IServiceCollection services)
        where TPermissionStore : class, IUserPermissionStore
    {
        services.RemoveAll<IPermissionCheck>();
        services.AddScoped<IUserPermissionStore, TPermissionStore>();
        services.AddScoped<IPermissionCheck, PermissionCheck>();
        return services;
    }

    /// <summary>
    ///     注册业务服务使用的远程权限验证器，将当前 Bearer Token 转发给 Identity API 验证会话和权限。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <returns>当前服务集合。</returns>
    public static IServiceCollection AddIdentityApiPermissionValidation(this IServiceCollection services)
    {
        services.RemoveAll<IPermissionCheck>();
        services.RemoveAll<IAccessTokenValidator>();
        services.AddScoped<IAccessTokenValidator, AuthenticatedAccessTokenValidator>();
        services.AddHttpClient<IdentityApiPermissionCheck>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<PermissionAuthorizationOptions>>()
                .CurrentValue;
            httpClient.BaseAddress = new Uri(options.IdentityApiBaseAddress, UriKind.Absolute);
            httpClient.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddScoped<IPermissionCheck>(serviceProvider =>
            serviceProvider.GetRequiredService<IdentityApiPermissionCheck>());
        return services;
    }

    /// <summary>
    ///     注册权限定义提供程序；每个使用动态权限策略的服务都必须注册其可识别的权限定义。
    /// </summary>
    /// <typeparam name="T">权限定义提供程序类型。</typeparam>
    /// <param name="services">服务集合。</param>
    /// <returns>当前服务集合。</returns>
    public static IServiceCollection AddPermissionDefinitionProvider<T>(this IServiceCollection services)
        where T : PermissionDefinitionProvider
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<PermissionDefinitionProvider, T>());
        return services;
    }
}
