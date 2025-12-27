using System.Text.Json;
using System.Text.RegularExpressions;
using BuildingBlocks.Authentication;
using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Mediator;
using BuildingBlocks.Redis;
using FluentValidation;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using LexiCraft.Services.Identity.Shared.Dtos;
using LexiCraft.Services.Identity.Shared.Exceptions;
using Z.EventBus;

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
    IEventBus<LoginLogDto> loginEventBus) 
    : ICommandHandler<LoginCommand, TokenResponse>
{
    public async Task<TokenResponse> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.QueryNoTracking<User>()
            .FirstOrDefaultAsync(x => x.UserAccount == command.UserAccount, cancellationToken);

        if(user is null)
        {
            var loginLogDto = new LoginLogDto(null, command.UserAccount, null, DateTime.Now,
                null, null, null, "Password", false, "用户不存在");

            ThrowIdentityAuthException.ThrowException(loginEventBus, JsonSerializer.Serialize(loginLogDto));
        }

        if (!user.VerifyPassword(command.Password))
        {
            var loginLogDto = new LoginLogDto(null, command.UserAccount, null, DateTime.Now,
                null, null, null, "Password", false, "密码错误");

            ThrowIdentityAuthException.ThrowException(loginEventBus, JsonSerializer.Serialize(loginLogDto));
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
        var successLoginLogDto = new LoginLogDto(user.Id, user.UserAccount, token, DateTime.Now,
            null, null, null, "Password", true, null);

        await loginEventBus.PublishAsync(successLoginLogDto);
        await redisManager.SetAsync(string.Format(UserInfoConst.RedisTokenKey, user.Id.ToString("N")), response, TimeSpan.FromDays(7).Seconds);
        
        return response;
    }
    
    [GeneratedRegex("^(?=.*[0-9])(?=.*[a-zA-Z]).*$")]
    private static partial Regex PasswordRegex();
}