namespace BuildingBlocks.EventBus.Abstractions;

/// <summary>
/// 事件总线接口
/// </summary>
/// <typeparam name="TEvent"></typeparam>
public interface IEventBus<in TEvent> where TEvent : class
{
    ValueTask PublishAsync(TEvent @event);
}
