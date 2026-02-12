using Humanizer;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Identity.Identity.Features.ReplayEvents;

public static class ReplayEventsEndpoint
{
    internal static RouteHandlerBuilder MapReplayEventsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("events/replay", Handle)
            .WithName(nameof(ReplayEvents))
            .WithDisplayName(nameof(ReplayEvents).Humanize())
            .WithSummary("重发/回放事件流".Humanize())
            .WithDescription("根据 StreamId 重发存储在 Redis 中的事件".Humanize())
            ; // 通常重发事件需要管理员权限，这里先要求授权

        async Task<IResult> Handle(
            [AsParameters] ReplayEventsRequestParameters requestParameters)
        {
            var (mediator, command, cancellationToken) = requestParameters;

            await mediator.Send(command, cancellationToken);

            return Results.Ok("事件回放请求已处理");
        }
    }
}

internal record ReplayEventsRequestParameters(
    IMediator Mediator,
    [FromBody] ReplayEventsCommand Command,
    CancellationToken CancellationToken
);
