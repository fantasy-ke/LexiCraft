using Humanizer;
using LexiCraft.Services.Vocabulary.Words.Features.SearchWord;
using LexiCraft.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Vocabulary.UserStates.Features.GetWeakWords;

public static class GetWeakWordsEndpoint
{
    internal static RouteHandlerBuilder MapGetWeakWordsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("user-words/weak", Handle)
            .RequireAuthorization(VocabularyPermissions.UserStates.Query)
            .WithName(nameof(GetWeakWords))
            .WithDisplayName(nameof(GetWeakWords).Humanize())
            .WithSummary("获取薄弱词汇集合".Humanize())
            .WithDescription(nameof(GetWeakWords).Humanize());

        async Task<List<WordDto>> Handle(
            [AsParameters] GetWeakWordsRequestParameters requestParameters)
        {
            var (mediator, userId, cancellationToken) = requestParameters;

            var result = await mediator.Send(new GetWeakWordsQuery(userId), cancellationToken);

            return result;
        }
    }
}

internal record GetWeakWordsRequestParameters(
    IMediator Mediator,
    [FromQuery] Guid UserId,
    CancellationToken CancellationToken
);