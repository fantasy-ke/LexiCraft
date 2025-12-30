using BuildingBlocks.Caching.Redis;
using BuildingBlocks.Extensions;
using FreeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BuildingBlocks.Caching.Extensions;

public static class DependencyInjectionExtensions
{
    /// <summary>
    /// 添加 Redis 缓存服务，集成一级本地缓存（FreeRedis ClientSideCaching）
    /// </summary>
    /// <param name="services"></param>
    /// <param name="sectionName">配置节点名称，默认为 RedisCache</param>
    /// <param name="configurator"></param>
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

    private static IServiceCollection AddRedisClient(this IServiceCollection services, Action<ClientSideCachingOptions>? clientCacheOptions = null)
    {
        services.TryAddSingleton(x =>
        {
            var cacheOption = x.GetRequiredService<RedisCacheOptions>();
            var logger = x.GetRequiredService<ILogger<RedisClient>>();
            
            var connectionString = cacheOption.GetConnectionString();

            var redisClient = new RedisClient(connectionString);

            // 配置默认使用Newtonsoft.Json作为序列化工具
            redisClient.Serialize = JsonConvert.SerializeObject;
            redisClient.Deserialize = JsonConvert.DeserializeObject;

            // 注册相关事件
            redisClient.Notice += (_, e) => logger.LogInformation(e.Log);
            redisClient.Connected += (_, e) => logger.LogInformation($"RedisClient_Connected：{e.Host}");
            redisClient.Unavailable += (_, e) => logger.LogWarning($"RedisClient_Unavailable：{e.Host}");

            // 一级缓存（客户端缓存 / 本地缓存）配置
            if (!cacheOption.SideCache.Enable) return redisClient;
            {
                var options = new ClientSideCachingOptions
                {
                    Capacity = cacheOption.SideCache.Capacity,
                    KeyFilter = key => string.IsNullOrEmpty(cacheOption.SideCache.KeyFilterCache) || 
                                       key.StartsWith(cacheOption.SideCache.KeyFilterCache),
                    CheckExpired = (_, dt) => DateTime.Now.Subtract(dt) > TimeSpan.FromMinutes(cacheOption.SideCache.ExpiredMinutes)
                };
                clientCacheOptions?.Invoke(options);
                redisClient.UseClientSideCaching(options);
            }

            return redisClient;
        });

        return services;
    }
}
