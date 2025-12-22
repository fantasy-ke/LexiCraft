using System.Text.Json;
using System.Text.RegularExpressions;
using BuildingBlocks.Authentication;
using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Mediator;
using BuildingBlocks.Redis;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Shared.Contracts;
using LexiCraft.Services.Identity.Users.Features.Login;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Services.Identity.Identity.Features.Login;

public record LoginCommand(string UserAccount, string Password) : ICommand<TokenResponse>;

public partial class LoginCommandHandler(
    IUserRepository userRepository,
    IJwtTokenProvider jwtTokenProvider,
    ICacheManager redisManager) 
    : ICommandHandler<LoginCommand, TokenResponse>
{
    public async Task<TokenResponse> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        // 验证密码强度
        if (string.IsNullOrEmpty(command.Password) 
            || command.Password.Length < 6 ||
            !PasswordRegex().IsMatch(command.Password))
        {
            throw new Exception("密码长度至少6位，且必须包含字母和数字");
        }

        // 验证用户账号
        if (string.IsNullOrEmpty(command.UserAccount))
        {
            throw new Exception("请输入账号");
        }

        var user = await userRepository.QueryNoTracking<User>()
            .FirstOrDefaultAsync(x => x.UserAccount == command.UserAccount, cancellationToken);

        if(user is null)
        {
            throw new Exception("用户不存在");
        }

        if (!user.VerifyPassword(command.Password))
        {
            throw new Exception("密码错误");
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

        await redisManager.SetAsync(string.Format(UserInfoConst.RedisTokenKey, user.Id.ToString("N")), response, TimeSpan.FromDays(7).Seconds);
        
        return response;
    }
    
    [GeneratedRegex("^(?=.*[0-9])(?=.*[a-zA-Z]).*$")]
    private static partial Regex PasswordRegex();
}