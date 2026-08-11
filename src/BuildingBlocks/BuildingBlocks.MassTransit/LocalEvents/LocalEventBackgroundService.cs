using BuildingBlocks.MassTransit.EventSourcing.Abstractions;
using BuildingBlocks.Mediator;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.MassTransit.LocalEvents;

/// <summary>
///     后台任务，负责消费本地事件总线中的消息并通过 MediatR 分发
/// </summary>
public sealed class LocalEventBackgroundService(
    ILocalEventBus localEventBus,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<LocalEventBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("本地事件后台处理服务已启动");

        try
        {
            await foreach (var @event in localEventBus.DequeueAsync(stoppingToken))
            {
                using var consumerScope = (localEventBus as LocalEventBus)?.EnterConsumerScope();
                await ProcessEventAsync(@event, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("本地事件后台处理服务正在停止");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "处理本地事件流时发生未捕获的错误");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        localEventBus.Complete();
        await base.StopAsync(cancellationToken);
    }

    private async Task ProcessEventAsync(IDomainEvent @event, CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();

        await HandleEventSourcingAsync(scope.ServiceProvider, @event, cancellationToken);

        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        try
        {
            logger.LogDebug("后台处理本地事件: {EventType}", @event.GetType().Name);
            await publisher.Publish(@event, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "后台处理本地事件 {EventType} 时发生错误", @event.GetType().Name);
        }
    }

    private async Task HandleEventSourcingAsync(
        IServiceProvider serviceProvider,
        IDomainEvent @event,
        CancellationToken cancellationToken)
    {
        if (@event is not IEventSourced eventSourced) return;

        try
        {
            var eventStore = serviceProvider.GetService<IEventStore>();
            if (eventStore == null) return;

            var streamId = eventSourced.GetStreamId();
            logger.LogDebug("自动化事件溯源: {EventType} -> Stream: {StreamId}",
                @event.GetType().Name, streamId);
            await eventStore.AppendEventsAsync(streamId, [@event], cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "自动化事件溯源处理失败: {EventType}", @event.GetType().Name);
        }
    }
}
