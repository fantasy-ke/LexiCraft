using System.Net.Http.Headers;
using BuildingBlocks.Authentication.Contract;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Authentication.Permission;

public sealed class RedisAccessTokenValidator(
    IHttpContextAccessor httpContextAccessor,
    IUserContext userContext,
    IAuthorizationCache authorizationCache,
    ILogger<RedisAccessTokenValidator> logger) : IAccessTokenValidator
{
    public async Task<AccessTokenValidationResult> ValidateAsync(
        CancellationToken cancellationToken = default)
    {
        if (userContext.UserId == Guid.Empty)
            return AccessTokenValidationResult.InvalidSession;

        var authorizationHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!AuthenticationHeaderValue.TryParse(authorizationHeader, out var header) ||
            !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(header.Parameter))
            return AccessTokenValidationResult.InvalidSession;

        try
        {
            var cacheKey = string.Format(UserInfoConst.RedisAuthorizationSessionKey, userContext.UserId.ToString("N"));
            var currentSession = await authorizationCache.GetAsync<AccessTokenCacheEntry>(cacheKey, cancellationToken);

            return currentSession != null &&
                   string.Equals(
                       currentSession.AccessTokenHash,
                       AuthorizationTokenHasher.Hash(header.Parameter),
                       StringComparison.Ordinal)
                ? AccessTokenValidationResult.Current
                : AccessTokenValidationResult.InvalidSession;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to validate the current access token for user {UserId}",
                userContext.UserId);
            return AccessTokenValidationResult.Unavailable;
        }
    }
}
