using Humanizer;
using LexiCraft.Services.Practice.PracticeTasks.Models;
using LexiCraft.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.ComponentModel.DataAnnotations;

namespace LexiCraft.Services.Practice.PracticeTasks.Features.GeneratePracticeTasks;

public static class GeneratePracticeTasksEndpoint
{
    internal static RouteHandlerBuilder MapGeneratePracticeTasksEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("tasks/generate", Handle)
            .RequireAuthorization(PracticePermissions.Tasks.Generate)
            .WithName(nameof(GeneratePracticeTasks))
            .WithDisplayName(nameof(GeneratePracticeTasks).Humanize())
            .WithSummary("生成练习任务".Humanize())
            .WithDescription("根据指定的单词ID和练习类型生成练习任务".Humanize());

        async Task<GeneratePracticeTasksResponse> Handle(
            [AsParameters] GeneratePracticeTasksRequestParameters requestParameters)
        {
            var (mediator, request, cancellationToken) = requestParameters;
            
            var command = new GeneratePracticeTasksCommand
            {
                WordIds = request.WordIds,
                PracticeType = request.PracticeType,
                Count = request.Count
            };

            var result = await mediator.Send(command, cancellationToken);
            
            return result;
        }
    }
}

internal record GeneratePracticeTasksRequestParameters(
    IMediator Mediator,
    [FromBody] GeneratePracticeTasksRequest Request,
    CancellationToken CancellationToken = default
);

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