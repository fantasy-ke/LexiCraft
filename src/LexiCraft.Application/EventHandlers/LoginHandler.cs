﻿using Gnarly.Data;
using LexiCraft.Application.Contract.Events;
using LexiCraft.Domain;
using LexiCraft.Domain.LoginLogs;
using MapsterMapper;
using Z.EventBus;

namespace LexiCraft.Application.EventHandlers;

/// <summary>
/// 登录事件处理器
/// </summary>
/// <param name="loginLogRepository"></param>
/// <param name="mapper"></param>
public sealed class LoginHandler(IRepository<LoginLog> loginLogRepository, IMapper mapper)
    : IEventHandler<LoginEto>
{
    public async Task HandleAsync(LoginEto @event, CancellationToken cancellationToken = default)
    {
        var entity = mapper.Map<LoginLog>(@event);

        await loginLogRepository.InsertAsync(entity);

        await loginLogRepository.SaveChangesAsync();
    }
}