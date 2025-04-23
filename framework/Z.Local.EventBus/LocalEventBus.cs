using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Z.EventBus;

namespace Z.Local.EventBus;

public class LocalEventBus<TEvent>(IHandlerSerializer handlerSerializer,
    ILogger<LocalEventBus<TEvent>> logger)
    :IEventBus<TEvent>, IDisposable where TEvent : class
{
    
    private readonly ConcurrentDictionary<string,
            Channel<string>> _channels = new();
    private readonly CancellationTokenSource _cts = new();
    
    /// <summary>
    /// publish事件指定的Dto Handler
    /// </summary>
    /// <param name="event"></param>
    public async ValueTask PublishAsync(TEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        
        // 分布式事件总线发布事件，
        // 且将TEvent的FullName作为事件类型包装，消费者会解析这个类型，然后反序列化为TEvent
        var channel = CreateChannels(@event.GetType(), out var channelName);
        while (await channel.Writer.WaitToWriteAsync())
        {
            var eventDto = new EventEto(channelName, handlerSerializer.SerializeJson(@event));
            
            var data = handlerSerializer.SerializeJson(eventDto);
            
            await channel.Writer.WriteAsync(data, _cts.Token);
        }
    }

    /// <summary>
    /// 创建channels
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="channelName"></param>
    /// <returns></returns>
    private Channel<string> CreateChannels(Type eventType, out string channelName)
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
            SingleWriter =  attribute?.SingleWriter ?? true,
            AllowSynchronousContinuations =  attribute?.AllowSynchronousContinuations ?? true,
        });
        _channels.TryAdd(channelName!, channel);

        logger.LogInformation($"创建channels：{channelName}");
        return channel;
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