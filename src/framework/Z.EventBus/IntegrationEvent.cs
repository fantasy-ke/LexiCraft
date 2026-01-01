using System.Text.Json.Serialization;

namespace Z.EventBus;

/// <summary>
/// 集成事件基类，用于跨服务通信
/// </summary>
[method: JsonConstructor]
public abstract record IntegrationEvent(Guid Id, Guid CorrelationId, DateTime CreationDate) : ISagaIntegrationEvent
{
    protected IntegrationEvent() : this(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow)
    {
    }

    /// <summary>
    /// 事件唯一标识
    /// </summary>
    [JsonPropertyOrder(-3)]
    public Guid Id { get; init; } = Id;

    /// <summary>
    /// 关联 ID，用于追踪整个分布式事务（Saga）的生命周期
    /// </summary>
    [JsonPropertyOrder(-2)]
    public Guid CorrelationId { get; init; } = CorrelationId;

    /// <summary>
    /// 事件创建时间
    /// </summary>
    [JsonPropertyOrder(-1)]
    public DateTime CreationDate { get; init; } = CreationDate;
}
