using Humanizer;
using LexiCraft.Services.Identity.Shared.Dtos;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

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
            var (mediator, command, cancellationToken) = requestParameters;

            var result = await mediator.Send(command, cancellationToken);

            return result;
        }
    }
}

/// <summary>
///     用户注册请求参数
/// </summary>
/// <param name="Mediator"></param>
/// <param name="Request"></param>
/// <param name="CancellationToken"></param>
internal record RegisterRequestParameters(
    IMediator Mediator,
    [FromBody] RegisterCommand Command,
    CancellationToken CancellationToken
);