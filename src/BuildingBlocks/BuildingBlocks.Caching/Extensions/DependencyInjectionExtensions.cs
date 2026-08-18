using BuildingBlocks.Caching.Abstractions;
using BuildingBlocks.Caching.Options;
using BuildingBlocks.Caching.Locking;
using BuildingBlocks.Caching.Redis.Connections;
using BuildingBlocks.Caching.Redis;
using BuildingBlocks.Caching.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Caching.Extensions;

/// <summary>
///     提供缓存、命名 Redis 连接和分布式锁的依赖注入注册入口。
/// </summary>
/// <remarks>
///     三个重载均注册单例缓存门面、按名称共享的 Redis 连接、锁提供者及进程内内存缓存。
///     宿主必须配置默认 Redis 连接；即使业务调用只选择本地缓存，服务解析时仍会验证默认连接配置。
/// </remarks>
public static class DependencyInjectionExtensions
{
    /// <summary>
    ///     注册缓存服务，并将配置根下的 <c>RedisCache</c> 节绑定为 Redis 连接选项。
    /// </summary>
    /// <param name="services">要添加服务的集合。</param>
    /// <param name="configuration">包含 <c>RedisCache</c> 节的应用配置。</param>
    /// <returns>同一个服务集合，便于链式注册。</returns>
    /// <remarks><see cref="CacheServiceOptions"/> 是逐调用选项，不会由此方法从配置文件绑定。</remarks>
    /// <exception cref="ArgumentNullException">服务集合或配置为 <see langword="null"/> 时由依赖注入/配置 API 抛出。</exception>
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
    ///     使用一个默认 Redis 连接字符串注册缓存服务。
    /// </summary>
    /// <param name="services">要添加服务的集合。</param>
    /// <param name="connectionString">名称为 <c>default</c> 的 Redis 连接字符串。</param>
    /// <returns>同一个服务集合，便于链式注册。</returns>
    /// <remarks>连接字符串中的密码等敏感值应来自环境变量、用户机密或部署平台密钥管理。</remarks>
    /// <exception cref="ArgumentNullException">服务集合为 <see langword="null"/> 时由依赖注入 API 抛出。</exception>
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
    ///     使用命名 Redis 实例字典和可选默认连接字符串注册缓存服务。
    /// </summary>
    /// <param name="services">要添加服务的集合。</param>
    /// <param name="redisInstances">实例名称到 Redis 连接字符串的映射。</param>
    /// <param name="defaultConnectionString">名称为 <c>default</c> 的连接字符串；若字典自身含 <c>default</c> 键，也可为 <see langword="null"/>。</param>
    /// <returns>同一个服务集合，便于链式注册。</returns>
    /// <remarks>
    ///     调用时通过 <see cref="CacheServiceOptions.RedisInstanceName"/> 选择实例。连接字符串中的敏感值不应写入源码。
    /// </remarks>
    /// <exception cref="ArgumentNullException">服务集合或实例字典为 <see langword="null"/> 时抛出。</exception>
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
        services.AddSingleton<IRedisCacheStore, RedisCacheStore>();
        services.AddSingleton<IDistributedLockProvider, RedisDistributedLockProvider>();
        services.AddSingleton<ICacheService, CacheService>();
        // 添加内存缓存支持
        services.AddMemoryCache();
    }
}