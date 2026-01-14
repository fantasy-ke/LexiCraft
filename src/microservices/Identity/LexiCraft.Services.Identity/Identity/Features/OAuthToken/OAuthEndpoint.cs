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
            .MapPost("oauth/{provider}/callback", Handle)
            .WithName(nameof(OAuthToken))
            .WithDisplayName(nameof(OAuthToken).Humanize())
            .WithSummary("第三方OAuth回调接口")
            .WithDescription("处理OAuth提供商的授权回调，返回访问令牌和刷新令牌")
            .AllowAnonymous(); // OAuth回调接口允许匿名访问

        async Task<OAuthTokenResponse> Handle(
            [AsParameters] OAuthCallbackRequestParameters requestParameters)
        {
            var (provider, request, mediator, cancellationToken) = requestParameters;
            
            var command = new OAuthCommand(provider, request.Code, request.RedirectUri);
            var token = await mediator.Send(command, cancellationToken);
            
            // 返回标准的OAuth令牌响应
            return new OAuthTokenResponse(
                Token: token,
                RefreshToken: null, // TODO: 实现刷新令牌逻辑
                ExpiresIn: 3600, // 1小时，应该从JWT配置中读取
                TokenType: "Bearer"
            );
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

/// <summary>
/// OAuth令牌响应
/// </summary>
/// <param name="Token">访问令牌</param>
/// <param name="RefreshToken">刷新令牌（可选）</param>
/// <param name="ExpiresIn">令牌过期时间（秒）</param>
/// <param name="TokenType">令牌类型，通常为Bearer</param>
internal record OAuthTokenResponse(
    string Token,
    string? RefreshToken,
    int ExpiresIn,
    string TokenType
);
