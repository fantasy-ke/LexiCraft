using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Z.EventBus;
using Z.Local.EventBus.Serializer;

namespace Z.Local.EventBus;

/// <summary>
/// 本地事件Client
/// </summary>
/// <param name="logger"></param>
/// <param name="serviceProvider"></param>
/// <param name="handlerSerializer"></param>
public class EventLocalClient(
    IServiceProvider serviceProvider,
    ILogger<EventLocalClient> logger,
    IHandlerSerializer handlerSerializer) : IDisposable
{
    private readonly ConcurrentDictionary<string, Channel<string>> _channels = new();
    private readonly ConcurrentDictionary<string, Type?> _types = new();
    private readonly ConcurrentDictionary<Type, Func<object, object, CancellationToken, Task>> _handlerCache = new();
    private readonly CancellationTokenSource _cts = new();

    /// <summary>
    /// 获取所有已注册的Channel
    /// </summary>
    /// <returns>Channel字典，键为事件类型全名，值为Channel实例</returns>
    public IReadOnlyDictionary<string, Channel<string>> GetAllChannels() => _channels;


    /// <summary>
    /// publish事件指定的Dto Handler
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="eventData"></param>
    public async Task PublishAsync(Type eventType, string eventData)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        // 分布式事件总线发布事件，
        // 且将TEvent的FullName作为事件类型包装，消费者会解析这个类型，然后反序列化为TEvent
        var channel = CreateOrGetChannels(eventType, out var channelName);
        while (await channel.Writer.WaitToWriteAsync())
        {
            var eventDto = new EventEto(channelName, @eventData);

            var data = handlerSerializer.SerializeJson(eventDto);

            await channel.Writer.WriteAsync(data, _cts.Token);
            
            break;
        }
    }

    /// <summary>
    /// 创建channels
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="channelName"></param>
    /// <returns></returns>
    private Channel<string> CreateOrGetChannels(Type eventType, out string channelName)
    {
        var attribute = eventType
            .GetCustomAttributes()
            .OfType<EventSchemeAttribute>()
            .FirstOrDefault();

        channelName = GetChannelName(attribute) ?? eventType.FullName!;

        var channel = _channels.GetValueOrDefault(channelName!);

        if (channel is not null)
        {
            return channel;
        }

        channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions()
        {
            SingleReader = attribute?.SingleReader ?? true,
            SingleWriter = attribute?.SingleWriter ?? true,
            AllowSynchronousContinuations = attribute?.AllowSynchronousContinuations ?? true,
        });
        _channels.TryAdd(channelName!, channel);

        logger.LogInformation($"创建channels：{channelName}");
        return channel;
    }

    public async Task ConsumeStartAsync(CancellationToken stoppingToken)
    {
        
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var channel in _channels)
            {
                _ = Task.Factory.StartNew(async () =>
                {
                    await ConsumeChannelAsync(channel.Key, channel.Value, _cts.Token);
                }, stoppingToken);
            }
            //周期性任务，于上次任务执行完成后，等待5秒，执行下一次任务
            await Task.Delay(5000, stoppingToken);
        }
    }


    /// <summary>
    /// 消费指定Channel的消息
    /// </summary>
    /// <param name="channelName">Channel名称（事件类型全名）</param>
    /// <param name="channel">Channel实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    private async Task ConsumeChannelAsync(string channelName, Channel<string> channel,
        CancellationToken cancellationToken)
    {
        logger.LogInformation($"开始消费Channel: {channelName}");

        try
        {
            while (channel.Reader.TryPeek(out _) && await channel.Reader.WaitToReadAsync(cancellationToken))
            {
                if (!channel.Reader.TryRead(out var message)) continue;

                var eventEto = handlerSerializer.Deserialize<EventEto>(message);
                if (eventEto == null)
                {
                    logger.LogWarning($"无法反序列化事件数据: {message}");
                    continue;
                }

                var eventType = _types.GetOrAdd(eventEto.FullName, fullName =>
                {
                    var typeName = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(assembly => assembly.GetTypes())
                        .FirstOrDefault(t => t.FullName == fullName ||
                                             t.GetCustomAttributes()
                                                 .OfType<EventSchemeAttribute>()
                                                 .Any(attr => attr.EventName == fullName));
                    return typeName;
                });

                if (eventType == null)
                {
                    logger.LogWarning($"无法找到事件类型: {eventEto.FullName}");
                    continue;
                }

                var eventData = handlerSerializer.Deserialize(eventEto.Data, eventType);
                if (eventData == null)
                {
                    logger.LogWarning($"无法反序列化事件数据为类型 {eventType.FullName}");
                    continue;
                }

                await ProcessEventAsync(eventType, eventData, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 正常取消，不需要特殊处理
            logger.LogInformation($"Channel {channelName} 的消费任务已取消");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"消费Channel {channelName} 时发生错误");
        }
        finally
        {
            // 如果 Channel 已经为空，清理它
            if (!channel.Reader.TryPeek(out _))
            {
                _channels.TryRemove(channelName, out _);
                logger.LogInformation($"Channel {channelName} 已被清理");
            }
        }

        logger.LogInformation($"停止消费Channel: {channelName}");
    }

    /// <summary>
    /// 处理事件
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="eventData">事件数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    private async Task ProcessEventAsync(Type eventType, object eventData, CancellationToken cancellationToken)
    {
        // 构造泛型处理程序类型 IEventHandler<T>
        var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);

        // 从服务容器中获取所有该类型的处理程序实例
        await using var scope = serviceProvider.CreateAsyncScope();
        var handlers = scope.ServiceProvider.GetServices(handlerType).ToArray();

        if (handlers.Length == 0)
        {
            logger.LogWarning($"没有找到事件 {eventType.FullName} 的处理程序");
            return;
        }

        logger.LogInformation($"找到 {handlers.Length} 个处理程序用于事件 {eventType.FullName}");

        // 调用每个处理程序的HandleAsync方法
        foreach (var handler in handlers)
        {
            try
            {
                // 通过表达式树来获取HandleAsync方法的委托
                var handlerDelegate = _handlerCache.GetOrAdd(eventType, type =>
                {
                    // 创建委托参数
                    var handlerParam = Expression.Parameter(typeof(object), "handler");
                    var eventParam = Expression.Parameter(typeof(object), "eventData");
                    var tokenParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

                    // 构建方法调用表达式
                    var handleMethod = handlerType.GetMethod("HandleAsync", [type, typeof(CancellationToken)]);
                    if (handleMethod == null)
                    {
                        throw new InvalidOperationException($"处理程序类型 {handlerType.FullName} 没有正确的HandleAsync方法");
                    }

                    // 转换参数类型
                    var convertedHandler = Expression.Convert(handlerParam, handlerType);
                    var convertedEvent = Expression.Convert(eventParam, type);

                    // 构建方法调用
                    var methodCall = Expression.Call(
                        convertedHandler,
                        handleMethod,
                        convertedEvent,
                        tokenParam
                    );

                    // 创建并编译表达式树
                    var lambda = Expression.Lambda<Func<object, object, CancellationToken, Task>>(
                        methodCall,
                        handlerParam,
                        eventParam,
                        tokenParam
                    );

                    return lambda.Compile();
                });

                // 使用编译后的委托调用处理方法
                await handlerDelegate(handler!, eventData, cancellationToken);

                logger.LogInformation($"处理程序 {handler!.GetType().FullName} 成功处理事件 {eventType.FullName}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"处理程序 {handler!.GetType().FullName} 处理事件 {eventType.FullName} 时发生错误");
            }
        }
    }

    /// <summary>
    /// 获取channelName
    /// </summary>
    /// <param name="attribute"></param>
    /// <returns></returns>
    private static string? GetChannelName(EventSchemeAttribute? attribute)
    {
        return string.IsNullOrWhiteSpace(attribute?.EventName) ? null : attribute.EventName;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}