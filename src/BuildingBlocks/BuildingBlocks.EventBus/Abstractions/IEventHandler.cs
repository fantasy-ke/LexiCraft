namespace BuildingBlocks.EventBus.Abstractions;

/// <summary>
///     事件处理程序接口
/// </summary>
/// <typeparam name="TEvent"></typeparam>
public interface IEventHandler<in TEvent> where TEvent : class
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}