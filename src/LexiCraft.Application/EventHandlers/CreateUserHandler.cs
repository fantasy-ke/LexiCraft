using LexiCraft.Application.Contract.Events;
using Microsoft.Extensions.Logging;
using Z.EventBus;

namespace LexiCraft.Application.EventHandlers;

/// <summary>
/// 创建用户事件处理器
/// </summary>
public class CreateUserHandler(ILogger<CreateUserHandler> logger) : IEventHandler<CreateUserEto>
{
    public async Task HandleAsync(CreateUserEto @event, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"创建用户事件处理器 处理事件{@event}");
        await Task.CompletedTask;
    }
}