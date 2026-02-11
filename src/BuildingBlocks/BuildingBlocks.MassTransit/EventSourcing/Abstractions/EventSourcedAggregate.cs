namespace BuildingBlocks.MassTransit.EventSourcing.Abstractions;

/// <summary>
///     支持事件溯源的聚合根接口
/// </summary>
public interface IEventSourcedAggregate
{
    /// <summary>
    ///     聚合根ID
    /// </summary>
    Guid Id { get; }

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
public abstract class EventSourcedAggregate : IEventSourcedAggregate
{
    private readonly List<object> _uncommittedEvents = new();

    public Guid Id { get; protected set; }
    public long Version { get; private set; } = -1;

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
    }

    protected abstract void ApplyEvent(object @event);
}