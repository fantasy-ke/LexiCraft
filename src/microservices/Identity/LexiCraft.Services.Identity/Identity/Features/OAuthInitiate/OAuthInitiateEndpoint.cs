using Humanizer;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Identity.Identity.Features.OAuthInitiate;

public static class OAuthInitiateEndpoint
{
    public static RouteHandlerBuilder MapOAuthInitiateEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("oauth/{provider}/initiate", Handle)
            .WithName(nameof(OAuthInitiate))
            .WithDisplayName(nameof(OAuthInitiate).Humanize())
            .WithSummary("获取OAuth授权URL")
            .WithDescription("根据OAuth提供商类型获取授权跳转URL")
            .AllowAnonymous(); // OAuth初始化接口允许匿名访问

        async Task<OAuthInitiateResponse> Handle(
            [AsParameters] OAuthInitiateRequestParameters requestParameters)
        {
            var (provider, mediator, cancellationToken) = requestParameters;

            var query = new OAuthInitiateQuery(provider);
            var authorizationUrl = await mediator.Send(query, cancellationToken);

            return new OAuthInitiateResponse(authorizationUrl);
        }
    }
}

/// <summary>
///     OAuth初始化请求参数
/// </summary>
internal record OAuthInitiateRequestParameters(
    [FromRoute] string Provider,
    IMediator Mediator,
    CancellationToken CancellationToken
);

/// <summary>
///     OAuth初始化响应
/// </summary>
/// <param name="AuthorizationUrl">OAuth授权URL</param>
internal record OAuthInitiateResponse(string AuthorizationUrl);