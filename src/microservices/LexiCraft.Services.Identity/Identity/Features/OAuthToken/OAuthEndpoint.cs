using Humanizer;
using Mapster;
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
            .WithName(nameof(OAuthToken))
            .WithDisplayName(nameof(OAuthToken).Humanize())
            .WithSummary("第三方授权登录".Humanize())
            .WithDescription(nameof(OAuthToken).Humanize())
            .AllowAnonymous(); // 注册接口允许匿名访问

        async Task<OAuthTokenResponse> Handle(
            [AsParameters] RegisterRequestParameters requestParameters)
        {
            var (type, code, redirectUri, mediator, cancellationToken) = requestParameters;
            
            var command = new OAuthCommand(type, code, redirectUri);
            var result = await mediator.Send(command, cancellationToken);
            
            return result.Adapt<OAuthTokenResponse>();
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

/// <summary>
/// OAuth令牌响应
/// </summary>
/// <param name="Token">访问令牌</param>
internal record OAuthTokenResponse(string Token);
