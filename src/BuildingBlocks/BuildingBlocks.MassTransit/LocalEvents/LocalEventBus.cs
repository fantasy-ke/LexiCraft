using System.Threading.Channels;
using BuildingBlocks.MassTransit.Options;
using BuildingBlocks.Mediator;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.MassTransit.LocalEvents;

/// <summary>
///     基于有界 Channel 的本地事件总线实现
/// </summary>
public sealed class LocalEventBus : ILocalEventBus
{
    private readonly AsyncLocal<int> _consumerDepth = new();
    private readonly Channel<IDomainEvent> _channel;
    private int _completed;

    public LocalEventBus(IOptions<MassTransitOptions> options)
    {
        _channel = Channel.CreateBounded<IDomainEvent>(
            new BoundedChannelOptions(options.Value.LocalEvents.Capacity)
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false,
                FullMode = BoundedChannelFullMode.Wait
            });
    }

    public async ValueTask PublishAsync(IDomainEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (Volatile.Read(ref _completed) != 0)
            throw new ChannelClosedException();

        if (_channel.Writer.TryWrite(@event)) return;

        if (_consumerDepth.Value > 0)
            throw new InvalidOperationException(
                "本地事件处理器不能在队列已满时同步等待再次发布，这会阻塞唯一消费者");

        await _channel.Writer.WriteAsync(@event, cancellationToken);
    }

    public IAsyncEnumerable<IDomainEvent> DequeueAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    public void Complete()
    {
        if (Interlocked.Exchange(ref _completed, 1) != 0) return;
        _channel.Writer.TryComplete();
    }

    internal IDisposable EnterConsumerScope()
    {
        var previousDepth = _consumerDepth.Value;
        _consumerDepth.Value = previousDepth + 1;
        return new ConsumerScope(_consumerDepth, previousDepth);
    }

    private sealed class ConsumerScope(AsyncLocal<int> consumerDepth, int previousDepth) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            consumerDepth.Value = previousDepth;
        }
    }
}
