// 完成练习端点
using Humanizer;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using LexiCraft.Shared.Permissions;

namespace LexiCraft.Services.Practice.Tasks.Features.CompletePractice;

/// <summary>
/// 完成练习端点扩展
/// </summary>
public static class CompletePracticeEndpoint
{
    /// <summary>
    /// 映射完成练习端点
    /// </summary>
    /// <param name="endpoints">端点路由构建器</param>
    /// <returns>端点约束构建器</returns>
    internal static RouteHandlerBuilder MapCompletePracticeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPut("/tasks/{taskId}/complete", Handle)
            .RequireAuthorization(PracticePermissions.Tasks.Complete)
            .WithName(nameof(CompletePractice))
            .WithDisplayName(nameof(CompletePractice).Humanize())
            .WithSummary("完成练习任务".Humanize())
            .WithDescription("完成指定的练习任务".Humanize());

        async Task<bool> Handle(
            [AsParameters] CompletePracticeRequestParameters requestParameters)
        {
            var (mediator, taskId, command, cancellationToken) = requestParameters;
            
            var result = await mediator.Send(command with { TaskId = taskId }, cancellationToken);
            
            return result;
        }
    }
}

internal record CompletePracticeRequestParameters(
    IMediator Mediator,
    [FromRoute] string TaskId,
    [FromBody] CompletePracticeCommand Command,
    CancellationToken CancellationToken = default
);