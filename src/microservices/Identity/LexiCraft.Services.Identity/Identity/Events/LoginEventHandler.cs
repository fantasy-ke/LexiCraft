using BuildingBlocks.EventBus.Abstractions;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Shared.Data;
using LexiCraft.Services.Identity.Shared.Dtos;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace LexiCraft.Services.Identity.Identity.Events;

/// <summary>
///     登录事件处理器
/// </summary>
/// <param name="loginLogRepository"></param>
/// <param name="mapper"></param>
public sealed class LoginEventHandler(Shared.Data.IdentityDbContext dbContext, 
    IMapper mapper, ILogger<LoginEventHandler> logger)
    : IEventHandler<LoginLogEvent>
{
    public async Task HandleAsync(LoginLogEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = mapper.Map<LoginLog>(@event);

            await dbContext.LoginLogs.AddAsync(entity, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"登录事件处理器 消费错误信息{e.Message}");
        }
    }
}