using BuildingBlocks.EventBus.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BuildingBlocks.EventBus.Redis;

public sealed class RedisEventBusConnection : IDisposable
{
    private int _disposed;

    public RedisEventBusConnection(IOptions<EventBusOptions> options)
    {
        var connectionString = options.Value.Redis.ConnectionString
            ?? throw new InvalidOperationException("启用 Redis EventBus 时必须提供 ConnectionString");

        Multiplexer = ConnectionMultiplexer.Connect(connectionString);
    }

    public IConnectionMultiplexer Multiplexer { get; }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        Multiplexer.Dispose();
    }
}
