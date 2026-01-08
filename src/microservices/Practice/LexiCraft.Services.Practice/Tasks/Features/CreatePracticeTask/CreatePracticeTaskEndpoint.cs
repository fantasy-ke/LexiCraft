// 创建练习任务端点
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

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
    public static IEndpointRouteBuilder MapCreatePracticeTaskEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/tasks", async (
            [FromBody] CreatePracticeTaskCommand command,
            ISender sender) =>
        {
            var result = await sender.Send(command);
            return Results.Ok(result);
        })
        .Produces<CreatePracticeTaskResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithName("CreatePracticeTask")
        .WithOpenApi();

        return endpoints;
    }
}