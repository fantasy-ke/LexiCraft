using BuildingBlocks.EventBus.Abstractions;
using BuildingBlocks.EventBus.Local;
using BuildingBlocks.EventBus.Options;
using BuildingBlocks.EventBus.Redis;
using BuildingBlocks.EventBus.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BuildingBlocks.EventBus;

/// <summary>
///     混合事件总线实现（支持本地与分布式智能分发）
/// </summary>
public sealed class HybridEventBus<TEvent>(
    IHandlerSerializer serializer,
    ILogger<HybridEventBus<TEvent>> logger,
    IOptions<EventBusOptions> options,
    IServiceProvider serviceProvider) : IEventBus<TEvent> where TEvent : class
{
    private EventBusOptions Options => options.Value;

    public ValueTask PublishAsync(TEvent @event)
    {
        return PublishAsync(@event, CancellationToken.None);
    }

    public async ValueTask PublishAsync(TEvent @event, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (Options.EnableRedis && @event is ISagaIntegrationEvent)
        {
            await PublishDistributedAsync(@event, cancellationToken);
            return;
        }

        if (Options.EnableLocal)
        {
            await PublishLocalAsync(@event, cancellationToken);
            return;
        }

        logger.LogWarning("事件未发布，因为本地与 Redis EventBus 均未启用: {EventType}", @event.GetType().FullName);
    }

    public ValueTask PublishLocalAsync(TEvent @event)
    {
        return PublishLocalAsync(@event, CancellationToken.None);
    }

    public async ValueTask PublishLocalAsync(TEvent @event, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (!Options.EnableLocal)
        {
            logger.LogWarning("尝试发布本地事件但 EnableLocal 为 false: {EventType}", @event.GetType().FullName);
            return;
        }

        var localClient = serviceProvider.GetService<EventLocalClient>()
            ?? throw new EventClientException("本地 EventBus 已启用，但 EventLocalClient 未注册");

        logger.LogDebug("正在发布本地事件: {EventType}", @event.GetType().FullName);
        var eventData = serializer.SerializeJson(@event);
        await localClient.PublishAsync(@event.GetType(), eventData, cancellationToken);
    }

    public ValueTask PublishDistributedAsync(TEvent @event)
    {
        return PublishDistributedAsync(@event, CancellationToken.None);
    }

    public async ValueTask PublishDistributedAsync(TEvent @event, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (!Options.EnableRedis)
        {
            logger.LogWarning("尝试发布分布式事件但 EnableRedis 为 false: {EventType}", @event.GetType().FullName);
            return;
        }

        var connection = serviceProvider.GetService<RedisEventBusConnection>()
            ?? throw new EventClientException("Redis EventBus 已启用，但 Redis 连接未注册");

        var eventType = @event.GetType();
        var channelName = GetRedisChannelName(eventType);
        var eventData = serializer.SerializeJson(@event);
        var eventEto = new EventEto(eventType.FullName ?? eventType.Name, eventData);
        var payload = serializer.SerializeJson(eventEto);

        try
        {
            var subscriber = connection.Multiplexer.GetSubscriber();
            await subscriber
                .PublishAsync(RedisChannel.Literal(channelName), payload)
                .WaitAsync(cancellationToken);
            logger.LogInformation("已发布分布式事件到 {Channel}: {EventType}", channelName, eventType.Name);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "发布分布式集成事件失败: {EventType}", eventType.FullName);
            throw new EventClientException($"发布分布式事件失败: {eventType.FullName}", ex);
        }
    }

    private string GetRedisChannelName(Type eventType)
    {
        var attribute = eventType.GetCustomAttributes(typeof(EventSchemeAttribute), true)
            .OfType<EventSchemeAttribute>()
            .FirstOrDefault();
        var name = attribute?.EventName ?? eventType.FullName ?? eventType.Name;
        return string.IsNullOrEmpty(Options.Redis.Prefix) ? name : $"{Options.Redis.Prefix}:{name}";
    }
}
