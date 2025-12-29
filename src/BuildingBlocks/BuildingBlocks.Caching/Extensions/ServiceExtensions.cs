using BuildingBlocks.Caching.Redis;
using BuildingBlocks.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Caching.Extensions;

public static class CachingServiceExtensions
{
    /// <summary>
    /// 添加 Redis 缓存服务，集成一级本地缓存（FreeRedis ClientSideCaching）
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <param name="sectionName">配置节点名称，默认为 RedisCache</param>
    /// <returns></returns>
    public static IServiceCollection AddRedisCaching(this IServiceCollection services, string sectionName = "RedisCache", Action<RedisCacheOptions>? configurator = null)
    {
        // 1. 绑定配置并注入 RedisCacheOptions
        services.AddConfigurationOptions<RedisCacheOptions>(sectionName, configurator);

        // 2. 注入 FreeRedis 客户端
        services.AddRedisClient();

        // 3. 注入 缓存管理器
        services.AddSingleton<ICacheManager, CacheManager>();

        return services;
    }
}
