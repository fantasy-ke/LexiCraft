using Humanizer;
using LexiCraft.Services.Identity.Shared.Dtos;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Identity.Identity.Features.Login;

public static class LoginEndpoint
{
    internal static RouteHandlerBuilder MapLoginEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("login", Handle)
            .WithName(nameof(Login))
            .WithDisplayName(nameof(Login).Humanize())
            .WithSummary("用户登录".Humanize())
            .WithDescription(nameof(Login).Humanize())
            .AllowAnonymous(); // 登录接口允许匿名访问

        async Task<TokenResponse> Handle(
            [AsParameters] LoginRequestParameters requestParameters,
            HttpContext httpContext)
        {
            var (mediator, command, cancellationToken) = requestParameters;

            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            var commandWithIp = command with { IpAddress = ipAddress };

            var result = await mediator.Send(commandWithIp, cancellationToken);

            return result;
        }
    }
}

/// <summary>
///     用户登录请求参数
/// </summary>
/// <param name="Mediator"></param>
/// <param name="Request"></param>
/// <param name="CancellationToken"></param>
internal record LoginRequestParameters(
    IMediator Mediator,
    [FromBody] LoginCommand Command,
    CancellationToken CancellationToken
);