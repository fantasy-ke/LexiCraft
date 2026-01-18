using System.Text.Json;
using BuildingBlocks.Authentication;
using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Caching.Redis;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Identity.Models.Enum;
using LexiCraft.Services.Identity.Shared.Dtos;
using MediatR;

namespace LexiCraft.Services.Identity.Identity.Internal.Commands;

public record GenerateTokenResponseCommand(User User, string? Message = null) : IRequest<TokenResponse>;

public class GenerateTokenResponseCommandHandler(
    IJwtTokenProvider jwtTokenProvider,
    ICacheManager redisManager,
    IMediator mediator) : IRequestHandler<GenerateTokenResponseCommand, TokenResponse>
{
    public async Task<TokenResponse> Handle(GenerateTokenResponseCommand request, CancellationToken cancellationToken)
    {
        var user = request.User;
        var userDict = new Dictionary<string, string>();
        
        // 我们不直接修改传入的对象，而是克隆一个用于序列化的版本
        var userForClaims = JsonSerializer.Deserialize<User>(JsonSerializer.Serialize(user, JsonSerializerOptions.Web));
        if (userForClaims != null)
        {
            userForClaims.PasswordHash = null;
            userDict.Add(UserInfoConst.UserId, user.Id.ToString());
            userDict.Add(UserInfoConst.UserName, user.Username);
            userDict.Add(UserInfoConst.UserAccount, user.UserAccount);
            userDict.Add("UserInfo", JsonSerializer.Serialize(userForClaims, JsonSerializerOptions.Web));
        }

        var token = jwtTokenProvider.GenerateAccessToken(userDict, user.Id, user.Roles.ToArray());
        var refreshToken = jwtTokenProvider.GenerateRefreshToken();
        var response = new TokenResponse(token, refreshToken);

        // 发布登录日志
        var logMessage = request.Message ?? (user.Source == SourceEnum.Register ? "注册成功" : "登录成功");
        await mediator.Send(new PublishLoginLogCommand(user.UserAccount, logMessage, user.Id, true, user.Source.ToString()), cancellationToken);

        await redisManager.SetAsync(string.Format(UserInfoConst.RedisTokenKey, user.Id.ToString("N")), response, TimeSpan.FromDays(7));
        await redisManager.SetAsync(string.Format(UserInfoConst.RedisRefreshTokenKey, refreshToken), user.Id.ToString("N"), TimeSpan.FromDays(7));

        return response;
    }
}
