// 完成练习端点
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

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
    public static IEndpointRouteBuilder MapCompletePracticeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/tasks/{taskid}/complete", async (
            [FromRoute] string taskid,
            [FromBody] CompletePracticeCommand command,
            ISender sender) =>
        {
            var result = await sender.Send(command with { TaskId = taskid });
            return Results.Ok(result);
        })
        .Produces<bool>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithName("CompletePractice")
        .WithOpenApi();

        return endpoints;
    }
}