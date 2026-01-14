using Humanizer;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using LexiCraft.Services.Identity.Shared.Dtos;

namespace LexiCraft.Services.Identity.Users.Features.RegisterUser;

public static class RegisterEndpoint
{
    internal static RouteHandlerBuilder MapRegisterEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("register", Handle)
            .WithName(nameof(RegisterUser))
            .WithDisplayName(nameof(RegisterUser).Humanize())
            .WithSummary("用户注册".Humanize())
            .WithDescription(nameof(RegisterUser).Humanize())
            .AllowAnonymous(); // 注册接口允许匿名访问

        async Task<TokenResponse> Handle(
            [AsParameters] RegisterRequestParameters requestParameters)
        {
            var (mediator, request, cancellationToken) = requestParameters;
            
            var result = await mediator.Send(request.Adapt<RegisterCommand>(), cancellationToken);
            
            return result;
        }
    }
}

/// <summary>
/// 用户注册请求参数
/// </summary>
/// <param name="Mediator"></param>
/// <param name="Request"></param>
/// <param name="CancellationToken"></param>
internal record RegisterRequestParameters(
    IMediator Mediator,
    [FromBody] RegisterUserRequest Request,
    CancellationToken CancellationToken
);

/// <summary>
/// 注册用户请求DTO
/// </summary>
/// <param name="UserAccount">用户账号</param>
/// <param name="Email">邮箱</param>
/// <param name="Password">密码</param>
/// <param name="CaptchaKey">验证码Key</param>
/// <param name="CaptchaCode">验证码</param>
public record RegisterUserRequest(
    string UserAccount,
    string Email,
    string Password,
    string CaptchaKey,
    string CaptchaCode);
