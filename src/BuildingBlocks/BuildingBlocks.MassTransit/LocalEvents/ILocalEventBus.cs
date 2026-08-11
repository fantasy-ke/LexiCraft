using BuildingBlocks.Mediator;

namespace BuildingBlocks.MassTransit.LocalEvents;

/// <summary>
///     本地事件内部总线，用于在后台任务中异步处理领域事件
/// </summary>
public interface ILocalEventBus
{
    /// <summary>
    ///     发布事件到内部有界通道；队列满时异步等待。
    /// </summary>
    ValueTask PublishAsync(IDomainEvent @event, CancellationToken cancellationToken = default);

    /// <summary>
    ///     从通道中获取事件流
    /// </summary>
    IAsyncEnumerable<IDomainEvent> DequeueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     完成写入并释放等待中的发布方。自定义实现可按自身资源模型覆盖。
    /// </summary>
    void Complete()
    {
    }
}
