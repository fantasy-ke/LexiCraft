using BuildingBlocks.Authentication.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Authentication;

/// <summary>
///     依次验证认证状态、当前登录会话和所需权限，并标记可映射为 401 或 503 的失败原因。
/// </summary>
public sealed class AuthorizeHandler(
    IUserContext userContext,
    IAccessTokenValidator accessTokenValidator,
    IPermissionCheck permissionCheck,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuthorizeHandler> logger) : AuthorizationHandler<AuthorizeRequirement>
{
    internal const string InvalidSessionItemKey = "BuildingBlocks.Authorization.InvalidSession";
    internal const string ServiceUnavailableItemKey = "BuildingBlocks.Authorization.ServiceUnavailable";

    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AuthorizeRequirement requirement)
    {
        if (!userContext.IsAuthenticated)
        {
            context.Fail();
            return;
        }

        var cancellationToken = httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        AccessTokenValidationResult tokenValidation;
        try
        {
            tokenValidation = await accessTokenValidator.ValidateAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Access token validation failed");
            FailAsUnavailable(context, "The Identity authorization session store is unavailable");
            return;
        }

        if (!tokenValidation.ServiceAvailable)
        {
            FailAsUnavailable(context, "The Identity authorization session store is unavailable");
            return;
        }

        if (!tokenValidation.SessionValid)
        {
            MarkFailure(InvalidSessionItemKey);
            context.Fail(new AuthorizationFailureReason(this,
                "The access token is no longer the current user session"));
            return;
        }

        PermissionValidationResult result;
        try
        {
            result = await permissionCheck.CheckAsync(requirement.AuthorizeName, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Permission validation failed");
            FailAsUnavailable(context, "The Identity authorization service is unavailable");
            return;
        }

        if (!result.ServiceAvailable)
        {
            FailAsUnavailable(context, "The Identity authorization service is unavailable");
            return;
        }

        if (!result.SessionValid)
        {
            MarkFailure(InvalidSessionItemKey);
            context.Fail(new AuthorizationFailureReason(this,
                "The access token is no longer the current user session"));
            return;
        }

        if (result.Granted)
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail(new AuthorizationFailureReason(this,
            $"Missing permissions: {string.Join(',', result.MissingPermissions)}"));
    }

    private void FailAsUnavailable(AuthorizationHandlerContext context, string message)
    {
        MarkFailure(ServiceUnavailableItemKey);
        context.Fail(new AuthorizationFailureReason(this, message));
    }

    private void MarkFailure(string itemKey)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext != null)
            httpContext.Items[itemKey] = true;
    }
}
