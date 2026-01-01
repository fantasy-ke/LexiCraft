using FreeRedis;
using Microsoft.Extensions.Logging;
using Z.EventBus;
using Z.Local.EventBus;
using Z.Local.EventBus.Serializer;

namespace Z.Redis.EventBus;

/// <summary>
/// 基于 Redis 的分布式事件总线发布者
/// </summary>
/// <typeparam name="TEvent"></typeparam>
public class RedisEventBus<TEvent>(
    RedisClient redisClient,
    ILogger<RedisEventBus<TEvent>> logger,
    IHandlerSerializer handlerSerializer) : IEventBus<TEvent> where TEvent : class
{
    public async ValueTask PublishAsync(TEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var eventType = @event.GetType();
        var channelName = GetChannelName(eventType);
        
        var eventData = handlerSerializer.SerializeJson(@event);
        var eventEto = new EventEto(eventType.FullName!, eventData);
        var payload = handlerSerializer.SerializeJson(eventEto);

        try
        {
            logger.LogInformation("正在通过 Redis 发布事件: {Channel}, CorrelationId: {CorrelationId}", 
                channelName, (@event as ISagaIntegrationEvent)?.CorrelationId);
            
            await redisClient.PublishAsync(channelName, payload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "发布 Redis 事件失败: {Message}", ex.Message);
            throw;
        }
    }

    private string GetChannelName(Type eventType)
    {
        var attribute = eventType.GetCustomAttributes(typeof(EventSchemeAttribute), true).FirstOrDefault() as EventSchemeAttribute;
        return attribute?.EventName ?? eventType.FullName!;
    }
}
