using System.Globalization;
using System.Text.Json;
using BuildingBlocks.Authentication;
using BuildingBlocks.Authentication.Abstractions;
using BuildingBlocks.Authentication.Options;
using BuildingBlocks.Extensions.System;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Shared.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BuildingBlocks.Authentication.Tokens;
using BuildingBlocks.Authentication.Redis.Keys;

namespace LexiCraft.Services.Identity.Identity.Internal.Commands;

public record GenerateTokenResponseCommand(User User, string LoginType, string? Message = null)
    : IRequest<TokenResponse>;

public class GenerateTokenResponseCommandHandler(
    IJwtTokenProvider jwtTokenProvider,
    IAuthorizationCache authorizationCache,
    IAuthorizationSynchronization authorizationSynchronization,
    IOptionsMonitor<OAuthOptions> oauthOptions,
    IMediator mediator,
    ILogger<GenerateTokenResponseCommandHandler> logger) : IRequestHandler<GenerateTokenResponseCommand, TokenResponse>
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
        var accessTokenHash = AuthorizationTokenHasher.Hash(accessToken);
        var refreshTokenHash = AuthorizationTokenHasher.Hash(refreshToken);
        var response = new TokenResponse(accessToken, refreshToken);

        // 发布登录日志
        var logMessage = request.Message ?? "登录成功";
        await mediator.Send(
            new PublishLoginLogCommand(user.UserAccount, logMessage, user.Id, true, request.LoginType),
            cancellationToken);

        var sessionKey = string.Format(
            CultureInfo.InvariantCulture,
            AuthorizationRedisKeys.Session,
            user.Id.Value.ToString("N"));

        await authorizationSynchronization.ExecuteAsync(
            $"session:{user.Id.Value:N}",
            async token =>
            {
                var oldSession = await authorizationCache.GetAsync<AccessTokenCacheEntry>(sessionKey, token);
                var refreshTokenKey = GetRefreshTokenKey(refreshTokenHash);
                var currentOptions = oauthOptions.CurrentValue;

                // 先写新的刷新令牌，再切换当前会话。任何一步失败时，旧会话仍然可用。
                await authorizationCache.SetAsync(
                    refreshTokenKey,
                    user.Id.Value.ToString("N"),
                    GetExpiration(currentOptions.RefreshExpireMinute, TimeSpan.FromDays(7)),
                    token);
                await authorizationCache.SetAsync(
                    sessionKey,
                    new AccessTokenCacheEntry(accessTokenHash, refreshTokenHash),
                    GetExpiration(currentOptions.RefreshExpireMinute, TimeSpan.FromDays(7)),
                    token);

                if (!string.IsNullOrEmpty(oldSession?.RefreshTokenHash) &&
                    !string.Equals(oldSession.RefreshTokenHash, refreshTokenHash, StringComparison.Ordinal))
                {
                    await TryRemoveRefreshTokenAsync(user.Id.Value, oldSession.RefreshTokenHash, token);
                }

                return true;
            },
            cancellationToken);

        return response;
    }

    private async Task TryRemoveRefreshTokenAsync(
        Guid userId,
        string refreshTokenHash,
        CancellationToken cancellationToken)
    {
        try
        {
            await authorizationCache.RemoveAsync(GetRefreshTokenKey(refreshTokenHash), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // 当前会话记录是刷新令牌的最终判定依据，旧键清理失败不会恢复旧会话。
            logger.LogWarning(exception, "Failed to remove the previous refresh token for user {UserId}", userId);
        }
    }

    private static string GetRefreshTokenKey(string refreshTokenHash)
    {
        return string.Format(CultureInfo.InvariantCulture, AuthorizationRedisKeys.RefreshToken, refreshTokenHash);
    }

    private static TimeSpan GetExpiration(int configuredMinutes, TimeSpan fallback)
    {
        return configuredMinutes > 0 ? TimeSpan.FromMinutes(configuredMinutes) : fallback;
    }
}
