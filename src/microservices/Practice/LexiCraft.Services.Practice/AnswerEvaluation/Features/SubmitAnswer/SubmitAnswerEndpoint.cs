using Humanizer;
using LexiCraft.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.ComponentModel.DataAnnotations;

namespace LexiCraft.Services.Practice.AnswerEvaluation.Features.SubmitAnswer;

public static class SubmitAnswerEndpoint
{
    internal static RouteHandlerBuilder MapSubmitAnswerEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("answers/submit", Handle)
            .RequireAuthorization(PracticePermissions.Answers.Submit)
            .WithName(nameof(SubmitAnswer))
            .WithDisplayName(nameof(SubmitAnswer).Humanize())
            .WithSummary("提交练习答案".Humanize())
            .WithDescription("提交并评估练习任务答案，提供即时反馈和评分".Humanize());

        async Task<SubmitAnswerResponse> Handle(
            [AsParameters] SubmitAnswerRequestParameters requestParameters)
        {
            var (mediator, request, cancellationToken) = requestParameters;
            
            var command = new SubmitAnswerCommand
            {
                PracticeTaskId = request.PracticeTaskId,
                UserAnswer = request.UserAnswer,
                ResponseTimeMs = request.ResponseTimeMs
            };

            var result = await mediator.Send(command, cancellationToken);
            
            return result;
        }
    }
}

internal record SubmitAnswerRequestParameters(
    IMediator Mediator,
    [FromBody] SubmitAnswerRequest Request,
    CancellationToken CancellationToken = default
);

/// <summary>
/// Request model for submitting an answer
/// </summary>
public class SubmitAnswerRequest
{
    /// <summary>
    /// ID of the practice task being answered
    /// </summary>
    [Required]
    public string PracticeTaskId { get; set; } = string.Empty;

    /// <summary>
    /// User's answer to the practice task
    /// </summary>
    [Required]
    public string UserAnswer { get; set; } = string.Empty;

    /// <summary>
    /// Time taken to respond in milliseconds
    /// </summary>
    [Range(0, int.MaxValue)]
    public int ResponseTimeMs { get; set; }
}