namespace BuildingBlocks.MassTransit.EventSourcing.Abstractions;

/// <summary>
///     事件存储接口
/// </summary>
public interface IEventStore
{
    /// <summary>
    ///     将事件保存到存储中
    /// </summary>
    /// <param name="streamId">事件流ID (通常是聚合根ID)</param>
    /// <param name="events">要保存的事件集合</param>
    /// <param name="expectedVersion">期望的版本号 (用于乐观并发控制)</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task AppendEventsAsync(string streamId, IEnumerable<object> events, long? expectedVersion = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     读取原始存储事件 (包含元数据)
    /// </summary>
    Task<IEnumerable<StoredEvent>> ReadStoredEventsAsync(string streamId, long fromVersion = 0, long? toVersion = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     读取事件流
    /// </summary>
    /// <param name="streamId">事件流ID</param>
    /// <param name="fromVersion">起始版本号</param>
    /// <param name="toVersion">截止版本号 (可选)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>事件集合</returns>
    Task<IEnumerable<object>> ReadEventsAsync(string streamId, long fromVersion = 0, long? toVersion = null,
        CancellationToken cancellationToken = default);
}