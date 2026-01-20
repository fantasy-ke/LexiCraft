using System.Text.RegularExpressions;
using BuildingBlocks.Caching.Abstractions;
using BuildingBlocks.Exceptions;
using BuildingBlocks.Mediator;
using FluentValidation;
using LexiCraft.Services.Identity.Identity.Internal.Commands;
using LexiCraft.Services.Identity.Shared.Contracts;
using MediatR;
using LexiCraft.Services.Identity.Shared.Dtos;

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
    ICacheService cacheService) 
    : ICommandHandler<LoginCommand, TokenResponse>
{
    public async Task<TokenResponse> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        // IP限制检查
        if (!string.IsNullOrEmpty(command.IpAddress))
        {
            var ipKey = $"Login:Ip:{command.IpAddress}";
            var ipCount = await cacheService.GetAsync<int>(ipKey, null, cancellationToken);
            if (ipCount >= 10)
            {
                ThrowUserFriendlyException.ThrowException("尝试次数过多，请稍后再试");
            }
            // 设置过期时间为1分钟
            await cacheService.SetAsync(ipKey, ipCount + 1, options => options.Expiry = TimeSpan.FromMinutes(1), cancellationToken);
        }

        var user = await userRepository.FirstOrDefaultAsync(x => x.UserAccount == command.UserAccount || x.Email == command.UserAccount);

        if (user is null)
        {
            await mediator.Send(new PublishLoginLogCommand(command.UserAccount, "用户不存在"), cancellationToken);
            ThrowUserFriendlyException.ThrowException("用户不存在");
        }

        // 检查账户锁定状态
        if (user is { LockoutEnabled: true, LockoutEnd: not null })
        {
            if (user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                ThrowUserFriendlyException.ThrowException($"账号已锁定，请在 {user.LockoutEnd.Value.ToLocalTime():yyyy-MM-dd HH:mm:ss} 后重试");
            }
        }

        if (!user.VerifyPassword(command.Password))
        {
            // 记录登录失败
            user.AccessFailed();
            
            // 如果失败次数达到阈值（例如5次），锁定账户
            if (user.LockoutEnabled && user.AccessFailedCount >= 5)
            {
                user.Lockout(DateTimeOffset.UtcNow.AddMinutes(5));
                user.ResetAccessFailedCount(); // 锁定后重置计数，或者保留计数直到解锁
                // 这里选择锁定后重置计数，意味着5分钟后解锁，用户又有5次机会。
            }

            await userRepository.UpdateAsync(user);
            await userRepository.SaveChangesAsync();

            await mediator.Send(new PublishLoginLogCommand(command.UserAccount, "密码错误", user.Id), cancellationToken); 
            ThrowUserFriendlyException.ThrowException("密码错误");
        }

        // 登录成功，重置失败计数和锁定状态
        if (user is { AccessFailedCount: <= 0, LockoutEnd: null })
            return await mediator.Send(new GenerateTokenResponseCommand(user, "Password"), cancellationToken);
        user.ResetAccessFailedCount();
        await userRepository.UpdateAsync(user);
        await userRepository.SaveChangesAsync();

        return await mediator.Send(new GenerateTokenResponseCommand(user, "Password"), cancellationToken);
    }
    
    [GeneratedRegex("^(?=.*[0-9])(?=.*[a-zA-Z]).*$")]
    private static partial Regex PasswordRegex();
}
