using System.Globalization;
using System.Text.Json;
using BuildingBlocks.Authentication;
using BuildingBlocks.Authentication.Abstractions;
using BuildingBlocks.Authentication.Options;
using BuildingBlocks.Exceptions;
using BuildingBlocks.Extensions.System;
using BuildingBlocks.Mediator;
using FluentValidation;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Shared.Contracts;
using LexiCraft.Services.Identity.Shared.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BuildingBlocks.Authentication.Tokens;
using BuildingBlocks.Authentication.Redis.Keys;

namespace LexiCraft.Services.Identity.Identity.Features.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : ICommand<TokenResponse>;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("刷新令牌不能为空");
    }
}

public class RefreshTokenCommandHandler(
    IAuthorizationCache authorizationCache,
    IAuthorizationSynchronization authorizationSynchronization,
    IUserRepository userRepository,
    IJwtTokenProvider jwtTokenProvider,
    IOptionsMonitor<OAuthOptions> oauthOptions,
    ILogger<RefreshTokenCommandHandler> logger) : ICommandHandler<RefreshTokenCommand, TokenResponse>
{
    public Task<TokenResponse> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var refreshTokenHash = AuthorizationTokenHasher.Hash(command.RefreshToken);
        var refreshTokenKey = GetRefreshTokenKey(refreshTokenHash);

        return authorizationSynchronization.ExecuteAsync(
            $"refresh:{refreshTokenHash}",
            async refreshTokenCancellationToken =>
            {
                var userIdValue = await authorizationCache.GetAsync<string>(
                    refreshTokenKey,
                    refreshTokenCancellationToken);
                if (string.IsNullOrWhiteSpace(userIdValue))
                    return ThrowInvalidRefreshToken("刷新令牌无效或已过期");

                if (!Guid.TryParse(userIdValue, out var userId))
                    return ThrowInvalidRefreshToken("刷新令牌无效");

                return await authorizationSynchronization.ExecuteAsync(
                    $"session:{userId:N}",
                    async sessionCancellationToken =>
                    {
                        var currentUserIdValue = await authorizationCache.GetAsync<string>(
                            refreshTokenKey,
                            sessionCancellationToken);
                        var sessionKey = string.Format(
                            CultureInfo.InvariantCulture,
                            AuthorizationRedisKeys.Session,
                            userId.ToString("N"));
                        var currentSession = await authorizationCache.GetAsync<AccessTokenCacheEntry>(
                            sessionKey,
                            sessionCancellationToken);

                        if (!string.Equals(currentUserIdValue, userIdValue, StringComparison.Ordinal) ||
                            !string.Equals(
                                currentSession?.RefreshTokenHash,
                                refreshTokenHash,
                                StringComparison.Ordinal))
                        {
                            await TryRemoveRefreshTokenAsync(refreshTokenHash, sessionCancellationToken);
                            return ThrowInvalidRefreshToken("刷新令牌无效或已过期");
                        }

                        var user = await userRepository.QueryNoTracking()
                            .FirstOrDefaultAsync(x => x.Id == userId, sessionCancellationToken);
                        if (user == null)
                            return ThrowInvalidRefreshToken("用户不存在");

                        var userDict = new Dictionary<string, string>();
                        var userForClaims = user.ToJson(JsonSerializerOptions.Web)
                            .FromJson<User>(JsonSerializerOptions.Web);
                        if (userForClaims != null)
                        {
                            userForClaims.ClearPassword();
                            userDict.Add(UserInfoConst.UserId, user.Id.ToString());
                            userDict.Add(UserInfoConst.UserName, user.Username);
                            userDict.Add(UserInfoConst.UserAccount, user.UserAccount);
                            userDict.Add("UserInfo", userForClaims.ToJson(JsonSerializerOptions.Web));
                        }

                        var accessToken = jwtTokenProvider.GenerateAccessToken(
                            userDict,
                            user.Id.Value,
                            user.Roles.ToArray());
                        var newRefreshToken = jwtTokenProvider.GenerateRefreshToken();
                        var accessTokenHash = AuthorizationTokenHasher.Hash(accessToken);
                        var newRefreshTokenHash = AuthorizationTokenHasher.Hash(newRefreshToken);
                        var currentOptions = oauthOptions.CurrentValue;

                        // 先写新的刷新令牌，再切换当前会话。旧刷新令牌即使清理失败也会因会话指针不匹配而失效。
                        await authorizationCache.SetAsync(
                            GetRefreshTokenKey(newRefreshTokenHash),
                            user.Id.Value.ToString("N"),
                            GetExpiration(currentOptions.RefreshExpireMinute, TimeSpan.FromDays(7)),
                            sessionCancellationToken);
                        await authorizationCache.SetAsync(
                            sessionKey,
                            new AccessTokenCacheEntry(accessTokenHash, newRefreshTokenHash),
                            GetExpiration(currentOptions.RefreshExpireMinute, TimeSpan.FromDays(7)),
                            sessionCancellationToken);
                        await TryRemoveRefreshTokenAsync(refreshTokenHash, sessionCancellationToken);

                        return new TokenResponse(accessToken, newRefreshToken);
                    },
                    refreshTokenCancellationToken);
            },
            cancellationToken);
    }

    private async Task TryRemoveRefreshTokenAsync(string refreshTokenHash, CancellationToken cancellationToken)
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
            logger.LogWarning(exception, "Failed to remove a stale refresh token");
        }
    }

    private static TokenResponse ThrowInvalidRefreshToken(string message)
    {
        ThrowUserFriendlyException.ThrowException(message);
        throw new InvalidOperationException(message);
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
