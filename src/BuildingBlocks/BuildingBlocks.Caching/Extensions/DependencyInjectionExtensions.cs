using BuildingBlocks.Caching.Abstractions;
using BuildingBlocks.Caching.Configuration;
using BuildingBlocks.Caching.DistributedLock;
using BuildingBlocks.Caching.Factories;
using BuildingBlocks.Caching.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Caching.Extensions;

/// <summary>
///     依赖注入扩展方法
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    ///     添加缓存服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 注册 Redis 连接选项
        services.Configure<RedisConnectionOptions>(configuration.GetSection("RedisCache"));

        // 注册核心服务
        RegisterCoreServices(services);

        return services;
    }

    /// <summary>
    ///     添加缓存服务（使用连接字符串）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">Redis 连接字符串</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCaching(
        this IServiceCollection services,
        string connectionString)
    {
        // 配置 Redis 连接选项
        services.Configure<RedisConnectionOptions>(options => { options.DefaultConnectionString = connectionString; });

        // 注册核心服务
        RegisterCoreServices(services);

        return services;
    }

    /// <summary>
    ///     添加缓存服务（使用多个 Redis 实例）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="redisInstances">Redis 实例配置字典</param>
    /// <param name="defaultConnectionString">默认连接字符串</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCaching(
        this IServiceCollection services,
        Dictionary<string, string> redisInstances,
        string? defaultConnectionString = null)
    {
        // 配置 Redis 连接选项
        services.Configure<RedisConnectionOptions>(options =>
        {
            options.DefaultConnectionString = defaultConnectionString;
            foreach (var instance in redisInstances) options.Instances[instance.Key] = instance.Value;
        });

        // 注册核心服务
        RegisterCoreServices(services);

        return services;
    }

    /// <summary>
    ///     注册核心服务
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void RegisterCoreServices(IServiceCollection services)
    {
        // 注册 Redis 连接工厂
        services.AddSingleton<IRedisConnectionFactory, RedisConnectionFactory>();

        // 注册缓存服务
        services.AddSingleton<IDistributedCacheService, DistributedCacheService>();
        services.AddSingleton<IDistributedLockProvider, RedisDistributedLockProvider>();
        services.AddSingleton<ICacheService, CacheService>();
        // 添加内存缓存支持
        services.AddMemoryCache();
    }
}