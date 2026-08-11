namespace BuildingBlocks.EventBus.Abstractions;

/// <summary>
///     事件总线接口
/// </summary>
/// <typeparam name="TEvent">事件类型</typeparam>
public interface IEventBus<in TEvent> where TEvent : class
{
    /// <summary>
    ///     智能发布事件（根据配置决定分发策略）
    /// </summary>
    ValueTask PublishAsync(TEvent @event);

    ValueTask PublishAsync(TEvent @event, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return PublishAsync(@event);
    }

    /// <summary>
    ///     强制使用本地内存通道发布
    /// </summary>
    ValueTask PublishLocalAsync(TEvent @event);

    ValueTask PublishLocalAsync(TEvent @event, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return PublishLocalAsync(@event);
    }

    /// <summary>
    ///     强制使用分布式消息中间件发布
    /// </summary>
    ValueTask PublishDistributedAsync(TEvent @event);

    ValueTask PublishDistributedAsync(TEvent @event, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return PublishDistributedAsync(@event);
    }
}
