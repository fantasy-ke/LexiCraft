using Humanizer;
using Mapster;
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
            [AsParameters] LoginRequestParameters requestParameters)
        {
            var (mediator, request, cancellationToken) = requestParameters;
            
            var result = await mediator.Send(request.Adapt<LoginCommand>(), cancellationToken);
            
            return result.Adapt<TokenResponse>();
        }
    }
}

/// <summary>
/// 用户登录请求参数
/// </summary>
/// <param name="Mediator"></param>
/// <param name="Request"></param>
/// <param name="CancellationToken"></param>
internal record LoginRequestParameters(
    IMediator Mediator,
    [FromBody] LoginUserRequest Request,
    CancellationToken CancellationToken
);

/// <summary>
/// 登录用户请求DTO
/// </summary>
/// <param name="UserAccount">用户账号</param>
/// <param name="Password">密码</param>
public record LoginUserRequest(
    string UserAccount,
    string Password);

/// <summary>
/// 登录令牌响应
/// </summary>
/// <param name="Token">访问令牌</param>
/// <param name="RefreshToken">刷新令牌</param>
public record TokenResponse(
    string Token,
    string RefreshToken);