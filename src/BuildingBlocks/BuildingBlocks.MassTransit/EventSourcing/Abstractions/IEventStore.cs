using System.Runtime.CompilerServices;

namespace BuildingBlocks.MassTransit.EventSourcing.Abstractions;

/// <summary>
///     事件存储接口
/// </summary>
public interface IEventStore
{
    /// <summary>
    ///     将事件保存到存储中
    /// </summary>
    Task AppendEventsAsync(
        string streamId,
        IEnumerable<object> events,
        long? expectedVersion = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     分页流式读取原始存储事件，适合事件回放等长事件流场景。
    ///     自定义实现未覆盖此方法时，会回退到 <see cref="ReadStoredEventsAsync" />。
    /// </summary>
    async IAsyncEnumerable<StoredEvent> StreamStoredEventsAsync(
        string streamId,
        long fromVersion = 0,
        long? toVersion = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var events = await ReadStoredEventsAsync(streamId, fromVersion, toVersion, cancellationToken);
        foreach (var storedEvent in events)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return storedEvent;
        }
    }

    /// <summary>
    ///     读取原始存储事件并物化为集合。长事件流优先使用 <see cref="StreamStoredEventsAsync" />。
    /// </summary>
    Task<IEnumerable<StoredEvent>> ReadStoredEventsAsync(
        string streamId,
        long fromVersion = 0,
        long? toVersion = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     读取并反序列化事件流
    /// </summary>
    Task<IEnumerable<object>> ReadEventsAsync(
        string streamId,
        long fromVersion = 0,
        long? toVersion = null,
        CancellationToken cancellationToken = default);
}
