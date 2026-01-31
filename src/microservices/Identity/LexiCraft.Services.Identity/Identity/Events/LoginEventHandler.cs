using BuildingBlocks.EventBus.Abstractions;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Shared.Data;
using LexiCraft.Services.Identity.Shared.Dtos;
using Microsoft.Extensions.Logging;

namespace LexiCraft.Services.Identity.Identity.Events;

/// <summary>
///     登录事件处理器
/// </summary>
public sealed class LoginEventHandler(IdentityDbContext dbContext, 
    ILogger<LoginEventHandler> logger)
    : IEventHandler<LoginLogEvent>
{
    public async Task HandleAsync(LoginLogEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            // 手动映射以避免 Mapster 构造函数问题，并正确处理领域逻辑
            var entity = new LoginLog(
                @event.LoginType ?? "Unknown",
                @event.LoginTime,
                @event.Ip,
                @event.UserAgent,
                @event.Origin
            );

            entity.SetUser(@event.UserId, @event.Username);

            if (@event.IsSuccess)
            {
                entity.SetSuccess(null, @event.Message);
            }
            else
            {
                entity.SetFailure(@event.Message ?? string.Empty);
            }

            await dbContext.LoginLogs.AddAsync(entity, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"登录事件处理器 消费错误信息{e.Message}");
        }
    }
}