using System.Text.Json.Serialization;

namespace BuildingBlocks.MassTransit.EventSourcing.Abstractions;

/// <summary>
/// 事件存储实体，用于持久化到 Redis
/// </summary>
public class StoredEvent
{
    /// <summary>
    /// 事件唯一标识
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 聚合根ID / 流ID
    /// </summary>
    public string StreamId { get; set; } =  string.Empty;

    /// <summary>
    /// 事件类型 (AssemblyQualifiedName)
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// 事件数据 (JSON)
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// 发生时间
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 元数据 (JSON)
    /// </summary>
    public string? MetaData { get; set; }

    /// <summary>
    /// 版本号 (在流中的位置)
    /// </summary>
    public long Version { get; set; }

    /// <summary>
    /// 无参构造函数 (反序列化用)
    /// </summary>
    public StoredEvent() { }

    public StoredEvent(Guid id, string streamId, string eventType, string data, DateTime timestamp, long version, string? metaData = null)
    {
        Id = id;
        StreamId = streamId;
        EventType = eventType;
        Data = data;
        Timestamp = timestamp;
        Version = version;
        MetaData = metaData;
    }
}
