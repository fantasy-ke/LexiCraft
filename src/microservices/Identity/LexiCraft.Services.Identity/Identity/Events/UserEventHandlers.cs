using BuildingBlocks.MassTransit.EventSourcing.Abstractions;
using BuildingBlocks.Mediator;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Shared.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Services.Identity.Identity.Events;

/// <summary>
///     用户登录成功领域事件处理器
/// </summary>
public sealed class UserLoginSuccessEventHandler(
    IdentityDbContext dbContext,
    ILogger<UserLoginSuccessEventHandler> logger)
    : IDomainEventHandler<UserLoginSuccessEvent>
{
    public async Task Handle(UserLoginSuccessEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == @event.UserId, cancellationToken);
            if (user == null)
            {
                logger.LogWarning("用户 {UserId} 不存在，无法更新登录成功状态", @event.UserId);
                return;
            }

            // 使用聚合根内部的 ApplyEvent 逻辑更新内存状态（确保幂等性）
            user.LoadFromHistory([@event]);
            await dbContext.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("用户 {UserId} 登录成功状态已同步到数据库 (Version: {Version})", 
                @event.UserId, user.Version);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "更新用户 {UserId} 登录成功状态失败", @event.UserId);
        }
    }
}

/// <summary>
///     用户登录失败领域事件处理器
/// </summary>
public sealed class UserLoginFailedEventHandler(
    IdentityDbContext dbContext,
    ILogger<UserLoginFailedEventHandler> logger)
    : IDomainEventHandler<UserLoginFailedEvent>
{
    public async Task Handle(UserLoginFailedEvent @event, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == @event.UserId, cancellationToken);
        if (user != null)
        {
            user.LoadFromHistory([@event]);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("用户 {UserId} 登录失败状态已同步到数据库: 失败次数 {Count}, 锁定至 {LockoutEnd}", 
                @event.UserId, @event.AccessFailedCount, @event.LockoutEnd);
        }
    }
}
