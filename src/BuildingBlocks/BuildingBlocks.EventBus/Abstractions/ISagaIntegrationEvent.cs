namespace BuildingBlocks.EventBus.Abstractions;

/// <summary>
/// Saga 集成事件接口，强制包含 CorrelationId 用于追踪
/// </summary>
public interface ISagaIntegrationEvent
{
    Guid CorrelationId { get; }
}
