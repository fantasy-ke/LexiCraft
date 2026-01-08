// 提交答案端点

using LexiCraft.Services.Practice.Tasks.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Practice.Assessments.Features.SubmitAnswer;

/// <summary>
/// 提交答案端点扩展
/// </summary>
public static class SubmitAnswerEndpoint
{
    /// <summary>
    /// 映射提交答案端点
    /// </summary>
    /// <param name="endpoints">端点路由构建器</param>
    /// <returns>端点约束构建器</returns>
    public static IEndpointRouteBuilder MapSubmitAnswerEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/assessments/submit", async (
            [FromBody] SubmitAnswerCommand command,
            ISender sender) =>
        {
            var result = await sender.Send(command);
            return Results.Ok(result);
        })
        .Produces<AssessmentResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithName("SubmitAnswer");

        return endpoints;
    }
}