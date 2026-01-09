// 创建练习任务端点
using Humanizer;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using LexiCraft.Shared.Permissions;

namespace LexiCraft.Services.Practice.Tasks.Features.CreatePracticeTask;

/// <summary>
/// 创建练习任务端点扩展
/// </summary>
public static class CreatePracticeTaskEndpoint
{
    /// <summary>
    /// 映射创建练习任务端点
    /// </summary>
    /// <param name="endpoints">端点路由构建器</param>
    /// <returns>端点约束构建器</returns>
    internal static RouteHandlerBuilder MapCreatePracticeTaskEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/tasks", Handle)
            .RequireAuthorization(PracticePermissions.Tasks.Create)
            .WithName(nameof(CreatePracticeTask))
            .WithDisplayName(nameof(CreatePracticeTask).Humanize())
            .WithSummary("创建练习任务".Humanize())
            .WithDescription("为用户创建一个新的练习任务".Humanize());

        async Task<CreatePracticeTaskResult> Handle(
            [AsParameters] CreatePracticeTaskRequestParameters requestParameters)
        {
            var (mediator, command, cancellationToken) = requestParameters;
            
            var result = await mediator.Send(command, cancellationToken);
            
            return result;
        }
    }
}

internal record CreatePracticeTaskRequestParameters(
    IMediator Mediator,
    [FromBody] CreatePracticeTaskCommand Command,
    CancellationToken CancellationToken = default
);