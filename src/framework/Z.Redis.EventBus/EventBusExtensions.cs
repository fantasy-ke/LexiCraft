using FreeRedis;
using Microsoft.Extensions.DependencyInjection;
using Z.EventBus;
using Z.Local.EventBus.Serializer;

namespace Z.Redis.EventBus;

/// <summary>
/// Redis 事件总线扩展
/// </summary>
public static class EventBusExtensions
{
    /// <summary>
    /// 添加基于 Redis 的分布式事件总线
    /// </summary>
    /// <param name="services"></param>
    /// <param name="redisConnectionString">Redis 连接字符串，如果为空则尝试从容器获取 RedisClient</param>
    /// <returns></returns>
    public static IServiceCollection AddRedisEventBus(this IServiceCollection services, string? redisConnectionString = null)
    {
        // 如果提供了连接字符串，则注册 RedisClient
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddSingleton(new RedisClient(redisConnectionString));
        }

        // 注册序列化器（复用 Z.Local.EventBus 的实现）
        services.AddSingleton<IHandlerSerializer, JsonHandlerSerializer>();

        // 注册泛型事件总线
        services.AddSingleton(typeof(IEventBus<>), typeof(RedisEventBus<>));

        // 注册消费者托管服务
        services.AddHostedService<RedisEventConsumerService>();

        return services;
    }
}
