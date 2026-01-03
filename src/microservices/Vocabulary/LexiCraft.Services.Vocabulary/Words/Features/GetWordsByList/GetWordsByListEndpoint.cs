using Humanizer;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using LexiCraft.Shared.Permissions;

namespace LexiCraft.Services.Vocabulary.Words.Features.GetWordsByList;

public static class GetWordsByListEndpoint
{
    internal static RouteHandlerBuilder MapGetWordsByListEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("word-lists/{id}/words", Handle)
            .RequireAuthorization(VocabularyPermissions.Words.Query)
            .WithName(nameof(GetWordsByList))
            .WithDisplayName(nameof(GetWordsByList).Humanize())
            .WithSummary("获取词库单词 (分页/乱序)".Humanize())
            .WithDescription("获取指定词库下的单词。若需乱序且不重复分页，请传入相同的 Seed 值".Humanize());

        async Task<PagedWordResult> Handle(
            [AsParameters] GetWordsByListRequestParameters requestParameters)
        {
            var (mediator, id, pageIndex, pageSize, seed, cancellationToken) = requestParameters;
            
            var result = await mediator.Send(new GetWordsByListQuery(id, pageIndex, pageSize, seed), cancellationToken);
            
            return result;
        }
    }
}

internal record GetWordsByListRequestParameters(
    IMediator Mediator,
    [FromRoute] long Id,
    [FromQuery] int PageIndex = 1,
    [FromQuery] int PageSize = 20,
    [FromQuery] string? Seed = null,
    CancellationToken CancellationToken = default
);
