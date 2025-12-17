using BuildingBlocks.Domain;
using LexiCraft.AuthServer.Application.Contract.Events;
using LexiCraft.AuthServer.Domain.LoginLogs;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Z.EventBus;

namespace LexiCraft.AuthServer.Application.EventHandlers;

/// <summary>
/// 登录事件处理器
/// </summary>
/// <param name="loginLogRepository"></param>
/// <param name="mapper"></param>
public sealed class LoginHandler(IRepository<LoginLog> loginLogRepository, 
    IMapper mapper,ILogger<LoginHandler> logger)
    : IEventHandler<LoginEto>
{
    public async Task HandleAsync(LoginEto @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = mapper.Map<LoginLog>(@event);

            await loginLogRepository.InsertAsync(entity);

            await loginLogRepository.SaveChangesAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, $"登录事件处理器 消费错误信息{e.Message}");
        }
        
    }
}