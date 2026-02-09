namespace BuildingBlocks.MassTransit.EventSourcing.Abstractions;

/// <summary>
/// 事件回放服务接口
/// </summary>
public interface IEventReplayer
{
    /// <summary>
    /// 回放指定流的事件并发布到总线
    /// </summary>
    /// <param name="streamId">事件流ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ReplayAsync(string streamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 回放指定流的事件并发布到总线 (指定起始版本)
    /// </summary>
    /// <param name="streamId">事件流ID</param>
    /// <param name="fromVersion">起始版本号</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ReplayAsync(string streamId, long fromVersion, CancellationToken cancellationToken = default);
}
