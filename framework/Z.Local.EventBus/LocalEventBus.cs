using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Z.EventBus;
using Z.Local.EventBus.Exceptions;
using Z.Local.EventBus.Serializer;

namespace Z.Local.EventBus;

public class LocalEventBus<TEvent>(EventLocalClient eventLocalClient,
    ILogger<LocalEventBus<TEvent>> logger,IHandlerSerializer handlerSerializer)
    :IEventBus<TEvent> where TEvent : class
{
    
    /// <summary>
    /// publish事件指定的Dto Handler
    /// </summary>
    /// <param name="event"></param>
    public async ValueTask PublishAsync(TEvent @event)
    {
        // 分布式事件总线发布事件，且将TEvent的FullName作为事件类型包装，消费者会解析这个类型，然后反序列化为TEvent
        ArgumentNullException.ThrowIfNull(@event);

        try
        {
            logger.LogInformation("publish事件指定的Dto Handler");
            await eventLocalClient.PublishAsync(@event.GetType(), handlerSerializer.SerializeJson(@event));
        }
        catch (Exception e)
        {
            ThrowEventClientException.ThrowException(e.Message);
        }
        
    }
    
}