using Humanizer;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using LexiCraft.Services.Identity.Shared.Dtos;

namespace LexiCraft.Services.Identity.Identity.Features.OAuthToken;

public static class OAuthEndpoint
{
    public static RouteHandlerBuilder MapOAuthEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("oauth/{provider}/callback", Handle)
            .WithName(nameof(OAuthToken))
            .WithDisplayName(nameof(OAuthToken).Humanize())
            .WithSummary("第三方OAuth回调接口")
            .WithDescription("处理OAuth提供商的授权回调，返回访问令牌和刷新令牌")
            .AllowAnonymous(); // OAuth回调接口允许匿名访问

        async Task<TokenResponse> Handle(
            [AsParameters] OAuthCallbackRequestParameters requestParameters)
        {
            var (provider, request, mediator, cancellationToken) = requestParameters;
            
            var command = new OAuthCommand(provider, request.Code, request.RedirectUri);
            var result = await mediator.Send(command, cancellationToken);
            
            return result;
        }
    }
}

/// <summary>
/// OAuth回调请求参数
/// </summary>
internal record OAuthCallbackRequestParameters(
    [FromRoute] string Provider,
    [FromBody] OAuthCallbackRequest Request,
    IMediator Mediator,
    CancellationToken CancellationToken
);

/// <summary>
/// OAuth回调请求体
/// </summary>
/// <param name="Code">授权码</param>
/// <param name="State">状态参数，用于防止CSRF攻击</param>
/// <param name="RedirectUri">重定向URI（可选）</param>
internal record OAuthCallbackRequest(
    string Code,
    string State,
    string? RedirectUri = null
);
