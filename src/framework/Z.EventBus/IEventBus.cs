using System;
using System.Threading.Tasks;

namespace Z.EventBus;

/// <summary>
/// EventBus 接口
/// </summary>
/// <typeparam name="TEvent"></typeparam>
public interface IEventBus<in TEvent> where TEvent : class
{
    ValueTask PublishAsync(TEvent @event);
}