using System.Text.RegularExpressions;
using BuildingBlocks.Exceptions;
using BuildingBlocks.Mediator;
using FluentValidation;
using LexiCraft.Services.Identity.Identity.Events.LoginLog;
using LexiCraft.Services.Identity.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using MediatR;
using LexiCraft.Services.Identity.Identity.Features.GenerateToken;
using LexiCraft.Services.Identity.Shared.Dtos;

namespace LexiCraft.Services.Identity.Identity.Features.Login;

public record LoginCommand(string UserAccount, string Password) : ICommand<TokenResponse>;

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
    IMediator mediator) 
    : ICommandHandler<LoginCommand, TokenResponse>
{
    public async Task<TokenResponse> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.QueryNoTracking()
            .FirstOrDefaultAsync(x => x.UserAccount == command.UserAccount || x.Email == command.UserAccount, cancellationToken);

        if (user is null)
        {
            await mediator.Send(new PublishLoginLogCommand(command.UserAccount, "用户不存在"), cancellationToken);
            ThrowUserFriendlyException.ThrowException("用户不存在");
        }

        if (!user.VerifyPassword(command.Password))
        {
            await mediator.Send(new PublishLoginLogCommand(command.UserAccount, "密码错误", user.Id), cancellationToken); 
            ThrowUserFriendlyException.ThrowException("密码错误");
        }

        return await mediator.Send(new GenerateTokenResponseCommand(user, "Password"), cancellationToken);
    }
    
    [GeneratedRegex("^(?=.*[0-9])(?=.*[a-zA-Z]).*$")]
    private static partial Regex PasswordRegex();
}
