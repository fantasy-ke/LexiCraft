using BuildingBlocks.Authentication;
using LexiCraft.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace LexiCraft.Services.Practice.AnswerEvaluation.Features.SubmitAnswer;

/// <summary>
/// 提交练习任务答案的API端点
/// </summary>
public static class SubmitAnswerEndpoint
{
    public static RouteHandlerBuilder MapSubmitAnswerEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/api/v{version:apiVersion}/practice/answers/submit", SubmitAnswerAsync)
            .WithName("SubmitAnswer")
            .WithTags("Practice Answers")
            .WithSummary("Submit an answer to a practice task")
            .WithDescription("""
                Submits and evaluates an answer to a practice task, providing immediate feedback and scoring.
                
                The system will:
                1. Validate the answer against the expected response
                2. Calculate accuracy and provide a score
                3. Classify any errors (spelling vs complete errors)
                4. Generate immediate feedback
                5. Create mistake items for incorrect answers
                6. Publish events for integration with other services
                
                **Example Request:**
                ```json
                {
                  "practiceTaskId": "507f1f77bcf86cd799439011",
                  "userAnswer": "hello",
                  "responseTimeMs": 2500
                }
                ```
                
                **Example Response:**
                ```json
                {
                  "success": true,
                  "answerRecord": {
                    "id": "507f1f77bcf86cd799439012",
                    "isCorrect": true,
                    "score": 1.0,
                    "accuracy": 1.0,
                    "feedback": "Excellent! Perfect answer.",
                    "responseTime": "00:00:02.5000000"
                  },
                  "mistakeItems": []
                }
                ```
                """)
            .RequireAuthorization(new ZAuthorizeAttribute(PracticePermissions.Answers.Submit))
            .Produces<SubmitAnswerResponse>(StatusCodes.Status200OK, "application/json")
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest, "application/json")
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/json")
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden, "application/json")
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound, "application/json")
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/json");
    }

    private static async Task<IResult> SubmitAnswerAsync(
        [FromBody] SubmitAnswerRequest request,
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

            var command = new SubmitAnswerCommand
            {
                PracticeTaskId = request.PracticeTaskId,
                UserId = userId,
                UserAnswer = request.UserAnswer,
                ResponseTimeMs = request.ResponseTimeMs
            };

            var response = await mediator.Send(command);

            if (!response.Success)
            {
                var statusCode = response.ErrorMessage?.Contains("not found") == true
                    ? StatusCodes.Status404NotFound
                    : response.ErrorMessage?.Contains("not authorized") == true
                        ? StatusCodes.Status401Unauthorized
                        : StatusCodes.Status400BadRequest;

                return Results.Problem(
                    title: "Answer Submission Failed",
                    detail: response.ErrorMessage,
                    statusCode: statusCode);
            }

            return Results.Ok(response);
        }
        catch (Exception)
        {
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while submitting your answer",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}

/// <summary>
/// Request model for submitting an answer
/// </summary>
public class SubmitAnswerRequest
{
    /// <summary>
    /// The ID of the practice task being answered
    /// </summary>
    /// <example>507f1f77bcf86cd799439011</example>
    [Required]
    [StringLength(24, MinimumLength = 24, ErrorMessage = "Practice task ID must be a valid MongoDB ObjectId")]
    public string PracticeTaskId { get; set; } = string.Empty;

    /// <summary>
    /// The answer provided by the user
    /// </summary>
    /// <example>hello</example>
    [Required]
    [StringLength(500, ErrorMessage = "Answer cannot exceed 500 characters")]
    public string UserAnswer { get; set; } = string.Empty;

    /// <summary>
    /// Time taken to respond (in milliseconds)
    /// </summary>
    /// <example>2500</example>
    [Range(0, long.MaxValue, ErrorMessage = "Response time must be a positive number")]
    public long ResponseTimeMs { get; set; }
}