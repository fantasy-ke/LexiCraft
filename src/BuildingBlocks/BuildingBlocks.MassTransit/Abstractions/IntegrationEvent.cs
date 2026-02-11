using BuildingBlocks.Mediator;

namespace BuildingBlocks.MassTransit.Abstractions;

/// <summary>
///     集成事件接口，同时也支持本地事件 (MediatR INotification)
/// </summary>
public interface IIntegrationEvent : IDomainEvent
{
    /// <summary>
    ///     事件唯一标识
    /// </summary>
    Guid Id { get; }

    /// <summary>
    ///     事件创建时间
    /// </summary>
    DateTime CreationDate { get; }
}

/// <summary>
///     集成事件基类
/// </summary>
public abstract record IntegrationEvent(Guid Id, DateTime CreationDate) : IIntegrationEvent
{
    protected IntegrationEvent() : this(Guid.NewGuid(), DateTime.UtcNow)
    {
    }
}