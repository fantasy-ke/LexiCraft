using BuildingBlocks.Extensions.System;
using BuildingBlocks.MassTransit.EventSourcing.Abstractions;
using BuildingBlocks.Mediator;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.MassTransit.EventSourcing.Services;

/// <summary>
///     本地领域事件回放服务实现
/// </summary>
public sealed class DomainEventReplayer(
    IEventStore eventStore,
    IPublisher publisher,
    ILogger<DomainEventReplayer> logger) : IDomainEventReplayer
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
            if (eventType == null)
            {
                logger.LogWarning("无法识别的事件类型: {EventType}", storedEvent.EventType);
                continue;
            }

            if (!typeof(IDomainEvent).IsAssignableFrom(eventType))
            {
                logger.LogDebug("跳过非领域事件: {EventType}", eventType.Name);
                continue;
            }

            var @event = storedEvent.Data.FromJson(eventType);
            if (@event == null) continue;

            logger.LogInformation("回放领域事件: {EventType}, Stream: {StreamId}, Version: {Version}",
                eventType.Name, streamId, storedEvent.Version);
            await publisher.Publish(@event, cancellationToken);
        }
    }
}
