namespace Z.EventBus;

/// <summary>
/// 定义 Saga 集成事件接口
/// </summary>
public interface ISagaIntegrationEvent
{
    /// <summary>
    /// 关联 ID，用于在 Saga 流程中标志同一个事务流程
    /// </summary>
    Guid CorrelationId { get; }
}
