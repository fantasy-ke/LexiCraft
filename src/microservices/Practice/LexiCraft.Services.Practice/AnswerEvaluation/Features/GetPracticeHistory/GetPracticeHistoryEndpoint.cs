using Humanizer;
using LexiCraft.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.ComponentModel.DataAnnotations;

namespace LexiCraft.Services.Practice.AnswerEvaluation.Features.GetPracticeHistory;

public static class GetPracticeHistoryEndpoint
{
    internal static RouteHandlerBuilder MapGetPracticeHistoryEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("history", Handle)
            .RequireAuthorization(PracticePermissions.History.Query)
            .WithName(nameof(GetPracticeHistory))
            .WithDisplayName(nameof(GetPracticeHistory).Humanize())
            .WithSummary("获取练习历史".Humanize())
            .WithDescription("检索用户的练习历史，支持过滤和分页".Humanize());

        async Task<GetPracticeHistoryResponse> Handle(
            [AsParameters] GetPracticeHistoryRequestParameters requestParameters)
        {
            var (mediator, fromDate, toDate, wordIds, pageIndex, pageSize, cancellationToken) = requestParameters;
            
            // Parse word IDs if provided
            List<long>? parsedWordIds = null;
            if (!string.IsNullOrEmpty(wordIds))
            {
                parsedWordIds = wordIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(long.Parse)
                    .ToList();
            }

            var query = new GetPracticeHistoryQuery
            {
                FromDate = fromDate,
                ToDate = toDate,
                WordIds = parsedWordIds,
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            var result = await mediator.Send(query, cancellationToken);
            
            return result;
        }
    }
}

internal record GetPracticeHistoryRequestParameters(
    IMediator Mediator,
    [FromQuery] DateTime? FromDate = null,
    [FromQuery] DateTime? ToDate = null,
    [FromQuery] string? WordIds = null,
    [FromQuery] [Range(0, int.MaxValue)] int PageIndex = 0,
    [FromQuery] [Range(1, 100)] int PageSize = 20,
    CancellationToken CancellationToken = default
);