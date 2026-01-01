namespace Z.EventBus;

/// <summary>
/// Saga 模式扩展方法，用于简化跨服务业务流的延续
/// </summary>
public static class SagaExtensions
{
    /// <summary>
    /// 从当前 Saga 事件创建一个后续事件，并自动关联 CorrelationId
    /// </summary>
    /// <typeparam name="TNextEvent">后续事件类型</typeparam>
    /// <param name="currentEvent">当前处理的 Saga 事件</param>
    /// <param name="factory">后续事件的逻辑构建工厂</param>
    /// <returns>关联了相同 CorrelationId 的新事件</returns>
    public static TNextEvent CreateNextEvent<TNextEvent>(
        this ISagaIntegrationEvent currentEvent, 
        Func<Guid, TNextEvent> factory) 
        where TNextEvent : ISagaIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(currentEvent);
        ArgumentNullException.ThrowIfNull(factory);

        return factory(currentEvent.CorrelationId);
    }
}
