using BuildingBlocks.Domain.Internal;

namespace BuildingBlocks.MassTransit.EventSourcing.Abstractions;

/// <summary>
///     支持事件溯源的聚合根接口
/// </summary>
public interface IEventSourcedAggregate : IAggregateRoot
{
    /// <summary>
    ///     当前版本号
    /// </summary>
    long Version { get; }

    /// <summary>
    ///     获取未提交的事件
    /// </summary>
    IEnumerable<object> GetUncommittedEvents();

    /// <summary>
    ///     清除未提交的事件
    /// </summary>
    void ClearUncommittedEvents();

    /// <summary>
    ///     从历史事件加载状态
    /// </summary>
    /// <param name="history">历史事件集合</param>
    void LoadFromHistory(IEnumerable<object> history);
}

/// <summary>
///     事件溯源聚合根基类
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
/// <typeparam name="TUserKey">审计用户主键类型</typeparam>
public abstract class EventSourcedAggregate<TKey, TUserKey> : AuditAggregateRoot<TKey, TUserKey>, IEventSourcedAggregate
{
    private readonly List<object> _uncommittedEvents = new();

    public long Version { get; private set; } = 0;

    public IEnumerable<object> GetUncommittedEvents()
    {
        return _uncommittedEvents.AsReadOnly();
    }

    public void ClearUncommittedEvents()
    {
        _uncommittedEvents.Clear();
    }

    public void LoadFromHistory(IEnumerable<object> history)
    {
        foreach (var @event in history)
        {
            ApplyEvent(@event);
            Version++;
        }
    }

    protected void AddEvent(object @event)
    {
        ApplyEvent(@event);
        _uncommittedEvents.Add(@event);
        Version++;
    }

    protected abstract void ApplyEvent(object @event);
}

/// <summary>
///     事件溯源聚合根基类 (默认用户主键为 Guid?)
/// </summary>
/// <typeparam name="TKey"></typeparam>
public abstract class EventSourcedAggregate<TKey> : EventSourcedAggregate<TKey, Guid?>;