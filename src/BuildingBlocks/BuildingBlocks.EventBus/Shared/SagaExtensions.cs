using BuildingBlocks.EventBus.Abstractions;

namespace BuildingBlocks.EventBus.Shared;

public static class SagaExtensions
{
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
