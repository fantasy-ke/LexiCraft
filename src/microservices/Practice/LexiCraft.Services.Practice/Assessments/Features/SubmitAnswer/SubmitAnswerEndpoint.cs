// 提交答案端点

using Humanizer;
using LexiCraft.Services.Practice.Tasks.Models;
using LexiCraft.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Practice.Assessments.Features.SubmitAnswer;

/// <summary>
///     提交答案端点扩展
/// </summary>
public static class SubmitAnswerEndpoint
{
    /// <summary>
    ///     映射提交答案端点
    /// </summary>
    /// <param name="endpoints">端点路由构建器</param>
    /// <returns>端点约束构建器</returns>
    internal static RouteHandlerBuilder MapSubmitAnswerEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/assessments/submit", Handle)
            .RequireAuthorization(PracticePermissions.Assessments.Submit)
            .WithName(nameof(SubmitAnswer))
            .WithDisplayName(nameof(SubmitAnswer).Humanize())
            .WithSummary("提交答案".Humanize())
            .WithDescription("提交练习任务的答案并获得评估结果".Humanize());

        async Task<AssessmentResult> Handle(
            [AsParameters] SubmitAnswerRequestParameters requestParameters)
        {
            var (mediator, command, cancellationToken) = requestParameters;

            var result = await mediator.Send(command, cancellationToken);

            return result;
        }
    }
}

internal record SubmitAnswerRequestParameters(
    IMediator Mediator,
    [FromBody] SubmitAnswerCommand Command,
    CancellationToken CancellationToken = default
);