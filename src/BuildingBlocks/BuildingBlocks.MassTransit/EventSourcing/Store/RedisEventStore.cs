using System.Globalization;
using System.Runtime.CompilerServices;
using BuildingBlocks.Extensions.System;
using BuildingBlocks.MassTransit.Abstractions;
using BuildingBlocks.MassTransit.EventSourcing.Abstractions;
using BuildingBlocks.MassTransit.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BuildingBlocks.MassTransit.EventSourcing.Store;

/// <summary>
///     基于 Redis Stream 的事件存储实现
/// </summary>
public sealed class RedisEventStore : IEventStore
{
    private readonly int _readBatchSize;
    private readonly EventStoreRedisConnection _redisConnection;
    private readonly string _streamPrefix;

    public RedisEventStore(EventStoreRedisConnection redisConnection, IOptions<MassTransitOptions> options)
    {
        _redisConnection = redisConnection;
        var eventSourcingOptions = options.Value.EventSourcing;
        _streamPrefix = eventSourcingOptions.StreamPrefix;
        _readBatchSize = eventSourcingOptions.ReadBatchSize;
    }

    public async Task AppendEventsAsync(
        string streamId,
        IEnumerable<object> events,
        long? expectedVersion = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentNullException.ThrowIfNull(events);

        var database = _redisConnection.Multiplexer.GetDatabase();
        var key = $"{_streamPrefix}{streamId}";
        var currentVersion = await database.StreamLengthAsync(key).WaitAsync(cancellationToken);

        if (expectedVersion.HasValue && expectedVersion.Value != currentVersion)
            throw new InvalidOperationException(
                $"Concurrency conflict. Expected version: {expectedVersion}, Actual version: {currentVersion}");

        foreach (var @event in events)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(@event);

            currentVersion++;
            var storedEvent = CreateStoredEvent(streamId, currentVersion, @event);
            var entries = ToEntries(storedEvent);

            await database
                .StreamAddAsync(key, entries)
                .WaitAsync(cancellationToken);
        }
    }

    public async IAsyncEnumerable<StoredEvent> StreamStoredEventsAsync(
        string streamId,
        long fromVersion = 0,
        long? toVersion = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        if (fromVersion < 0) throw new ArgumentOutOfRangeException(nameof(fromVersion));
        if (toVersion.HasValue && toVersion.Value < fromVersion)
            throw new ArgumentOutOfRangeException(nameof(toVersion));

        var database = _redisConnection.Multiplexer.GetDatabase();
        var key = $"{_streamPrefix}{streamId}";
        RedisValue minimumId = "-";

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var streamEntries = await database
                .StreamRangeAsync(key, minimumId, "+", _readBatchSize, Order.Ascending)
                .WaitAsync(cancellationToken);

            if (streamEntries.Length == 0) yield break;

            foreach (var entry in streamEntries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var storedEvent = ParseStoredEvent(entry);

                if (storedEvent.Version < fromVersion) continue;
                if (toVersion.HasValue && storedEvent.Version > toVersion.Value) yield break;

                yield return storedEvent;
            }

            if (streamEntries.Length < _readBatchSize) yield break;
            minimumId = $"({streamEntries[^1].Id}";
        }
    }

    public async Task<IEnumerable<StoredEvent>> ReadStoredEventsAsync(
        string streamId,
        long fromVersion = 0,
        long? toVersion = null,
        CancellationToken cancellationToken = default)
    {
        var events = new List<StoredEvent>();
        await foreach (var storedEvent in StreamStoredEventsAsync(
                           streamId, fromVersion, toVersion, cancellationToken))
            events.Add(storedEvent);

        return events;
    }

    public async Task<IEnumerable<object>> ReadEventsAsync(
        string streamId,
        long fromVersion = 0,
        long? toVersion = null,
        CancellationToken cancellationToken = default)
    {
        var events = new List<object>();

        await foreach (var storedEvent in StreamStoredEventsAsync(
                           streamId, fromVersion, toVersion, cancellationToken))
        {
            var eventType = Type.GetType(storedEvent.EventType);
            if (eventType == null) continue;

            var @event = storedEvent.Data.FromJson(eventType);
            if (@event != null) events.Add(@event);
        }

        return events;
    }

    private static StoredEvent CreateStoredEvent(string streamId, long version, object @event)
    {
        return new StoredEvent(
            GetEventId(@event),
            streamId,
            @event.GetType().AssemblyQualifiedName ?? @event.GetType().FullName!,
            @event.ToJson(),
            DateTime.UtcNow,
            version,
            GetMetaData(@event));
    }

    private static NameValueEntry[] ToEntries(StoredEvent storedEvent)
    {
        return
        [
            new(nameof(StoredEvent.Id), storedEvent.Id.ToString()),
            new(nameof(StoredEvent.StreamId), storedEvent.StreamId),
            new(nameof(StoredEvent.EventType), storedEvent.EventType),
            new(nameof(StoredEvent.Data), storedEvent.Data),
            new(nameof(StoredEvent.Timestamp), storedEvent.Timestamp.ToString("O")),
            new(nameof(StoredEvent.Version), storedEvent.Version),
            new(nameof(StoredEvent.MetaData), storedEvent.MetaData ?? string.Empty)
        ];
    }

    private static StoredEvent ParseStoredEvent(StreamEntry entry)
    {
        var values = entry.Values
            .Where(value => !value.Name.IsNull && !value.Value.IsNull)
            .ToDictionary(value => value.Name.ToString(), value => value.Value.ToString());

        var id = values.TryGetValue(nameof(StoredEvent.Id), out var cacheId) && Guid.TryParse(cacheId, out var eventId)
            ? eventId
            : Guid.Empty;
        var streamId = values.GetValueOrDefault(nameof(StoredEvent.StreamId), string.Empty);
        var eventType = values.GetValueOrDefault(nameof(StoredEvent.EventType), string.Empty);
        var data = values.GetValueOrDefault(nameof(StoredEvent.Data), string.Empty);
        var timestamp = values.TryGetValue(nameof(StoredEvent.Timestamp), out var cacheTimestamp)
                        && DateTime.TryParse(cacheTimestamp, CultureInfo.InvariantCulture,
                            DateTimeStyles.RoundtripKind, out var parsedTimestamp)
            ? parsedTimestamp
            : DateTime.MinValue;
        var version = values.TryGetValue(nameof(StoredEvent.Version), out var cacheVersion)
                      && long.TryParse(cacheVersion, NumberStyles.Integer, CultureInfo.InvariantCulture,
                          out var parsedVersion)
            ? parsedVersion
            : 0;
        var metaData = values.GetValueOrDefault(nameof(StoredEvent.MetaData));

        return new StoredEvent(id, streamId, eventType, data, timestamp, version,
            string.IsNullOrEmpty(metaData) ? null : metaData);
    }

    private static Guid GetEventId(object @event)
    {
        if (@event is IIntegrationEvent integrationEvent) return integrationEvent.Id;

        var property = @event.GetType().GetProperty("Id");
        if (property?.PropertyType == typeof(Guid) && property.GetValue(@event) is Guid id) return id;

        return Guid.NewGuid();
    }

    private static string? GetMetaData(object @event)
    {
        if (@event is IEventSourced eventSourced) return eventSourced.GetMetaData()?.ToJson();

        var property = @event.GetType().GetProperty("MetaData");
        var value = property?.GetValue(@event);
        return value switch
        {
            null => null,
            IDictionary<string, object> dictionary => dictionary.ToJson(),
            _ => value.ToJson()
        };
    }
}
