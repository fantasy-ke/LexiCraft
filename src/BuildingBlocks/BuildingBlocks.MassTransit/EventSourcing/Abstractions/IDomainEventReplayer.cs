namespace BuildingBlocks.MassTransit.EventSourcing.Abstractions;

/// <summary>
///     本地领域事件回放接口
/// </summary>
public interface IDomainEventReplayer
{
    /// <summary>
    ///     回放指定流的事件并发布到 MediatR (本地处理)
    /// </summary>
    /// <param name="streamId">事件流ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ReplayAsync(string streamId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     回放指定流的事件并发布到 MediatR (本地处理，指定版本范围)
    /// </summary>
    /// <param name="streamId">事件流ID</param>
    /// <param name="fromVersion">起始版本号</param>
    /// <param name="toVersion">截止版本号 (可选)</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ReplayAsync(string streamId, long fromVersion, long? toVersion = null, CancellationToken cancellationToken = default);
}
