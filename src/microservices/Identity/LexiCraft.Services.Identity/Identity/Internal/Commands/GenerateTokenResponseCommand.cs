using System.Text.Json;
using BuildingBlocks.Authentication;
using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Caching.Abstractions;
using BuildingBlocks.Extensions.System;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Identity.Models.Enum;
using LexiCraft.Services.Identity.Shared.Dtos;
using MediatR;

namespace LexiCraft.Services.Identity.Identity.Internal.Commands;

public record GenerateTokenResponseCommand(User User, string LoginType, string? Message = null) : IRequest<TokenResponse>;

public class GenerateTokenResponseCommandHandler(
    IJwtTokenProvider jwtTokenProvider,
    ICacheService cacheService,
    IMediator mediator) : IRequestHandler<GenerateTokenResponseCommand, TokenResponse>
{
    public async Task<TokenResponse> Handle(GenerateTokenResponseCommand request, CancellationToken cancellationToken)
    {
        var user = request.User;
        var userDict = new Dictionary<string, string>();

        // 我们不直接修改传入的对象，而是克隆一个用于序列化的版本
        var userForClaims = user.ToJson(JsonSerializerOptions.Web).FromJson<User>(JsonSerializerOptions.Web);
        if (userForClaims != null)
        {
            userForClaims.ClearPassword();
            userDict.Add(UserInfoConst.UserId, user.Id.ToString());
            userDict.Add(UserInfoConst.UserName, user.Username);
            userDict.Add(UserInfoConst.UserAccount, user.UserAccount);
            userDict.Add("UserInfo", userForClaims.ToJson(JsonSerializerOptions.Web));
        }

        var accessToken = jwtTokenProvider.GenerateAccessToken(userDict, user.Id.Value, user.Roles.ToArray());
        var refreshToken = jwtTokenProvider.GenerateRefreshToken();

        var response = new TokenResponse(accessToken, refreshToken);

        // 发布登录日志
        var logMessage = request.Message ?? "登录成功";
        await mediator.Send(
            new PublishLoginLogCommand(user.UserAccount, logMessage, user.Id, true, request.LoginType),
            cancellationToken);

        var cacheKey = string.Format(UserInfoConst.RedisTokenKey, user.Id.Value.ToString("N"));

        // 检查Redis中是否存在该用户的Token记录，如果存在则先删除旧的Token
        var oldToken = await cacheService.GetAsync<TokenResponse>(cacheKey, cancellationToken: cancellationToken);
        if (oldToken != null)
        {
            if (!string.IsNullOrEmpty(oldToken.RefreshToken))
            {
                var oldRefreshTokenKey = string.Format(UserInfoConst.RedisRefreshTokenKey, oldToken.RefreshToken);
                await cacheService.RemoveAsync(oldRefreshTokenKey, cancellationToken: cancellationToken);
            }

            await cacheService.RemoveAsync(cacheKey, cancellationToken: cancellationToken);
        }

        await cacheService.SetAsync(cacheKey, response,
            options => options.Expiry = TimeSpan.FromDays(7), cancellationToken);
        await cacheService.SetAsync(string.Format(UserInfoConst.RedisRefreshTokenKey, refreshToken),
            user.Id.Value.ToString("N"), options => options.Expiry = TimeSpan.FromDays(7), cancellationToken);

        return response;
    }
}