using BuildingBlocks.EventBus.Abstractions;
using BuildingBlocks.EventBus.Local;
using BuildingBlocks.EventBus.Options;
using BuildingBlocks.EventBus.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BuildingBlocks.EventBus;

/// <summary>
///     混合事件总线实现 (支持本地与分布式智能分发)
/// </summary>
public class HybridEventBus<TEvent>(
    EventLocalClient localClient,
    IHandlerSerializer serializer,
    ILogger<HybridEventBus<TEvent>> logger,
    IOptionsMonitor<EventBusOptions> options,
    IServiceProvider serviceProvider) : IEventBus<TEvent> where TEvent : class
{
    private EventBusOptions Options => options.CurrentValue;

    public async ValueTask PublishAsync(TEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (Options.EnableRedis && @event is ISagaIntegrationEvent)
        {
            await PublishDistributedAsync(@event);
        }
        // 否则走本地通道 (如果启用)
        else if (Options.EnableLocal)
        {
            await PublishLocalAsync(@event);
        }
    }

    public async ValueTask PublishLocalAsync(TEvent @event)
    {
        if (!Options.EnableLocal)
        {
            logger.LogWarning("尝试发布本地事件但 EnableLocal 为 false: {EventType}", typeof(TEvent).Name);
            return;
        }

        logger.LogDebug("Publishing local event: {@Event}", @event);
        var eventData = serializer.SerializeJson(@event);
        await localClient.PublishAsync(@event.GetType(), eventData);
    }

    public async ValueTask PublishDistributedAsync(TEvent @event)
    {
        if (!Options.EnableRedis)
        {
            logger.LogWarning("尝试发布分布式事件但 EnableRedis 为 false: {EventType}", typeof(TEvent).Name);
            return;
        }

        var redis = serviceProvider.GetService<IConnectionMultiplexer>();
        if (redis == null)
        {
            logger.LogError("Redis 连接未注册，无法发布分布式事件");
            return;
        }

        var eventType = @event.GetType();
        var channelName = GetRedisChannelName(eventType);
        var eventData = serializer.SerializeJson(@event);
        var eventEto = new EventEto(eventType.FullName ?? string.Empty, eventData);
        var payload = serializer.SerializeJson(eventEto);

        try
        {
            var sub = redis.GetSubscriber();
            await sub.PublishAsync(RedisChannel.Literal(channelName), payload);
            logger.LogInformation("已发布分布式事件到 {Channel}: {EventType}", channelName, eventType.Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "发布分布式集成事件失败");
        }
    }

    private string GetRedisChannelName(Type eventType)
    {
        var attribute =
            eventType.GetCustomAttributes(typeof(EventSchemeAttribute), true).FirstOrDefault() as EventSchemeAttribute;
        var name = attribute?.EventName ?? eventType.FullName ?? string.Empty;
        return string.IsNullOrEmpty(Options.Redis.Prefix) ? name : $"{Options.Redis.Prefix}:{name}";
    }
}