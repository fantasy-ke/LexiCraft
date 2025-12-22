using Humanizer;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Identity.Identity.Features.OAuthToken;

public static class OAuthEndpoint
{
    public static RouteHandlerBuilder MapOAuthEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("oauthToken", Handle)
            .WithTags("OAuthToken")
            .WithName(nameof(OAuthToken))
            .WithDisplayName(nameof(OAuthToken).Humanize())
            .WithSummary("第三方授权登录".Humanize())
            .WithDescription(nameof(OAuthToken).Humanize())
            .AllowAnonymous(); // 注册接口允许匿名访问

        async Task<string> Handle(
            [AsParameters] RegisterRequestParameters requestParameters)
        {
            var (type, code, redirectUri, mediator, cancellationToken) = requestParameters;
            
            var command = new OAuthCommand(type, code, redirectUri);
            
            return await mediator.Send(command, cancellationToken);
        }
    }
}


internal record RegisterRequestParameters(
    [FromQuery] string Type,
    [FromQuery] string Code,
    [FromQuery] string? RedirectUri,
    IMediator Mediator,
    CancellationToken CancellationToken
);
