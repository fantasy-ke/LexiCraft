using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Authentication.Permission;
using BuildingBlocks.Authentication.Shared;
using BuildingBlocks.Caching.Configuration;
using BuildingBlocks.Caching.Factories;
using BuildingBlocks.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.Authentication;

public static class AuthorizationExtensions
{
    public static IHostApplicationBuilder RegisterAuthorization(this IHostApplicationBuilder builder)
    {
        builder.AddAuthorizationRedis();
        builder.Services.AddTransient<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();
        builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizeResultHandle>();
        builder.Services.AddSingleton<IAuthorizationHandler, AuthorizeHandler>();
        builder.Services.AddSingleton<IJwtTokenProvider, JwtTokenProvider>();
        builder.Services.AddScoped<IUserContext, UserContext>();
        // 权限相关服务
        builder.Services.AddSingleton<IPermissionDefinitionManager, PermissionDefinitionManager>();

        return builder;
    }

    /// <summary>
    ///     添加权限 Redis
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IHostApplicationBuilder AddAuthorizationRedis(this IHostApplicationBuilder builder)
    {
        // 使用扩展方法绑定配置
        var oauthOptions = builder.Configuration.BindOptions<OAuthOptions>();
        // 注册配置选项
        builder.Services.AddConfigurationOptions<OAuthOptions>();
        // 如果未启用 Redis，直接返回
        if (!oauthOptions.OAuthRedis.Enable) return builder;

        // 注册默认 Redis 配置 (如果存在)
        builder.Services.Configure<RedisConnectionOptions>(builder.Configuration.GetSection("RedisCache"));

        // 配置 Redis 连接选项，添加 OAuth 实例
        builder.Services.Configure<RedisConnectionOptions>(options =>
        {
            if (!string.IsNullOrEmpty(oauthOptions.OAuthRedis.ConnectionString))
                options.Instances["OAuthRedis"] = oauthOptions.OAuthRedis.ConnectionString;
        });

        // 确保 RedisConnectionFactory 已注册
        builder.Services.TryAddSingleton<IRedisConnectionFactory, RedisConnectionFactory>();

        // 注册 Redis 权限缓存服务
        builder.Services.AddSingleton<IPermissionCache, RedisPermissionCache>();
        builder.Services.AddScoped<IPermissionCheck, PermissionCheck>();
        return builder;
    }

    /// <summary>
    ///     添加权限定义提供程序
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddPermissionDefinitionProvider<T>(this IServiceCollection services)
        where T : PermissionDefinitionProvider
    {
        services.AddSingleton<PermissionDefinitionProvider, T>();
        return services;
    }
}