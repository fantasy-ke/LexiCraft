namespace BuildingBlocks.MassTransit.EventSourcing.Abstractions;

/// <summary>
///     标记一个事件需要被自动记录到事件存储中
/// </summary>
public interface IEventSourced
{
    /// <summary>
    ///     获取事件流ID (通常是聚合根ID)
    /// </summary>
    string GetStreamId();

    /// <summary>
    ///     获取事件元数据 (可选)
    /// </summary>
    IDictionary<string, object>? GetMetaData()
    {
        return null;
    }
}