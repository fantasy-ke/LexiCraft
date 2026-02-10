using BuildingBlocks.MassTransit.EventSourcing.Abstractions;
using BuildingBlocks.Mediator;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Shared.Data;
using LexiCraft.Services.Identity.Shared.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LexiCraft.Services.Identity.Identity.Events;

/// <summary>
///     登录事件处理器
/// </summary>
public sealed class LoginEventHandler(IdentityDbContext dbContext, 
    IEventStore eventStore,
    ILogger<LoginEventHandler> logger)
    : IDomainEventHandler<LoginLogEvent>
{
    public async Task Handle(LoginLogEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            // 事件溯源：记录登录事件
            // StreamId 使用 UserId (如果存在) 或 用户名 (如果是未知用户尝试)
            // 加上前缀 "identity-login-" 区分业务领域
            var streamId = $"identity-login-{(@event.UserId.HasValue ? @event.UserId.Value.ToString() : @event.Username)}";
            await eventStore.AppendEventsAsync(streamId, [@event], cancellationToken: cancellationToken);

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