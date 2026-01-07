using Humanizer;
using BuildingBlocks.MongoDB.Performance;
using LexiCraft.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.ComponentModel.DataAnnotations;

namespace LexiCraft.Services.Practice.Shared.Features.GetPerformanceMetrics;

public static class GetPerformanceMetricsEndpoint
{
    internal static RouteHandlerBuilder MapGetPerformanceMetricsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapGet("metrics/performance", Handle)
            .RequireAuthorization(PracticePermissions.Performance.Query)
            .WithName(nameof(GetPerformanceMetrics))
            .WithDisplayName(nameof(GetPerformanceMetrics).Humanize())
            .WithSummary("获取性能指标".Humanize())
            .WithDescription("获取MongoDB性能指标用于监控".Humanize());

        async Task<PerformanceMetricsResponse> Handle(
            [AsParameters] GetPerformanceMetricsRequestParameters requestParameters)
        {
            var (mediator, periodMinutes, cancellationToken) = requestParameters;
            
            var query = new GetPerformanceMetricsQuery
            {
                PeriodMinutes = periodMinutes
            };

            var result = await mediator.Send(query, cancellationToken);
            
            return result;
        }
    }
}

internal record GetPerformanceMetricsRequestParameters(
    IMediator Mediator,
    [FromQuery] [Range(1, 60)] int PeriodMinutes = 5,
    CancellationToken CancellationToken = default
);