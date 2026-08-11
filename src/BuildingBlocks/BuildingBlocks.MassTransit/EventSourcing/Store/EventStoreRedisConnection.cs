using BuildingBlocks.MassTransit.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BuildingBlocks.MassTransit.EventSourcing.Store;

/// <summary>
///     事件存储专用 Redis 连接，避免与缓存或其他消息组件共享错误的连接配置。
/// </summary>
public sealed class EventStoreRedisConnection : IDisposable
{
    private int _disposed;

    public EventStoreRedisConnection(IOptions<MassTransitOptions> options)
    {
        var connectionString = options.Value.EventSourcing.RedisConnectionString;
        Multiplexer = ConnectionMultiplexer.Connect(ConfigurationOptions.Parse(connectionString));
    }

    public IConnectionMultiplexer Multiplexer { get; }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        Multiplexer.Dispose();
    }
}
