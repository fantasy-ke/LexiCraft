using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Channels;
using BuildingBlocks.EventBus.Abstractions;
using BuildingBlocks.EventBus.Options;
using BuildingBlocks.EventBus.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BuildingBlocks.EventBus.Redis;

/// <summary>
///     Redis 事件消费者托管服务（分布式实现）
/// </summary>
public sealed class RedisEventConsumerService : BackgroundService
{
    private readonly RedisEventBusConnection _connection;
    private readonly EventHandlerInvoker _handlerInvoker;
    private readonly IHandlerSerializer _handlerSerializer;
    private readonly ILogger<RedisEventConsumerService> _logger;
    private readonly Channel<ChannelMessage> _messageQueue;
    private readonly EventBusOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly List<ChannelMessageQueue> _subscriptions = [];
    private readonly ConcurrentDictionary<string, Type> _typeCache = new();
    private long _droppedMessages;

    public RedisEventConsumerService(
        RedisEventBusConnection connection,
        IServiceProvider serviceProvider,
        ILogger<RedisEventConsumerService> logger,
        IHandlerSerializer handlerSerializer,
        EventHandlerInvoker handlerInvoker,
        IOptions<EventBusOptions> options)
    {
        _connection = connection;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _handlerSerializer = handlerSerializer;
        _handlerInvoker = handlerInvoker;
        _options = options.Value;
        _messageQueue = Channel.CreateBounded<ChannelMessage>(
            new BoundedChannelOptions(_options.Redis.ConsumerQueueCapacity)
            {
                SingleReader = _options.Redis.ConsumerConcurrency == 1,
                SingleWriter = false,
                AllowSynchronousContinuations = false,
                FullMode = BoundedChannelFullMode.Wait
            });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableRedis) return;

        var eventTypes = DiscoverEventTypesWithHandlers().ToArray();
        if (eventTypes.Length == 0)
        {
            _logger.LogInformation("未发现 Redis 集成事件处理器，跳过频道订阅");
            return;
        }

        var workers = Enumerable.Range(0, _options.Redis.ConsumerConcurrency)
            .Select(_ => ConsumeMessagesAsync(stoppingToken))
            .ToArray();

        try
        {
            var subscriber = _connection.Multiplexer.GetSubscriber();

            foreach (var eventType in eventTypes)
            {
                stoppingToken.ThrowIfCancellationRequested();

                var channelName = GetChannelName(eventType);
                if (!string.IsNullOrEmpty(eventType.FullName)) _typeCache.TryAdd(eventType.FullName, eventType);

                var subscription = await subscriber
                    .SubscribeAsync(RedisChannel.Literal(channelName))
                    .WaitAsync(stoppingToken);

                subscription.OnMessage(message => EnqueueMessage(message, stoppingToken));
                _subscriptions.Add(subscription);

                _logger.LogInformation("正在订阅 Redis 频道: {Channel} 用于事件 {Event}",
                    channelName, eventType.Name);
            }

            await Task.WhenAll(workers);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Redis 事件消费者正在停止");
        }
        finally
        {
            await UnsubscribeAsync();
            _messageQueue.Writer.TryComplete();

            try
            {
                await Task.WhenAll(workers);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Host 正常停止。
            }
        }
    }

    private void EnqueueMessage(ChannelMessage message, CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested) return;
        if (_messageQueue.Writer.TryWrite(message)) return;

        var dropped = Interlocked.Increment(ref _droppedMessages);
        if (dropped == 1 || dropped % 100 == 0)
            _logger.LogWarning(
                "Redis EventBus 消费缓冲区已满，已丢弃 {DroppedCount} 条消息。Channel: {Channel}",
                dropped,
                message.Channel);
    }

    private async Task ConsumeMessagesAsync(CancellationToken cancellationToken)
    {
        await foreach (var message in _messageQueue.Reader.ReadAllAsync(cancellationToken))
            await HandleMessageAsync(message.Message.ToString(), cancellationToken);
    }

    private IEnumerable<Type> DiscoverEventTypesWithHandlers()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(GetLoadableTypes)
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .SelectMany(type => type.GetInterfaces())
            .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEventHandler<>))
            .Select(type => type.GetGenericArguments()[0])
            .Where(eventType => typeof(ISagaIntegrationEvent).IsAssignableFrom(eventType))
            .Distinct();
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
    }

    private string GetChannelName(Type eventType)
    {
        var attribute = eventType.GetCustomAttributes(typeof(EventSchemeAttribute), true)
            .OfType<EventSchemeAttribute>()
            .FirstOrDefault();
        var name = attribute?.EventName ?? eventType.FullName ?? eventType.Name;
        return string.IsNullOrEmpty(_options.Redis.Prefix) ? name : $"{_options.Redis.Prefix}:{name}";
    }

    private async Task HandleMessageAsync(string? payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payload)) return;

        try
        {
            var eventEto = _handlerSerializer.Deserialize<EventEto>(payload);
            if (eventEto == null) return;

            var eventType = ResolveEventType(eventEto.FullName);
            if (eventType == null)
            {
                _logger.LogWarning("无法识别 Redis 事件类型: {EventType}", eventEto.FullName);
                return;
            }

            var eventData = _handlerSerializer.Deserialize(eventEto.Data, eventType);
            if (eventData == null) return;

            if (eventData is IntegrationEvent integrationEvent)
            {
                var idempotencyPrefix = string.IsNullOrWhiteSpace(_options.Redis.Prefix)
                    ? "eventbus"
                    : _options.Redis.Prefix;
                var idempotencyKey = $"{idempotencyPrefix}:idempotency:{eventEto.FullName}:{integrationEvent.Id}";
                var database = _connection.Multiplexer.GetDatabase();
                var isNew = await database.StringSetAsync(
                        idempotencyKey,
                        "1",
                        TimeSpan.FromSeconds(_options.Redis.IdempotencyExpireSeconds),
                        When.NotExists)
                    .WaitAsync(cancellationToken);

                if (!isNew)
                {
                    _logger.LogInformation("检测到重复消息，已跳过处理: {EventId}, Type: {EventType}",
                        integrationEvent.Id, eventEto.FullName);
                    return;
                }
            }

            await ProcessEventAsync(eventType, eventData, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理 Redis 消息时出错");
        }
    }

    private Type? ResolveEventType(string fullName)
    {
        if (_typeCache.TryGetValue(fullName, out var cachedType)) return cachedType;

        var eventType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(GetLoadableTypes)
            .FirstOrDefault(type => type.FullName == fullName);

        if (eventType != null) _typeCache.TryAdd(fullName, eventType);
        return eventType;
    }

    private async Task ProcessEventAsync(Type eventType, object eventData, CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);
        var handlers = scope.ServiceProvider.GetServices(handlerType).Where(handler => handler != null).ToArray();

        foreach (var handler in handlers)
        {
            try
            {
                await _handlerInvoker.InvokeAsync(handler!, eventType, eventData, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis 事件处理器执行失败: {EventType}, Handler: {HandlerType}",
                    eventType.FullName, handler!.GetType().FullName);
            }
        }
    }

    private async Task UnsubscribeAsync()
    {
        foreach (var subscription in _subscriptions)
        {
            try
            {
                await subscription.UnsubscribeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "取消 Redis 频道订阅时发生错误: {Channel}", subscription.Channel);
            }
        }

        _subscriptions.Clear();
    }
}
