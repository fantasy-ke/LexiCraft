using System.Threading.Channels;
using BuildingBlocks.EventBus.Abstractions;
using BuildingBlocks.EventBus.Options;
using BuildingBlocks.EventBus.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.EventBus.Local;

/// <summary>
///     基于有界 Channel 的本地事件客户端
/// </summary>
public sealed class EventLocalClient : IDisposable
{
    private readonly AsyncLocal<int> _consumerDepth = new();
    private readonly Channel<LocalEventEnvelope> _channel;
    private readonly EventHandlerInvoker _handlerInvoker;
    private readonly IHandlerSerializer _handlerSerializer;
    private readonly ILogger<EventLocalClient> _logger;
    private readonly IServiceProvider _serviceProvider;
    private int _completed;
    private int _disposed;

    public EventLocalClient(
        IServiceProvider serviceProvider,
        ILogger<EventLocalClient> logger,
        IHandlerSerializer handlerSerializer,
        EventHandlerInvoker handlerInvoker,
        IOptions<EventBusOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _handlerSerializer = handlerSerializer;
        _handlerInvoker = handlerInvoker;

        _channel = Channel.CreateBounded<LocalEventEnvelope>(new BoundedChannelOptions(options.Value.Local.Capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public async ValueTask PublishAsync(
        Type eventType,
        string eventData,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(eventData);

        if (Volatile.Read(ref _completed) != 0)
            throw new EventClientException("本地事件总线已停止，无法继续发布事件");

        var envelope = new LocalEventEnvelope(eventType, eventData);
        if (_channel.Writer.TryWrite(envelope)) return;

        if (_consumerDepth.Value > 0)
            throw new EventClientException(
                "本地事件处理器不能在队列已满时同步等待再次发布，这会阻塞唯一消费者");

        try
        {
            await _channel.Writer.WriteAsync(envelope, cancellationToken);
        }
        catch (ChannelClosedException ex)
        {
            throw new EventClientException("本地事件总线已停止，无法继续发布事件", ex);
        }
    }

    public async Task ConsumeStartAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var envelope in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                var previousDepth = _consumerDepth.Value;
                _consumerDepth.Value = previousDepth + 1;
                try
                {
                    await ProcessEnvelopeAsync(envelope, stoppingToken);
                }
                finally
                {
                    _consumerDepth.Value = previousDepth;
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host 正常停止。
        }
    }

    public void Complete()
    {
        if (Interlocked.Exchange(ref _completed, 1) != 0) return;
        _channel.Writer.TryComplete();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        Complete();
    }

    private async Task ProcessEnvelopeAsync(LocalEventEnvelope envelope, CancellationToken cancellationToken)
    {
        try
        {
            var eventData = _handlerSerializer.Deserialize(envelope.EventData, envelope.EventType);
            if (eventData == null)
            {
                _logger.LogWarning("无法反序列化本地事件: {EventType}", envelope.EventType.FullName);
                return;
            }

            await using var scope = _serviceProvider.CreateAsyncScope();
            var handlerType = typeof(IEventHandler<>).MakeGenericType(envelope.EventType);
            var handlers = scope.ServiceProvider.GetServices(handlerType).Where(handler => handler != null).ToArray();

            foreach (var handler in handlers)
            {
                try
                {
                    await _handlerInvoker.InvokeAsync(handler!, envelope.EventType, eventData, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "本地事件处理器执行失败: {EventType}, Handler: {HandlerType}",
                        envelope.EventType.FullName, handler!.GetType().FullName);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "消费本地事件时发生错误: {EventType}", envelope.EventType.FullName);
        }
    }

    private sealed record LocalEventEnvelope(Type EventType, string EventData);
}
