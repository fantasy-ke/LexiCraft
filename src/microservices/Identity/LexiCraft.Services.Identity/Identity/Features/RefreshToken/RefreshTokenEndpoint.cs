using Humanizer;
using LexiCraft.Services.Identity.Shared.Dtos;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Identity.Identity.Features.RefreshToken;

public static class RefreshTokenEndpoint
{
    internal static RouteHandlerBuilder MapRefreshTokenEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("refresh-token", Handle)
            .WithName(nameof(RefreshToken))
            .WithDisplayName(nameof(RefreshToken).Humanize())
            .WithSummary("刷新访问令牌".Humanize())
            .WithDescription(nameof(RefreshToken).Humanize())
            .AllowAnonymous();

        async Task<TokenResponse> Handle(
            [AsParameters] RefreshTokenRequestParameters requestParameters)
        {
            var (mediator, command, cancellationToken) = requestParameters;

            var result = await mediator.Send(command, cancellationToken);

            return result;
        }
    }
}

internal record RefreshTokenRequestParameters(
    IMediator Mediator,
    [FromBody] RefreshTokenCommand Command,
    CancellationToken CancellationToken
);