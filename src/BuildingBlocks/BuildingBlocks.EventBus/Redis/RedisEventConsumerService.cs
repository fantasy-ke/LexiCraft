using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using BuildingBlocks.EventBus.Abstractions;
using BuildingBlocks.EventBus.Options;
using BuildingBlocks.EventBus.Shared;
using FreeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.EventBus.Redis;

/// <summary>
/// Redis 事件消费者托管服务 (分布式实现)
/// </summary>
public class RedisEventConsumerService(
    RedisClient redisClient,
    IServiceProvider serviceProvider,
    ILogger<RedisEventConsumerService> logger,
    IHandlerSerializer handlerSerializer,
    IOptionsMonitor<EventBusOptions> options)
    : BackgroundService
{
    private readonly ConcurrentDictionary<Type, Func<object, object, CancellationToken, Task>> _handlerCache = new();
    private readonly ConcurrentDictionary<string, Type> _typeCache = new();
    private EventBusOptions _options => options.CurrentValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableRedis) return;

        var eventTypes = DiscoverEventTypesWithHandlers();
        var iEnumerable = eventTypes as Type[] ?? eventTypes.ToArray();
        if (!iEnumerable.Any()) return;

        foreach (var eventType in iEnumerable)
        {
            var channelName = GetChannelName(eventType);
            _typeCache.TryAdd(eventType.FullName!, eventType);
            
            logger.LogInformation("正在订阅 Redis 频道: {Channel} 用于事件 {Event}", channelName, eventType.Name);
            
            redisClient.Subscribe(channelName, (channel, msg) => 
            {
                _ = Task.Run(async () => await HandleMessageAsync(msg as string, stoppingToken), stoppingToken);
            });
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
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
        var attribute = eventType.GetCustomAttributes(typeof(EventSchemeAttribute), true).FirstOrDefault() as EventSchemeAttribute;
        var name = attribute?.EventName ?? eventType.FullName!;
        return string.IsNullOrEmpty(_options.Redis.Prefix) ? name : $"{_options.Redis.Prefix}:{name}";
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
                // 使用 SetNxAsync。注意：FreeRedis 的 SetNxAsync 仅支持 key, value。
                // 若要设置过期时间，需额外调用 ExpireAsync。
                var isNew = await redisClient.SetNxAsync(idempotencyKey, "1");
                if (!isNew) 
                {
                    logger.LogInformation("检测到重复消息，已跳过处理: {EventId}, Type: {EventType}", integrationEvent.Id, eventEto.FullName);
                    return;
                }
                // 设置过期时间
                await redisClient.ExpireAsync(idempotencyKey, _options.Redis.IdempotencyExpireSeconds);
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
        var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);
        using var scope = serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices(handlerType).ToArray();

        foreach (var handler in handlers)
        {
            if (handler == null) continue;
            var handlerDelegate = _handlerCache.GetOrAdd(eventType, type =>
            {
                var hParam = Expression.Parameter(typeof(object), "h");
                var dParam = Expression.Parameter(typeof(object), "d");
                var tParam = Expression.Parameter(typeof(CancellationToken), "t");
                var method = handlerType.GetMethod("HandleAsync", [type, typeof(CancellationToken)]);
                var call = Expression.Call(Expression.Convert(hParam, handlerType), method!, Expression.Convert(dParam, type), tParam);
                return Expression.Lambda<Func<object, object, CancellationToken, Task>>(call, hParam, dParam, tParam).Compile();
            });
            await handlerDelegate(handler, eventData, cancellationToken);
        }
    }
}
