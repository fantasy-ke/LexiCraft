using BuildingBlocks.EventBus.Abstractions;
using BuildingBlocks.EventBus.Local;
using BuildingBlocks.EventBus.Options;
using BuildingBlocks.EventBus.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.EventBus;

/// <summary>
/// 混合事件总线实现 (支持本地与分布式智能分发)
/// </summary>
public class HybridEventBus<TEvent>(
    EventLocalClient localClient,
    IHandlerSerializer serializer,
    ILogger<HybridEventBus<TEvent>> logger,
    IOptionsSnapshot<EventBusOptions> options,
    IServiceProvider serviceProvider) : IEventBus<TEvent> where TEvent : class
{
    private readonly EventBusOptions _options = options.Value;

    public async ValueTask PublishAsync(TEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        // 1. 本地分发 (如果启用)
        if (_options.EnableLocal)
        {
            var eventData = serializer.SerializeJson(@event);
            await localClient.PublishAsync(@event.GetType(), eventData);
        }

        // 2. Redis 分布式分发 (如果是集成事件且启用 Redis)
        if (_options.EnableRedis && @event is ISagaIntegrationEvent)
        {
            var redisClient = (FreeRedis.RedisClient?)serviceProvider.GetService(typeof(FreeRedis.RedisClient));
            if (redisClient != null)
            {
                var eventType = @event.GetType();
                var channelName = GetRedisChannelName(eventType);
                var eventData = serializer.SerializeJson(@event);
                var eventEto = new EventEto(eventType.FullName!, eventData);
                var payload = serializer.SerializeJson(eventEto);

                try
                {
                    await redisClient.PublishAsync(channelName, payload);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "发布分布式集成事件失败");
                }
            }
        }
    }

    private string GetRedisChannelName(Type eventType)
    {
        var attribute = eventType.GetCustomAttributes(typeof(EventSchemeAttribute), true).FirstOrDefault() as EventSchemeAttribute;
        var name = attribute?.EventName ?? eventType.FullName!;
        return string.IsNullOrEmpty(_options.Redis.Prefix) ? name : $"{_options.Redis.Prefix}:{name}";
    }
}
