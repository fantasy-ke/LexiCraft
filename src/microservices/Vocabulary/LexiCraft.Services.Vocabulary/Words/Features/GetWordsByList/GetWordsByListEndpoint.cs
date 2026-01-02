using Humanizer;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using LexiCraft.Services.Vocabulary.Words.Features.SearchWord;

namespace LexiCraft.Services.Vocabulary.Words.Features.GetWordsByList;

public static class GetWordsByListEndpoint
{
    internal static RouteHandlerBuilder MapGetWordsByListEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("word-lists/{id}/words", Handle)
            .WithName(nameof(GetWordsByList))
            .WithDisplayName(nameof(GetWordsByList).Humanize())
            .WithSummary("获取词库单词".Humanize())
            .WithDescription(nameof(GetWordsByList).Humanize());

        async Task<List<WordDto>> Handle(
            [AsParameters] GetWordsByListRequestParameters requestParameters)
        {
            var (mediator, id, cancellationToken) = requestParameters;
            
            var result = await mediator.Send(new GetWordsByListQuery(id), cancellationToken);
            
            return result;
        }
    }
}

internal record GetWordsByListRequestParameters(
    IMediator Mediator,
    [FromRoute] long Id,
    CancellationToken CancellationToken
);
