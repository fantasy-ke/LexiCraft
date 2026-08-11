using BuildingBlocks.MassTransit.LocalEvents;
using BuildingBlocks.Mediator;
using MassTransit;

namespace BuildingBlocks.MassTransit.Services;

/// <summary>
///     统一事件发布者接口，支持集成事件和本地事件
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class;

    Task PublishLocalAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IDomainEvent;
}

/// <summary>
///     统一事件发布者实现
/// </summary>
public sealed class EventPublisher(IPublishEndpoint publishEndpoint, ILocalEventBus localEventBus) : IEventPublisher
{
    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(@event);
        return publishEndpoint.Publish(@event, cancellationToken);
    }

    public Task PublishLocalAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(@event);
        return localEventBus.PublishAsync(@event, cancellationToken).AsTask();
    }
}
