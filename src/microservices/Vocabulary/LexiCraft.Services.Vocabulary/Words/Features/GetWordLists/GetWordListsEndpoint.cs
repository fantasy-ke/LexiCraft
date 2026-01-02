using Humanizer;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Vocabulary.Words.Features.GetWordLists;

public static class GetWordListsEndpoint
{
    internal static RouteHandlerBuilder MapGetWordListsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("word-lists", Handle)
            .WithName(nameof(GetWordLists))
            .WithDisplayName(nameof(GetWordLists).Humanize())
            .WithSummary("获取词库列表 (非分页)".Humanize())
            .WithDescription("获取词库列表，可按分类过滤".Humanize());

        async Task<List<WordListDto>> Handle(
            [AsParameters] GetWordListsRequestParameters requestParameters)
        {
            var (mediator, category, cancellationToken) = requestParameters;
            
            var result = await mediator.Send(new GetWordListsQuery(category), cancellationToken);
            
            return result;
        }
    }
}

internal record GetWordListsRequestParameters(
    IMediator Mediator,
    [FromQuery] string? Category = null,
    CancellationToken CancellationToken = default
);
