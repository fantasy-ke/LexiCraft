using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using FreeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Z.EventBus;
using Z.Local.EventBus;
using Z.Local.EventBus.Serializer;

namespace Z.Redis.EventBus;

/// <summary>
/// Redis 事件消费者托管服务
/// </summary>
public class RedisEventConsumerService(
    RedisClient redisClient,
    IServiceProvider serviceProvider,
    ILogger<RedisEventConsumerService> logger,
    IHandlerSerializer handlerSerializer)
    : BackgroundService
{
    private readonly ConcurrentDictionary<Type, Func<object, object, CancellationToken, Task>> _handlerCache = new();
    private readonly ConcurrentDictionary<string, Type> _typeCache = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var eventTypes = DiscoverEventTypesWithHandlers();

        var iEnumerable = eventTypes as Type[] ?? eventTypes.ToArray();
        if (!iEnumerable.Any())
        {
            logger.LogWarning("未发现任何已注册的 IEventHandler，Redis 消费者将不订阅任何频道。");
            return;
        }

        foreach (var eventType in iEnumerable)
        {
            var channelName = GetChannelName(eventType);
            _typeCache.TryAdd(eventType.FullName!, eventType);
            
            logger.LogInformation("正在订阅 Redis 频道: {Channel} 用于事件 {Event}", channelName, eventType.Name);
            
            redisClient.Subscribe(channelName, (chan, msg) => 
            {
                _ = Task.Run(async () => await HandleMessageAsync(chan, msg as string, stoppingToken), stoppingToken);
            });
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private IEnumerable<Type> DiscoverEventTypesWithHandlers()
    {
        // 查找当前 AppDomain 中所有实现了 IEventHandler<T> 的具体类
        // 优化：仅订阅那些实现了 ISagaIntegrationEvent 或被明确标记为集成事件的类型
        // 这样可以避免将纯本地事件（LocalEvent）误注册到 Redis 频道
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces())
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>))
            .Select(i => i.GetGenericArguments()[0])
            .Where(eventType => typeof(ISagaIntegrationEvent).IsAssignableFrom(eventType)) // 关键过滤器
            .Distinct();
    }

    private string GetChannelName(Type eventType)
    {
        var attribute = eventType.GetCustomAttributes(typeof(EventSchemeAttribute), true).FirstOrDefault() as EventSchemeAttribute;
        return attribute?.EventName ?? eventType.FullName!;
    }

    private async Task HandleMessageAsync(string channel, string? payload, CancellationToken cancellationToken)
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

            if (eventType == null)
            {
                logger.LogWarning("收到未知事件类型: {FullName}", eventEto.FullName);
                return;
            }

            var eventData = handlerSerializer.Deserialize(eventEto.Data, eventType);
            if (eventData == null) return;

            // 幂等性校验：如果是集成事件，利用其唯一 ID 进行占位
            if (eventData is ISagaIntegrationEvent sagaEvent && sagaEvent is IntegrationEvent integrationEvent)
            {
                var idempotencyKey = $"lexicraft:idempotency:{eventEto.FullName}:{integrationEvent.Id}";
                // 尝试占位，设置 24 小时过期
                var success = await redisClient.SetNxAsync(idempotencyKey, "1", 86400);
                if (!success) // FreeRedis Set Nx 返回 "OK" 或 null/空
                {
                    logger.LogInformation("检测到重复消息，已跳过处理: {EventId}, Type: {EventType}", integrationEvent.Id, eventEto.FullName);
                    return;
                }
            }

            await ProcessEventAsync(eventType, eventData, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "处理 Redis 消息时出错: {Message}", ex.Message);
        }
    }

    private async Task ProcessEventAsync(Type eventType, object eventData, CancellationToken cancellationToken)
    {
        var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);
        
        using var scope = serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices(handlerType).ToArray();

        foreach (var handler in handlers)
        {
            if (handler == null) continue;

            var handlerDelegate = _handlerCache.GetOrAdd(eventType, type =>
            {
                var handlerParam = Expression.Parameter(typeof(object), "h");
                var dataParam = Expression.Parameter(typeof(object), "d");
                var tokenParam = Expression.Parameter(typeof(CancellationToken), "t");

                var method = handlerType.GetMethod("HandleAsync", [type, typeof(CancellationToken)]);
                var call = Expression.Call(Expression.Convert(handlerParam, handlerType), method!, Expression.Convert(dataParam, type), tokenParam);

                return Expression.Lambda<Func<object, object, CancellationToken, Task>>(call, handlerParam, dataParam, tokenParam).Compile();
            });

            await handlerDelegate(handler, eventData, cancellationToken);
        }
    }
}
