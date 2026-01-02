using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Channels;
using BuildingBlocks.EventBus.Abstractions;
using BuildingBlocks.EventBus.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.EventBus.Local;

/// <summary>
/// 本地事件 Client 核心实现
/// </summary>
public class EventLocalClient(
    IServiceProvider serviceProvider,
    ILogger<EventLocalClient> logger,
    IHandlerSerializer handlerSerializer) : IDisposable
{
    private readonly ConcurrentDictionary<string, Channel<string>> _channels = new();
    private readonly ConcurrentDictionary<string, Type?> _types = new();
    private readonly ConcurrentDictionary<Type, Func<object, object, CancellationToken, Task>> _handlerCache = new();
    private readonly CancellationTokenSource _cts = new();

    public async Task PublishAsync(Type eventType, string eventData)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        var channel = CreateOrGetChannels(eventType, out var channelName);
        while (await channel.Writer.WaitToWriteAsync())
        {
            var eventEto = new EventEto(channelName, @eventData);
            var data = handlerSerializer.SerializeJson(eventEto);
            await channel.Writer.WriteAsync(data, _cts.Token);
            break;
        }
    }

    private Channel<string> CreateOrGetChannels(Type eventType, out string channelName)
    {
        var attribute = eventType.GetCustomAttributes().OfType<EventSchemeAttribute>().FirstOrDefault();
        channelName = attribute?.EventName ?? eventType.FullName!;
        var channel = _channels.GetValueOrDefault(channelName!);
        if (channel is not null) return channel;

        channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions()
        {
            SingleReader = attribute?.SingleReader ?? true,
            SingleWriter = attribute?.SingleWriter ?? true,
            AllowSynchronousContinuations = attribute?.AllowSynchronousContinuations ?? true,
        });
        _channels.TryAdd(channelName!, channel);
        return channel;
    }

    public async Task ConsumeStartAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var channel in _channels)
            {
                _ = Task.Factory.StartNew(
                    async () => { await ConsumeChannelAsync(channel.Key, channel.Value, _cts.Token); }, stoppingToken);
            }
            await Task.Delay(5000, stoppingToken);
        }
    }

    private async Task ConsumeChannelAsync(string channelName, Channel<string> channel, CancellationToken cancellationToken)
    {
        while (channel.Reader.TryPeek(out _) && await channel.Reader.WaitToReadAsync(cancellationToken))
        {
            try
            {
                if (!channel.Reader.TryRead(out var message)) continue;
                var eventEto = handlerSerializer.Deserialize<EventEto>(message);
                if (eventEto == null) continue;

                var eventType = _types.GetOrAdd(eventEto.FullName, fullName =>
                {
                    return AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(assembly => assembly.GetTypes())
                        .FirstOrDefault(t => t.FullName == fullName ||
                                             t.GetCustomAttributes().OfType<EventSchemeAttribute>().Any(attr => attr.EventName == fullName));
                });

                if (eventType == null) continue;
                var eventData = handlerSerializer.Deserialize(eventEto.Data, eventType);
                if (eventData == null) continue;

                await ProcessEventAsync(eventType, eventData, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"消费本地Channel {channelName} 时发生错误");
            }
            finally
            {
                if (!channel.Reader.TryPeek(out _)) _channels.TryRemove(channelName, out _);
            }
        }
    }

    private async Task ProcessEventAsync(Type eventType, object eventData, CancellationToken cancellationToken)
    {
        var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);
        await using var scope = serviceProvider.CreateAsyncScope();
        var handlers = scope.ServiceProvider.GetServices(handlerType).ToArray();

        foreach (var handler in handlers)
        {
            try
            {
                var handlerDelegate = _handlerCache.GetOrAdd(eventType, type =>
                {
                    var handlerParam = Expression.Parameter(typeof(object), "handler");
                    var eventParam = Expression.Parameter(typeof(object), "eventData");
                    var tokenParam = Expression.Parameter(typeof(CancellationToken), "token");
                    var handleMethod = handlerType.GetMethod("HandleAsync", [type, typeof(CancellationToken)]);
                    var call = Expression.Call(Expression.Convert(handlerParam, handlerType), handleMethod!, Expression.Convert(eventParam, type), tokenParam);
                    return Expression.Lambda<Func<object, object, CancellationToken, Task>>(call, handlerParam, eventParam, tokenParam).Compile();
                });
                await handlerDelegate(handler!, eventData, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"本地处理程序处理事件 {eventType.FullName} 时发生错误");
            }
        }
    }

    public void Dispose() => _cts.Dispose();
}
