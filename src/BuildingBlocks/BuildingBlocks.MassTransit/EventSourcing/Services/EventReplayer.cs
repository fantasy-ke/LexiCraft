using BuildingBlocks.Extensions.System;
using BuildingBlocks.MassTransit.EventSourcing.Abstractions;
using MassTransit;

namespace BuildingBlocks.MassTransit.EventSourcing.Services;

/// <summary>
///     事件回放服务实现
/// </summary>
public class EventReplayer(IEventStore eventStore, IPublishEndpoint publishEndpoint) : IEventReplayer
{
    public async Task ReplayAsync(string streamId, CancellationToken cancellationToken = default)
    {
        await ReplayAsync(streamId, 0, null, cancellationToken);
    }

    public async Task ReplayAsync(string streamId, long fromVersion, long? toVersion = null, CancellationToken cancellationToken = default)
    {
        // 使用 ReadStoredEventsAsync 获取包含元数据的事件
        var storedEvents = await eventStore.ReadStoredEventsAsync(streamId, fromVersion, toVersion, cancellationToken);

        foreach (var storedEvent in storedEvents)
        {
            var eventType = Type.GetType(storedEvent.EventType);
            if (eventType == null) continue;

            var @event = storedEvent.Data.FromJson(eventType);
            if (@event == null) continue;

            await publishEndpoint.Publish(@event, context =>
            {
                // 标记为回放事件
                context.Headers.Set("MT-Event-Replay", "true");

                // 传递原始事件信息
                context.Headers.Set("MT-Original-MessageId", storedEvent.Id);
                context.Headers.Set("MT-Original-Timestamp", storedEvent.Timestamp);
                context.Headers.Set("MT-Stream-Version", storedEvent.Version);

                if (!string.IsNullOrEmpty(storedEvent.MetaData))
                    context.Headers.Set("MT-Original-MetaData", storedEvent.MetaData);
            }, cancellationToken);
        }
    }
}