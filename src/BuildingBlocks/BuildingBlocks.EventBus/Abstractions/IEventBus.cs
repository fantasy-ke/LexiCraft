namespace BuildingBlocks.EventBus.Abstractions;

/// <summary>
/// 事件总线接口
/// </summary>
/// <typeparam name="TEvent"></typeparam>
public interface IEventBus<in TEvent> where TEvent : class
{
    /// <summary>
    /// 智能发布事件 (根据配置决定分发策略)
    /// </summary>
    ValueTask PublishAsync(TEvent @event);

    /// <summary>
    /// 强制使用本地内存通道发布
    /// </summary>
    ValueTask PublishLocalAsync(TEvent @event);

    /// <summary>
    /// 强制使用分布式消息中间件发布
    /// </summary>
    ValueTask PublishDistributedAsync(TEvent @event);
}
