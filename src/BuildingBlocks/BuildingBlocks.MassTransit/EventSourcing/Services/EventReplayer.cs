using BuildingBlocks.Extensions.System;
using BuildingBlocks.MassTransit.EventSourcing.Abstractions;
using MassTransit;

namespace BuildingBlocks.MassTransit.EventSourcing.Services;

/// <summary>
///     事件回放服务实现
/// </summary>
public sealed class EventReplayer(IEventStore eventStore, IPublishEndpoint publishEndpoint) : IEventReplayer
{
    public Task ReplayAsync(string streamId, CancellationToken cancellationToken = default)
    {
        return ReplayAsync(streamId, 0, null, cancellationToken);
    }

    public async Task ReplayAsync(
        string streamId,
        long fromVersion,
        long? toVersion = null,
        CancellationToken cancellationToken = default)
    {
        await foreach (var storedEvent in eventStore.StreamStoredEventsAsync(
                           streamId, fromVersion, toVersion, cancellationToken))
        {
            var eventType = Type.GetType(storedEvent.EventType);
            if (eventType == null) continue;

            var @event = storedEvent.Data.FromJson(eventType);
            if (@event == null) continue;

            await publishEndpoint.Publish(@event, context =>
            {
                context.Headers.Set("MT-Event-Replay", "true");
                context.Headers.Set("MT-Original-MessageId", storedEvent.Id);
                context.Headers.Set("MT-Original-Timestamp", storedEvent.Timestamp);
                context.Headers.Set("MT-Stream-Version", storedEvent.Version);

                if (!string.IsNullOrEmpty(storedEvent.MetaData))
                    context.Headers.Set("MT-Original-MetaData", storedEvent.MetaData);
            }, cancellationToken);
        }
    }
}
