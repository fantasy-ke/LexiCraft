using BuildingBlocks.Caching.Abstractions;
using BuildingBlocks.Caching.Configuration;
using BuildingBlocks.Caching.DistributedLock;
using BuildingBlocks.Caching.Factories;
using BuildingBlocks.Caching.Serialization;
using BuildingBlocks.Caching.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;

namespace BuildingBlocks.Caching.Extensions;

/// <summary>
/// 依赖注入扩展方法
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// 添加缓存服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <param name="configureOptions">配置选项委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<CacheServiceOptions>? configureOptions = null)
    {
        // 注册配置选项
        var optionsBuilder = services.Configure<CacheServiceOptions>(configuration.GetSection("CacheService"));
        if (configureOptions != null)
        {
            optionsBuilder.Configure(configureOptions);
        }

        // 注册 Redis 连接选项
        services.Configure<RedisConnectionOptions>(configuration.GetSection("Redis"));

        // 注册核心服务
        RegisterCoreServices(services);

        return services;
    }

    /// <summary>
    /// 添加缓存服务（使用连接字符串）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">Redis 连接字符串</param>
    /// <param name="configureOptions">配置选项委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCaching(
        this IServiceCollection services,
        string connectionString,
        Action<CacheServiceOptions>? configureOptions = null)
    {
        // 配置 Redis 连接选项
        services.Configure<RedisConnectionOptions>(options =>
        {
            options.DefaultConnectionString = connectionString;
        });

        // 配置缓存服务选项
        if (configureOptions != null)
        {
            services.Configure<CacheServiceOptions>(configureOptions);
        }

        // 注册核心服务
        RegisterCoreServices(services);

        return services;
    }

    /// <summary>
    /// 添加缓存服务（使用多个 Redis 实例）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="redisInstances">Redis 实例配置字典</param>
    /// <param name="defaultConnectionString">默认连接字符串</param>
    /// <param name="configureOptions">配置选项委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCaching(
        this IServiceCollection services,
        Dictionary<string, string> redisInstances,
        string? defaultConnectionString = null,
        Action<CacheServiceOptions>? configureOptions = null)
    {
        // 配置 Redis 连接选项
        services.Configure<RedisConnectionOptions>(options =>
        {
            options.DefaultConnectionString = defaultConnectionString;
            foreach (var instance in redisInstances)
            {
                options.Instances[instance.Key] = instance.Value;
            }
        });

        // 配置缓存服务选项
        if (configureOptions != null)
        {
            services.Configure<CacheServiceOptions>(configureOptions);
        }

        // 注册核心服务
        RegisterCoreServices(services);

        return services;
    }

    /// <summary>
    /// 添加缓存服务（使用预设配置）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <param name="presetOptions">预设配置选项</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration,
        CacheServiceOptions presetOptions)
    {
        return services.AddCaching(configuration, options =>
        {
            // 复制预设配置的属性
            options.UseDistributed = presetOptions.UseDistributed;
            options.UseLocal = presetOptions.UseLocal;
            options.Expiry = presetOptions.Expiry;
            options.LocalExpiry = presetOptions.LocalExpiry;
            options.HideErrors = presetOptions.HideErrors;
            options.EnableCompression = presetOptions.EnableCompression;
            options.EnableBinarySerialization = presetOptions.EnableBinarySerialization;
            options.EnableLock = presetOptions.EnableLock;
            options.LockTimeout = presetOptions.LockTimeout;
            options.LockAcquireTimeout = presetOptions.LockAcquireTimeout;
            options.FallbackToFactory = presetOptions.FallbackToFactory;
            options.FallbackToDefault = presetOptions.FallbackToDefault;
            options.DefaultValue = presetOptions.DefaultValue;
            options.FallbackFunction = presetOptions.FallbackFunction;
            options.OnError = presetOptions.OnError;
            options.AdjustExpiryForHash = presetOptions.AdjustExpiryForHash;
            options.AdjustExpiryForValue = presetOptions.AdjustExpiryForValue;
            options.RedisInstanceName = presetOptions.RedisInstanceName;
        });
    }

    /// <summary>
    /// 添加分布式缓存服务（仅分布式缓存）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <param name="configureOptions">配置选项委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddDistributedCaching(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<CacheServiceOptions>? configureOptions = null)
    {
        return services.AddCaching(configuration, options =>
        {
            options.UseDistributed = true;
            options.UseLocal = false;
            configureOptions?.Invoke(options);
        });
    }

    /// <summary>
    /// 添加本地缓存服务（仅本地缓存）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <param name="configureOptions">配置选项委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddLocalCaching(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<CacheServiceOptions>? configureOptions = null)
    {
        return services.AddCaching(configuration, options =>
        {
            options.UseDistributed = false;
            options.UseLocal = true;
            options.EnableLock = false; // 本地缓存不需要分布式锁
            configureOptions?.Invoke(options);
        });
    }

    /// <summary>
    /// 添加混合缓存服务（本地 + 分布式）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <param name="configureOptions">配置选项委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddHybridCaching(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<CacheServiceOptions>? configureOptions = null)
    {
        return services.AddCaching(configuration, options =>
        {
            options.UseDistributed = true;
            options.UseLocal = true;
            configureOptions?.Invoke(options);
        });
    }

    /// <summary>
    /// 添加高性能缓存服务（启用二进制序列化和压缩）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <param name="configureOptions">配置选项委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddHighPerformanceCaching(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<CacheServiceOptions>? configureOptions = null)
    {
        return services.AddCaching(configuration, options =>
        {
            options.UseDistributed = true;
            options.UseLocal = true;
            options.EnableBinarySerialization = true;
            options.EnableCompression = true;
            options.EnableLock = true;
            configureOptions?.Invoke(options);
        });
    }

    /// <summary>
    /// 添加开发环境缓存服务（不隐藏异常，便于调试）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <param name="configureOptions">配置选项委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddDevelopmentCaching(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<CacheServiceOptions>? configureOptions = null)
    {
        return services.AddCaching(configuration, options =>
        {
            options.UseDistributed = true;
            options.UseLocal = true;
            options.HideErrors = false; // 开发环境不隐藏异常
            options.EnableLock = false; // 开发环境简化配置
            options.Expiry = TimeSpan.FromMinutes(10); // 较短的过期时间
            options.LocalExpiry = TimeSpan.FromMinutes(2);
            configureOptions?.Invoke(options);
        });
    }

    /// <summary>
    /// 添加生产环境缓存服务（高可用性和性能优化）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <param name="configureOptions">配置选项委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddProductionCaching(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<CacheServiceOptions>? configureOptions = null)
    {
        return services.AddCaching(configuration, options =>
        {
            options.UseDistributed = true;
            options.UseLocal = true;
            options.EnableBinarySerialization = true;
            options.EnableCompression = true;
            options.EnableLock = true;
            options.HideErrors = true;
            options.FallbackToFactory = true;
            options.Expiry = TimeSpan.FromHours(4);
            options.LocalExpiry = TimeSpan.FromMinutes(30);
            options.LockTimeout = TimeSpan.FromSeconds(2);
            options.LockAcquireTimeout = TimeSpan.FromSeconds(2);
            configureOptions?.Invoke(options);
        });
    }

    /// <summary>
    /// 添加 Redis 缓存（兼容方法）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项委托</param>
    /// <returns>服务集合</returns>
    [Obsolete("请使用 AddCaching 或 AddDistributedCaching 替代")]
    public static IServiceCollection AddRedisCaching(
        this IServiceCollection services,
        Action<CacheServiceOptions>? configureOptions = null)
    {
        // 从配置中获取 IConfiguration
        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        
        return services.AddDistributedCaching(configuration, configureOptions);
    }

    /// <summary>
    /// 注册核心服务
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void RegisterCoreServices(IServiceCollection services)
    {
        // 注册 Redis 连接工厂
        services.AddSingleton<IRedisConnectionFactory, RedisConnectionFactory>();

        // 注册序列化器
        services.AddSingleton<JsonCacheSerializer>();
        services.AddSingleton<MemoryPackCacheSerializer>();
        services.AddSingleton<GZipCacheCompressor>();

        // 注册缓存服务
        services.AddSingleton<IDistributedCacheService, DistributedCacheService>();
        services.AddSingleton<IDistributedLockProvider, RedisDistributedLockProvider>();
        services.AddSingleton<ICacheService, CacheService>();

        // 注册兼容层（向后兼容）
        services.AddSingleton<ICacheManager, CacheManagerAdapter>();

        // 添加内存缓存支持
        services.AddMemoryCache();
    }

    /// <summary>
    /// 复制预设配置选项
    /// </summary>
    /// <param name="source">源配置</param>
    /// <param name="target">目标配置</param>
    private static void CopyPresetOptions(CacheServiceOptions source, CacheServiceOptions target)
    {
        target.UseDistributed = source.UseDistributed;
        target.UseLocal = source.UseLocal;
        target.Expiry = source.Expiry;
        target.LocalExpiry = source.LocalExpiry;
        target.HideErrors = source.HideErrors;
        target.EnableCompression = source.EnableCompression;
        target.EnableBinarySerialization = source.EnableBinarySerialization;
        target.EnableLock = source.EnableLock;
        target.LockTimeout = source.LockTimeout;
        target.LockAcquireTimeout = source.LockAcquireTimeout;
        target.FallbackToFactory = source.FallbackToFactory;
        target.FallbackToDefault = source.FallbackToDefault;
        target.DefaultValue = source.DefaultValue;
        target.FallbackFunction = source.FallbackFunction;
        target.OnError = source.OnError;
        target.AdjustExpiryForHash = source.AdjustExpiryForHash;
        target.AdjustExpiryForValue = source.AdjustExpiryForValue;
        target.RedisInstanceName = source.RedisInstanceName;
    }

    /// <summary>
    /// 添加缓存服务（使用连接字符串和预设配置）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">Redis 连接字符串</param>
    /// <param name="presetOptions">预设配置选项</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCaching(
        this IServiceCollection services,
        string connectionString,
        CacheServiceOptions presetOptions)
    {
        return services.AddCaching(connectionString, options => CopyPresetOptions(presetOptions, options));
    }

    /// <summary>
    /// 添加分布式缓存服务（使用连接字符串）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">Redis 连接字符串</param>
    /// <param name="configureOptions">配置选项委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddDistributedCaching(
        this IServiceCollection services,
        string connectionString,
        Action<CacheServiceOptions>? configureOptions = null)
    {
        return services.AddCaching(connectionString, options =>
        {
            options.UseDistributed = true;
            options.UseLocal = false;
            configureOptions?.Invoke(options);
        });
    }

    /// <summary>
    /// 添加本地缓存服务（仅本地缓存，不需要 Redis）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddLocalCaching(
        this IServiceCollection services,
        Action<CacheServiceOptions>? configureOptions = null)
    {
        // 配置缓存服务选项
        services.Configure<CacheServiceOptions>(options =>
        {
            options.UseDistributed = false;
            options.UseLocal = true;
            options.EnableLock = false; // 本地缓存不需要分布式锁
            configureOptions?.Invoke(options);
        });

        // 只注册本地缓存相关的服务
        services.AddSingleton<ICacheService, CacheService>();
        services.AddSingleton<ICacheManager, CacheManagerAdapter>();
        services.AddMemoryCache();

        return services;
    }

    /// <summary>
    /// 添加混合缓存服务（使用连接字符串）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">Redis 连接字符串</param>
    /// <param name="configureOptions">配置选项委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddHybridCaching(
        this IServiceCollection services,
        string connectionString,
        Action<CacheServiceOptions>? configureOptions = null)
    {
        return services.AddCaching(connectionString, options =>
        {
            options.UseDistributed = true;
            options.UseLocal = true;
            configureOptions?.Invoke(options);
        });
    }

    /// <summary>
    /// 添加缓存健康检查
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCacheHealthChecks(this IServiceCollection services)
    {
        services.AddSingleton<ICacheHealthCheck, CacheHealthCheck>();
        return services;
    }
}