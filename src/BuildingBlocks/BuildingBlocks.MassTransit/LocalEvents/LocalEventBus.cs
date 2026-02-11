using System.Threading.Channels;
using BuildingBlocks.Mediator;

namespace BuildingBlocks.MassTransit.LocalEvents;

/// <summary>
///     基于 System.Threading.Channels 的本地事件总线实现
/// </summary>
public class LocalEventBus : ILocalEventBus
{
    private readonly Channel<IDomainEvent> _channel = Channel.CreateUnbounded<IDomainEvent>(new UnboundedChannelOptions
    {
        SingleReader = true, // 后台服务是单消费者
        SingleWriter = false // 可能有多个发布者
    });

    // 使用无界通道，确保生产者（主线程）不会因为通道满而阻塞
    // 后台服务是单消费者
    // 可能有多个发布者

    public async ValueTask PublishAsync(IDomainEvent @event, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(@event, cancellationToken);
    }

    public IAsyncEnumerable<IDomainEvent> DequeueAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}