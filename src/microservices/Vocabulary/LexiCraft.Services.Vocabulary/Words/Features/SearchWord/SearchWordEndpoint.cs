using Humanizer;
using LexiCraft.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Vocabulary.Words.Features.SearchWord;

public static class SearchWordEndpoint
{
    internal static RouteHandlerBuilder MapSearchWordEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("words", Handle)
            .RequireAuthorization(VocabularyPermissions.Words.Query)
            .WithName(nameof(SearchWord))
            .WithDisplayName(nameof(SearchWord).Humanize())
            .WithSummary("搜索单词".Humanize())
            .WithDescription(nameof(SearchWord).Humanize());

        async Task<List<WordDto>> Handle(
            [AsParameters] SearchWordRequestParameters requestParameters)
        {
            var (mediator, keyword, cancellationToken) = requestParameters;

            var result = await mediator.Send(new SearchWordQuery(keyword), cancellationToken);

            return result;
        }
    }
}

internal record SearchWordRequestParameters(
    IMediator Mediator,
    [FromQuery] string Keyword,
    CancellationToken CancellationToken
);