using BuildingBlocks.Domain;
using LexiCraft.Services.Identity.Shared.Dtos;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using BuildingBlocks.EventBus.Abstractions;

namespace LexiCraft.Services.Identity.Identity.Events;

/// <summary>
/// 登录事件处理器
/// </summary>
/// <param name="loginLogRepository"></param>
/// <param name="mapper"></param>
public sealed class LoginEventHandler(IRepository<Models.LoginLog> loginLogRepository, 
    IMapper mapper, ILogger<LoginEventHandler> logger)
    : IEventHandler<LoginLogDto>
{
    public async Task HandleAsync(LoginLogDto @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = mapper.Map<Models.LoginLog>(@event);

            await loginLogRepository.InsertAsync(entity);

            await loginLogRepository.SaveChangesAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, $"登录事件处理器 消费错误信息{e.Message}");
        }
        
    }
}