using MediatR;

namespace BuildingBlocks.Mediator;

/// <summary>
///     用于标记这是一个领域事件
/// </summary>
public interface IDomainEvent : INotification
{
}