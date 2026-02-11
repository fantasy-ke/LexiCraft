using BuildingBlocks.MassTransit.Abstractions;
using BuildingBlocks.MassTransit.LocalEvents;
using BuildingBlocks.Mediator;
using MassTransit;
using MediatR;

namespace BuildingBlocks.MassTransit.Services;

/// <summary>
/// 统一事件发布者接口，支持集成事件和本地事件
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// 发布集成事件 (到消息队列)
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="event">事件实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 发布本地事件 (进程内，异步后台处理)
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="event">事件实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task PublishLocalAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IDomainEvent;
}

/// <summary>
/// 统一事件发布者实现
/// </summary>
public class EventPublisher(IPublishEndpoint publishEndpoint, ILocalEventBus localEventBus) : IEventPublisher
{
    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        await publishEndpoint.Publish(@event, cancellationToken);
    }

    public async Task PublishLocalAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IDomainEvent
    {
        await localEventBus.PublishAsync(@event, cancellationToken);
    }
}
