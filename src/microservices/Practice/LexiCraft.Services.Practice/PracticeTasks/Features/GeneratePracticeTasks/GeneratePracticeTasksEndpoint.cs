using BuildingBlocks.Authentication;
using LexiCraft.Services.Practice.PracticeTasks.Models;
using LexiCraft.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace LexiCraft.Services.Practice.PracticeTasks.Features.GeneratePracticeTasks;

/// <summary>
/// 生成练习任务的API端点
/// </summary>
public static class GeneratePracticeTasksEndpoint
{
    public static RouteHandlerBuilder MapGeneratePracticeTasksEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/api/v{version:apiVersion}/practice/tasks/generate", GeneratePracticeTasksAsync)
            .WithName("GeneratePracticeTasks")
            .WithTags("Practice Tasks")
            .WithSummary("Generate practice tasks for vocabulary learning")
            .WithDescription("""
                Generates practice tasks based on specified word IDs and practice type.
                
                **Practice Types:**
                - `Dictation` (0): Listen to pronunciation and write the word (听音写词)
                - `DefinitionToWord` (1): Read definition and write the word (看义写词)
                
                **Example Request:**
                ```json
                {
                  "wordIds": [1, 2, 3, 4, 5],
                  "practiceType": 0,
                  "count": 3
                }
                ```
                
                **Example Response:**
                ```json
                {
                  "success": true,
                  "tasks": [
                    {
                      "id": "507f1f77bcf86cd799439011",
                      "wordId": 1,
                      "wordSpelling": "hello",
                      "wordDefinition": "a greeting",
                      "practiceType": 0,
                      "expectedAnswer": "hello",
                      "status": 0,
                      "createdAt": "2024-01-07T10:00:00Z"
                    }
                  ],
                  "totalGenerated": 3
                }
                ```
                """)
            .RequireAuthorization(new ZAuthorizeAttribute(PracticePermissions.Tasks.Generate))
            .Produces<GeneratePracticeTasksResponse>(StatusCodes.Status200OK, "application/json")
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/json")
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden, "application/json")
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/json");
    }

    private static async Task<IResult> GeneratePracticeTasksAsync(
        [FromBody] GeneratePracticeTasksRequest request,
        [FromServices] IMediator mediator,
        ClaimsPrincipal user)
    {
        try
        {
            // Get user ID from claims
            var userIdClaim = user.FindFirst(ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Problem(
                    title: "Invalid User",
                    detail: "User ID not found in token",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var command = new GeneratePracticeTasksCommand
            {
                UserId = userId,
                WordIds = request.WordIds,
                PracticeType = request.PracticeType,
                Count = request.Count
            };

            var response = await mediator.Send(command);

            if (!response.Success)
            {
                return Results.Problem(
                    title: "Task Generation Failed",
                    detail: response.ErrorMessage,
                    statusCode: StatusCodes.Status400BadRequest);
            }

            return Results.Ok(response);
        }
        catch (Exception)
        {
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while generating practice tasks",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}

/// <summary>
/// Request model for generating practice tasks
/// </summary>
public class GeneratePracticeTasksRequest
{
    /// <summary>
    /// List of word IDs to generate practice tasks for
    /// </summary>
    /// <example>[1, 2, 3, 4, 5]</example>
    [Required]
    [MinLength(1, ErrorMessage = "At least one word ID is required")]
    public List<long> WordIds { get; set; } = new();

    /// <summary>
    /// Type of practice exercise to generate
    /// </summary>
    /// <example>0</example>
    [Required]
    public PracticeType PracticeType { get; set; }

    /// <summary>
    /// Number of tasks to generate (optional, defaults to all words)
    /// </summary>
    /// <example>3</example>
    [Range(1, 100, ErrorMessage = "Count must be between 1 and 100")]
    public int? Count { get; set; }
}