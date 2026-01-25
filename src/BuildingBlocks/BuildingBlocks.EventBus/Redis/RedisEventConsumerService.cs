using System.Collections.Concurrent;
using System.Linq.Expressions;
using BuildingBlocks.EventBus.Abstractions;
using BuildingBlocks.EventBus.Options;
using BuildingBlocks.EventBus.Shared;
using StackExchange.Redis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.EventBus.Redis;

/// <summary>
///     Redis 事件消费者托管服务 (分布式实现)
/// </summary>
public class RedisEventConsumerService(
    IConnectionMultiplexer connectionMultiplexer,
    IServiceProvider serviceProvider,
    ILogger<RedisEventConsumerService> logger,
    IHandlerSerializer handlerSerializer,
    IOptionsMonitor<EventBusOptions> options)
    : BackgroundService
{
    private readonly ConcurrentDictionary<Type, Func<object, object, CancellationToken, Task>> _handlerCache = new();
    private readonly ConcurrentDictionary<string, Type> _typeCache = new();
    private EventBusOptions Options => options.CurrentValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Options.EnableRedis) return;

        var eventTypes = DiscoverEventTypesWithHandlers();
        var iEnumerable = eventTypes as Type[] ?? eventTypes.ToArray();
        if (!iEnumerable.Any()) return;

        var subscriber = connectionMultiplexer.GetSubscriber();

        foreach (var eventType in iEnumerable)
        {
            var channelName = GetChannelName(eventType);
            _typeCache.TryAdd(eventType.FullName!, eventType);

            logger.LogInformation("正在订阅 Redis 频道: {Channel} 用于事件 {Event}", channelName, eventType.Name);
            
            await subscriber.SubscribeAsync(RedisChannel.Literal(channelName), (channel, msg) => 
            {
                _ = Task.Run(async () => await HandleMessageAsync(msg, stoppingToken), stoppingToken);
            });
        }

        while (!stoppingToken.IsCancellationRequested) await Task.Delay(1000, stoppingToken);
    }

    private IEnumerable<Type> DiscoverEventTypesWithHandlers()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces())
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>))
            .Select(i => i.GetGenericArguments()[0])
            .Where(eventType => typeof(ISagaIntegrationEvent).IsAssignableFrom(eventType))
            .Distinct();
    }

    private string GetChannelName(Type eventType)
    {
        var attribute =
            eventType.GetCustomAttributes(typeof(EventSchemeAttribute), true).FirstOrDefault() as EventSchemeAttribute;
        var name = attribute?.EventName ?? eventType.FullName!;
        return string.IsNullOrEmpty(Options.Redis.Prefix) ? name : $"{Options.Redis.Prefix}:{name}";
    }

    private async Task HandleMessageAsync(string? payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(payload)) return;
        try
        {
            var eventEto = handlerSerializer.Deserialize<EventEto>(payload);
            if (eventEto == null) return;

            if (!_typeCache.TryGetValue(eventEto.FullName, out var eventType))
            {
                eventType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.FullName == eventEto.FullName);
                if (eventType != null) _typeCache.TryAdd(eventEto.FullName, eventType);
            }

            if (eventType == null) return;
            var eventData = handlerSerializer.Deserialize(eventEto.Data, eventType);
            if (eventData == null) return;

            // 幂等性校验
            if (eventData is IntegrationEvent integrationEvent)
            {
                var idempotencyKey = $"lexicraft:idempotency:{eventEto.FullName}:{integrationEvent.Id}";
                var db = connectionMultiplexer.GetDatabase();
                
                // 使用 SetNx (When.NotExists) 并设置过期时间
                var isNew = await db.StringSetAsync(
                    idempotencyKey, 
                    "1", 
                    TimeSpan.FromSeconds(Options.Redis.IdempotencyExpireSeconds), 
                    When.NotExists
                );

                if (!isNew) 
                {
                    logger.LogInformation("检测到重复消息，已跳过处理: {EventId}, Type: {EventType}", integrationEvent.Id,
                        eventEto.FullName);
                    return;
                }
            }

            await ProcessEventAsync(eventType, eventData, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "处理 Redis 消息时出错");
        }
    }

    private async Task ProcessEventAsync(Type eventType, object eventData, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);
        var handlers = scope.ServiceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            if (handler == null) continue;
            
            var handlerKey = handler.GetType();
            var method = _handlerCache.GetOrAdd(handlerKey, key =>
            {
                var handleMethod = key.GetMethod("HandleAsync", [eventType, typeof(CancellationToken)]);
                if (handleMethod == null) return (_, _, _) => Task.CompletedTask;

                var instanceParam = Expression.Parameter(typeof(object), "instance");
                var eventParam = Expression.Parameter(typeof(object), "event");
                var tokenParam = Expression.Parameter(typeof(CancellationToken), "token");

                var castInstance = Expression.Convert(instanceParam, key);
                var castEvent = Expression.Convert(eventParam, eventType);

                var call = Expression.Call(castInstance, handleMethod, castEvent, tokenParam);
                return Expression.Lambda<Func<object, object, CancellationToken, Task>>(call, instanceParam, eventParam, tokenParam).Compile();
            });

            await method(handler, eventData, cancellationToken);
        }
    }
}