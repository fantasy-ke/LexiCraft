using MediatR;

namespace BuildingBlocks.Mediator;

/// <summary>
/// 领域事件处理器接口
/// </summary>
/// <typeparam name="TDomainEvent">领域事件类型</typeparam>
public interface IDomainEventHandler<in TDomainEvent> : INotificationHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent
{
}