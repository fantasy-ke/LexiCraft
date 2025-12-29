using System.Text.Json;
using System.Text.RegularExpressions;
using BuildingBlocks.Authentication;
using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Exceptions;
using BuildingBlocks.Mediator;
using BuildingBlocks.Caching.Redis;
using FluentValidation;
using LexiCraft.Services.Identity.Identity.Events.LoginLog;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using MediatR;

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
    IJwtTokenProvider jwtTokenProvider,
    ICacheManager redisManager,
    IMediator mediator) 
    : ICommandHandler<LoginCommand, TokenResponse>
{
    public async Task<TokenResponse> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.QueryNoTracking<User>()
            .FirstOrDefaultAsync(x => x.UserAccount == command.UserAccount, cancellationToken);

        if (user is null)
        {
            await mediator.Send(new PublishLoginLogCommand(command.UserAccount, "用户不存在"), cancellationToken);
            ThrowUserFriendlyException.ThrowException("用户不存在");
        }

        if (!user?.VerifyPassword(command.Password) ?? false)
        {
            await mediator.Send(new PublishLoginLogCommand(command.UserAccount, "密码错误", user.Id), cancellationToken);
            ThrowUserFriendlyException.ThrowException("密码错误");
        }

        var userDict = new Dictionary<string, string>();

        user.PasswordHash = null;
        user.PasswordSalt = null;
        userDict.Add(UserInfoConst.UserId, user.Id.ToString());
        userDict.Add(UserInfoConst.UserName, user.Username);
        userDict.Add(UserInfoConst.UserAccount, user.UserAccount);
        userDict.Add("UserInfo", JsonSerializer.Serialize(user, JsonSerializerOptions.Web));

        var token = jwtTokenProvider.GenerateAccessToken(userDict, user.Id, user.Roles.ToArray());
        var refreshToken = jwtTokenProvider.GenerateRefreshToken();
        var response = new TokenResponse(token, refreshToken);

        // 成功登录记录
        await mediator.Send(new PublishLoginLogCommand(user.UserAccount, "登录成功", user.Id, true), cancellationToken);

        await redisManager.SetAsync(string.Format(UserInfoConst.RedisTokenKey, user.Id.ToString("N")), response, TimeSpan.FromDays(7).Seconds);

        return response;
    }


    
    [GeneratedRegex("^(?=.*[0-9])(?=.*[a-zA-Z]).*$")]
    private static partial Regex PasswordRegex();
}