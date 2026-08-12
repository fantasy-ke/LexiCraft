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

public static class AuthorizationExtensions
{
    public const string AuthorizationRedisInstanceName = "OAuthRedis";

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

    public static IServiceCollection AddLocalPermissionValidation<TPermissionStore>(
        this IServiceCollection services)
        where TPermissionStore : class, IUserPermissionStore
    {
        services.RemoveAll<IPermissionCheck>();
        services.AddScoped<IUserPermissionStore, TPermissionStore>();
        services.AddScoped<IPermissionCheck, PermissionCheck>();
        return services;
    }

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

    public static IServiceCollection AddPermissionDefinitionProvider<T>(this IServiceCollection services)
        where T : PermissionDefinitionProvider
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<PermissionDefinitionProvider, T>());
        return services;
    }
}
