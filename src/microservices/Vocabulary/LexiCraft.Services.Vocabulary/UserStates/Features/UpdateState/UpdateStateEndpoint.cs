using Humanizer;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using LexiCraft.Services.Vocabulary.UserStates.Models.Enum;

namespace LexiCraft.Services.Vocabulary.UserStates.Features.UpdateState;

public static class UpdateStateEndpoint
{
    internal static RouteHandlerBuilder MapUpdateStateEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("user-words/state", Handle)
            .WithName(nameof(UpdateState))
            .WithDisplayName(nameof(UpdateState).Humanize())
            .WithSummary("更新单词掌握状态".Humanize())
            .WithDescription(nameof(UpdateState).Humanize());

        async Task<bool> Handle(
            [AsParameters] UpdateStateRequestParameters requestParameters)
        {
            var (mediator, request, cancellationToken) = requestParameters;
            
            var result = await mediator.Send(new UpdateStateCommand(
                request.UserId,
                request.WordId,
                request.State,
                request.IsInWordBook,
                request.MasteryScore), cancellationToken);
            
            return result;
        }
    }
}

public record UpdateStateRequest(
    Guid UserId,
    long WordId,
    WordState State,
    bool? IsInWordBook,
    int? MasteryScore);

internal record UpdateStateRequestParameters(
    IMediator Mediator,
    [FromBody] UpdateStateRequest Request,
    CancellationToken CancellationToken
);
