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
            .WithTags("Users")
            .WithName(nameof(Logout))
            .WithDisplayName(nameof(Logout).Humanize())
            .WithSummary("用户退出登录".Humanize())
            .WithDescription(nameof(Logout).Humanize());

        async Task<bool> Handle(
            [AsParameters] LogoutRequestParameters requestParameters)
        {
            var (mediator, userContext, cancellationToken) = requestParameters;
            
            return await mediator.Send(new LogoutCommand(userContext.UserId), cancellationToken);
        }
    }
}

/// <summary>
/// 用户退出登录请求参数
/// </summary>
/// <param name="Mediator"></param>
/// <param name="UserContext"></param>
/// <param name="CancellationToken"></param>
internal record LogoutRequestParameters(
    IMediator Mediator,
    IUserContext UserContext,
    CancellationToken CancellationToken
);