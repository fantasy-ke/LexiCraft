using BuildingBlocks.Authentication.Contract;
using Humanizer;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Identity.Identity.Features.Logout;

public static class LogoutEndpoint
{
    internal static RouteHandlerBuilder MapLogoutEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("logout", Handle)
            .WithName(nameof(Logout))
            .WithDisplayName(nameof(Logout).Humanize())
            .WithSummary("用户退出登录".Humanize())
            .WithDescription(nameof(Logout).Humanize());

        async Task<LogoutResponse> Handle(
            [AsParameters] LogoutRequestParameters requestParameters)
        {
            var (mediator, userContext, cancellationToken) = requestParameters;

            var result = await mediator.Send(new LogoutCommand(userContext.UserId), cancellationToken);

            return new LogoutResponse(result);
        }
    }
}

/// <summary>
///     用户退出登录请求参数
/// </summary>
/// <param name="Mediator"></param>
/// <param name="UserContext"></param>
/// <param name="CancellationToken"></param>
internal record LogoutRequestParameters(
    IMediator Mediator,
    IUserContext UserContext,
    CancellationToken CancellationToken
);

/// <summary>
///     用户退出登录响应
/// </summary>
/// <param name="Success">退出是否成功</param>
internal record LogoutResponse(bool Success);