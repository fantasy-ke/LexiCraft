using System.Globalization;
using BuildingBlocks.Extensions.System;
using BuildingBlocks.MassTransit.Abstractions;
using BuildingBlocks.MassTransit.EventSourcing.Abstractions;
using BuildingBlocks.MassTransit.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BuildingBlocks.MassTransit.EventSourcing.Store;

/// <summary>
///     基于 Redis 的事件存储实现
/// </summary>
public class RedisEventStore(IConnectionMultiplexer redis, IOptionsMonitor<MassTransitOptions> options)
    : IEventStore
{
    private readonly string _streamPrefix = options.CurrentValue.EventSourcing.StreamPrefix;

    public async Task AppendEventsAsync(string streamId, IEnumerable<object> events, long? expectedVersion = null,
        CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var key = $"{_streamPrefix}{streamId}";

        // 获取当前流的长度作为版本号基准
        long currentVersion = 0;
        if (await db.KeyExistsAsync(key))
            try
            {
                var info = await db.StreamInfoAsync(key);
                currentVersion = info.Length;
            }
            catch (RedisServerException)
            {
                // 如果 Key 存在但不是 Stream 类型，或者其他异常，视为 0 或抛出
                // 这里简单处理：如果获取失败，假设为 0 (针对 no such key 虽然 KeyExists 为 false，但为了保险)
                currentVersion = 0;
            }

        // 简单的乐观并发控制
        if (expectedVersion.HasValue && expectedVersion.Value != currentVersion)
            // 实际生产中应抛出并发异常，这里仅记录或抛出通用异常
            throw new InvalidOperationException(
                $"Concurrency conflict. Expected version: {expectedVersion}, Actual version: {currentVersion}");

        foreach (var @event in events)
        {
            currentVersion++;
            var eventId = GetEventId(@event);
            var eventData = @event.ToJson();
            var eventType = @event.GetType().AssemblyQualifiedName ?? @event.GetType().FullName!;
            var timestamp = DateTime.UtcNow;
            var metaData = GetMetaData(@event);

            // 创建存储实体
            var storedEvent = new StoredEvent(
                eventId,
                streamId,
                eventType,
                eventData,
                timestamp,
                currentVersion,
                metaData
            );

            // 转换为 Redis Stream Entry
            var entries = new NameValueEntry[]
            {
                new(nameof(StoredEvent.Id), storedEvent.Id.ToString()),
                new(nameof(StoredEvent.StreamId), storedEvent.StreamId),
                new(nameof(StoredEvent.EventType), storedEvent.EventType),
                new(nameof(StoredEvent.Data), storedEvent.Data),
                new(nameof(StoredEvent.Timestamp), storedEvent.Timestamp.ToString("O")),
                new(nameof(StoredEvent.Version), storedEvent.Version),
                new(nameof(StoredEvent.MetaData), storedEvent.MetaData ?? string.Empty)
            };

            await db.StreamAddAsync(key, entries);
        }
    }

    public async Task<IEnumerable<StoredEvent>> ReadStoredEventsAsync(string streamId, long fromVersion = 0,
        long? toVersion = null,
        CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var key = $"{_streamPrefix}{streamId}";

        // 简单读取所有，实际应根据 fromVersion 过滤 (Redis Stream 支持 Range 查询，但 ID 是时间戳)
        // 如果要精确按 Version 查询，可能需要自行索引或遍历
        // 这里为了演示，使用 Range - + 读取所有
        var streamEntries = await db.StreamRangeAsync(key, "-", "+");

        var events = new List<StoredEvent>();

        foreach (var entry in streamEntries)
        {
            var storedEvent = ParseStoredEvent(entry);

            if (storedEvent.Version < fromVersion) continue;
            if (toVersion.HasValue && storedEvent.Version > toVersion.Value) continue;

            events.Add(storedEvent);
        }

        return events;
    }

    public async Task<IEnumerable<object>> ReadEventsAsync(string streamId, long fromVersion = 0,
        long? toVersion = null,
        CancellationToken cancellationToken = default)
    {
        var storedEvents = await ReadStoredEventsAsync(streamId, fromVersion, toVersion, cancellationToken);
        var events = new List<object>();

        foreach (var storedEvent in storedEvents)
        {
            var eventType = Type.GetType(storedEvent.EventType);
            if (eventType != null)
            {
                var @event = storedEvent.Data.FromJson(eventType);
                if (@event != null) events.Add(@event);
            }
        }

        return events;
    }

    private StoredEvent ParseStoredEvent(StreamEntry entry)
    {
        var dict = new Dictionary<string, string>();
        foreach (var val in entry.Values)
            if (!val.Name.IsNull && !val.Value.IsNull)
                dict[val.Name.ToString()] = val.Value.ToString();

        var id = dict.TryGetValue(nameof(StoredEvent.Id), out var cacheId) ? Guid.Parse(cacheId) : Guid.Empty;
        var streamId = dict.TryGetValue(nameof(StoredEvent.StreamId), out var cacheStreamId)
            ? cacheStreamId
            : string.Empty;
        var eventType = dict.TryGetValue(nameof(StoredEvent.EventType), out var cacheEventType)
            ? cacheEventType
            : string.Empty;
        var data = dict.TryGetValue(nameof(StoredEvent.Data), out var cacheData) ? cacheData : string.Empty;
        var timestamp = dict.TryGetValue(nameof(StoredEvent.Timestamp), out var cacheTimestamp)
            ? DateTime.Parse(cacheTimestamp, CultureInfo.InvariantCulture)
            : DateTime.MinValue;
        var version = dict.TryGetValue(nameof(StoredEvent.Version), out var cacheVersion)
            ? long.Parse(cacheVersion, CultureInfo.InvariantCulture)
            : 0;
        var metaData = dict.TryGetValue(nameof(StoredEvent.MetaData), out var cacheMetaData) ? cacheMetaData : null;

        return new StoredEvent(id, streamId, eventType, data, timestamp, version, metaData);
    }

    private Guid GetEventId(object @event)
    {
        if (@event is IIntegrationEvent integrationEvent) return integrationEvent.Id;

        var property = @event.GetType().GetProperty("Id");
        if (property != null && property.PropertyType == typeof(Guid)) return (Guid)property.GetValue(@event)!;

        return Guid.NewGuid();
    }

    private string? GetMetaData(object @event)
    {
        IDictionary<string, object>? metaDataDict = null;

        if (@event is IEventSourced eventSourced)
        {
            metaDataDict = eventSourced.GetMetaData();
        }
        else
        {
            var property = @event.GetType().GetProperty("MetaData");
            if (property == null) return metaDataDict != null ? metaDataDict.ToJson() : null;
            var value = property.GetValue(@event);
            if (value is IDictionary<string, object> dict)
                metaDataDict = dict;
            else if (value != null)
                // 如果不是字典但有值，尝试序列化它
                return value.ToJson();
        }

        return metaDataDict != null ? metaDataDict.ToJson() : null;
    }
}