using FreeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace BuildingBlocks.Caching.Redis;

public static class RedisExtensions
{
    public static IServiceCollection AddRedisClient(this IServiceCollection services, Action<ClientSideCachingOptions>? clientCacheOptions = null)
    {
        services.TryAddSingleton(x =>
        {
            var cacheOption = x.GetRequiredService<RedisCacheOptions>();
            var logger = x.GetRequiredService<ILogger<RedisClient>>();
            
            if (string.IsNullOrEmpty(cacheOption.Configuration))
            {
                throw new ArgumentNullException(nameof(cacheOption.ConnectionString), "Redis connection string is not configured.");
            }

            var redisClient = new RedisClient(cacheOption.Configuration);

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
