using System.Net;
using BuildingBlocks.Extensions.System;
using BuildingBlocks.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Authentication.Policies;

/// <summary>
///     将授权中间件结果统一转换为 <c>ResultDto</c> 格式的 401、403 或 503 响应。
/// </summary>
internal sealed class AuthorizeResultHandle(
    ILogger<AuthorizeResultHandle> logger,
    IOptionsMonitor<JsonOptions> options) : IAuthorizationMiddlewareResultHandler
{
    /// <inheritdoc />
    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (context.Items.ContainsKey(AuthorizeHandler.ServiceUnavailableItemKey))
        {
            logger.LogError("Identity authorization service unavailable for {RequestPath}", context.Request.Path);
            await WriteFailureAsync(
                context,
                HttpStatusCode.ServiceUnavailable,
                "Authorization service unavailable");
            return;
        }

        var invalidSession = context.Items.ContainsKey(AuthorizeHandler.InvalidSessionItemKey);
        if (authorizeResult.Challenged || invalidSession)
        {
            await WriteFailureAsync(
                context,
                HttpStatusCode.Unauthorized,
                "Authentication failed, token invalid");
            return;
        }

        if (authorizeResult.Forbidden)
        {
            var reason = string.Join(",",
                authorizeResult.AuthorizationFailure?.FailureReasons.Select(item => item.Message) ?? []);
            if (string.IsNullOrWhiteSpace(reason))
                reason = "Authorization failed";

            logger.LogWarning(
                "Authorization failed for {RequestPath}: {Reason}",
                context.Request.Path,
                reason);

            await WriteFailureAsync(context, HttpStatusCode.Forbidden, reason);
            return;
        }

        await next(context);
    }

    private async Task WriteFailureAsync(HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        var response = ResultDto.Fail(message, (int)statusCode);
        await context.Response.WriteAsync(response.ToJson(options.CurrentValue.SerializerOptions));
    }
}
