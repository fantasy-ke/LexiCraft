using System.Text.RegularExpressions;
using BuildingBlocks.Caching.Abstractions;
using BuildingBlocks.Exceptions;
using BuildingBlocks.MassTransit.Services;
using BuildingBlocks.Mediator;
using FluentValidation;
using LexiCraft.Services.Identity.Identity.Internal.Commands;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Shared.Contracts;
using LexiCraft.Services.Identity.Shared.Dtos;
using MediatR;

namespace LexiCraft.Services.Identity.Identity.Features.Login;

public record LoginCommand(string UserAccount, string Password, string? IpAddress = null) : ICommand<TokenResponse>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.UserAccount)
            .NotEmpty().WithMessage("请输入账号")
            .MaximumLength(50).WithMessage("账号长度不能超过50个字符");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("请输入密码")
            .MinimumLength(6).WithMessage("密码长度至少6位")
            .Matches("^(?=.*[0-9])(?=.*[a-zA-Z]).*$").WithMessage("密码必须包含字母和数字");
    }
}

public partial class LoginCommandHandler(
    IUserRepository userRepository,
    IMediator mediator,
    ICacheService cacheService,
    IEventPublisher eventPublisher)
    : ICommandHandler<LoginCommand, TokenResponse>
{
    public async Task<TokenResponse> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        // IP限制检查
        if (!string.IsNullOrEmpty(command.IpAddress))
        {
            var ipKey = $"Login:Ip:{command.IpAddress}";
            var ipCount = await cacheService.GetAsync<int>(ipKey, null, cancellationToken);
            if (ipCount >= 10) ThrowUserFriendlyException.ThrowException("尝试次数过多，请稍后再试");
            // 设置过期时间为1分钟
            await cacheService.SetAsync(ipKey, ipCount + 1, options => options.Expiry = TimeSpan.FromMinutes(1),
                cancellationToken);
        }

        var user = await userRepository.FirstOrDefaultAsync(x =>
            x.UserAccount == command.UserAccount || x.Email == command.UserAccount);

        if (user is null)
        {
            await mediator.Send(new PublishLoginLogCommand(command.UserAccount, "用户不存在"), cancellationToken);
            ThrowUserFriendlyException.ThrowException("用户不存在");
        }

        // 检查账户锁定状态
        if (user is { LockoutEnabled: true, LockoutEnd: not null })
            if (user.LockoutEnd.Value > DateTimeOffset.UtcNow)
                ThrowUserFriendlyException.ThrowException(
                    $"账号已锁定，请在 {user.LockoutEnd.Value.ToLocalTime():yyyy-MM-dd HH:mm:ss} 后重试");

        if (!user.VerifyPassword(command.Password))
        {
            // 记录登录失败（内部处理失败计数和锁定）
            user.LoginFailed();

            await SaveAndPublishEvents(user, cancellationToken);

            await mediator.Send(new PublishLoginLogCommand(command.UserAccount, "密码错误", user.Id), cancellationToken);
            ThrowUserFriendlyException.ThrowException("密码错误");
        }

        // 登录成功（内部处理最后登录时间和状态重置）
        user.LoginSuccess(DateTime.Now);

        await SaveAndPublishEvents(user, cancellationToken);

        return await mediator.Send(new GenerateTokenResponseCommand(user, "Password"), cancellationToken);
    }

    private async Task SaveAndPublishEvents(User user, CancellationToken cancellationToken)
    {
        var events = user.GetUncommittedEvents().ToList();
        if (events.Count == 0) return;
        foreach (var @event in events)
        {
            if (@event is IDomainEvent domainEvent)
            {
                await eventPublisher.PublishLocalAsync(domainEvent, cancellationToken);
            }
        }
        user.ClearUncommittedEvents();
    }

    [GeneratedRegex("^(?=.*[0-9])(?=.*[a-zA-Z]).*$")]
    private static partial Regex PasswordRegex();
}