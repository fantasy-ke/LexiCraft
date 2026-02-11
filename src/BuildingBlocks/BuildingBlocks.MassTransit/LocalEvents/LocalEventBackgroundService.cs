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
public class LocalEventBackgroundService(
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
                await ProcessEventAsync(@event, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("本地事件后台处理服务正在停止");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "处理本地事件流时发生未捕获的错误");
        }
    }

    private async Task ProcessEventAsync(IDomainEvent @event, CancellationToken cancellationToken)
    {
        // 为每个事件创建一个新的服务范围，以便正确注入 Scoped 服务（如 DbContext）
        using var scope = serviceScopeFactory.CreateScope();

        // 自动化事件溯源处理
        await HandleEventSourcingAsync(scope.ServiceProvider, @event, cancellationToken);

        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        try
        {
            logger.LogDebug("后台处理本地事件: {EventType}", @event.GetType().Name);
            await publisher.Publish(@event, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "后台处理本地事件 {EventType} 时发生错误", @event.GetType().Name);
        }
    }

    private async Task HandleEventSourcingAsync(IServiceProvider serviceProvider, IDomainEvent @event,
        CancellationToken cancellationToken)
    {
        if (@event is IEventSourced eventSourced)
            try
            {
                var eventStore = serviceProvider.GetService<IEventStore>();
                if (eventStore != null)
                {
                    var streamId = eventSourced.GetStreamId();
                    logger.LogDebug("自动化事件溯源: {EventType} -> Stream: {StreamId}", @event.GetType().Name, streamId);
                    await eventStore.AppendEventsAsync(streamId, [@event], cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "自动化事件溯源处理失败: {EventType}", @event.GetType().Name);
            }
    }
}