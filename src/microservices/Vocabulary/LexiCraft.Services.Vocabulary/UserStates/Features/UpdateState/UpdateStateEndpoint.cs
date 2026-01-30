using Humanizer;
using LexiCraft.Services.Vocabulary.UserStates.Models.Enum;
using LexiCraft.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using LexiCraft.Shared.Models;
using LexiCraft.Services.Vocabulary.Words.Models;

namespace LexiCraft.Services.Vocabulary.UserStates.Features.UpdateState;

public static class UpdateStateEndpoint
{
    internal static RouteHandlerBuilder MapUpdateStateEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("user-words/state", Handle)
            .RequireAuthorization(VocabularyPermissions.UserStates.Update)
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
    UserId UserId,
    WordId WordId,
    WordState State,
    bool? IsInWordBook,
    int? MasteryScore);

internal record UpdateStateRequestParameters(
    IMediator Mediator,
    [FromBody] UpdateStateRequest Request,
    CancellationToken CancellationToken
);