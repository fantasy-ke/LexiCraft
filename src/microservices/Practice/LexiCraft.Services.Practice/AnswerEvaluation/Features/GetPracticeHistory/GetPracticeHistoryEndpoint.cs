using BuildingBlocks.Authentication;
using LexiCraft.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace LexiCraft.Services.Practice.AnswerEvaluation.Features.GetPracticeHistory;

/// <summary>
/// 获取练习历史的API端点
/// </summary>
public static class GetPracticeHistoryEndpoint
{
    public static RouteHandlerBuilder MapGetPracticeHistoryEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/api/v{version:apiVersion}/practice/history", GetPracticeHistoryAsync)
            .WithName("GetPracticeHistory")
            .WithTags("练习历史")
            .WithSummary("获取用户练习历史")
            .WithDescription("""
                检索用户的练习历史，支持过滤和分页。
                
                **查询参数:**
                - `fromDate`: 从此日期开始过滤结果 (ISO 8601 格式)
                - `toDate`: 过滤结果到此日期 (ISO 8601 格式)
                - `wordIds`: 要过滤的单词ID列表，用逗号分隔
                - `pageIndex`: 从零开始的页面索引 (默认: 0)
                - `pageSize`: 每页项目数 (默认: 20, 最大: 100)
                
                **请求示例:**
                ```
                GET /api/v1/practice/history?fromDate=2024-01-01T00:00:00Z&pageSize=10&wordIds=1,2,3
                ```
                
                **响应示例:**
                ```json
                {
                  "success": true,
                  "answerRecords": [
                    {
                      "id": "507f1f77bcf86cd799439012",
                      "practiceTaskId": "507f1f77bcf86cd799439011",
                      "wordId": 1,
                      "userAnswer": "hello",
                      "expectedAnswer": "hello",
                      "isCorrect": true,
                      "score": 1.0,
                      "submittedAt": "2024-01-07T10:00:00Z",
                      "responseTime": "00:00:02.5000000"
                    }
                  ],
                  "totalCount": 25,
                  "pageIndex": 0,
                  "pageSize": 10,
                  "totalPages": 3
                }
                ```
                """)
            .RequireAuthorization(new ZAuthorizeAttribute(PracticePermissions.History.Query))
            .Produces<GetPracticeHistoryResponse>(StatusCodes.Status200OK, "application/json")
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/json")
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden, "application/json")
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/json");
    }

    private static async Task<IResult> GetPracticeHistoryAsync(
        [FromServices] IMediator mediator,
        ClaimsPrincipal user,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? wordIds = null,
        [FromQuery] [Range(0, int.MaxValue)] int pageIndex = 0,
        [FromQuery] [Range(1, 100)] int pageSize = 20)
    {
        try
        {
            // 从声明中获取用户ID
            var userIdClaim = user.FindFirst(ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Problem(
                    title: "无效用户",
                    detail: "令牌中未找到用户ID",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            // 如果提供了单词ID则解析
            List<long>? parsedWordIds = null;
            if (!string.IsNullOrEmpty(wordIds))
            {
                try
                {
                    parsedWordIds = wordIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(long.Parse)
                        .ToList();
                }
                catch (FormatException)
                {
                    return Results.Problem(
                        title: "无效单词ID",
                        detail: "单词ID必须是用逗号分隔的数字",
                        statusCode: StatusCodes.Status400BadRequest);
                }
            }

            var query = new GetPracticeHistoryQuery
            {
                UserId = userId,
                FromDate = fromDate,
                ToDate = toDate,
                WordIds = parsedWordIds,
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            var response = await mediator.Send(query);

            if (!response.Success)
            {
                return Results.Problem(
                    title: "历史检索失败",
                    detail: response.ErrorMessage,
                    statusCode: StatusCodes.Status400BadRequest);
            }

            return Results.Ok(response);
        }
        catch (Exception)
        {
            return Results.Problem(
                title: "内部服务器错误",
                detail: "检索练习历史时发生意外错误",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}