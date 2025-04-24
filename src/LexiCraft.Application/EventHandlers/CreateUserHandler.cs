using LexiCraft.Application.Contract.Events;
using Z.EventBus;

namespace LexiCraft.Application.EventHandlers;

/// <summary>
/// 创建用户事件处理器
/// </summary>
public class CreateUserHandler : IEventHandler<CreateUserEto>
{
    public async Task HandleAsync(CreateUserEto @event, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }
}