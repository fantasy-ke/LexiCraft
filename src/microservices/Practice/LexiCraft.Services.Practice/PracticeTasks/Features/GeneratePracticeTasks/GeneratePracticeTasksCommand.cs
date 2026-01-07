using FluentValidation;
using LexiCraft.Services.Practice.PracticeTasks.Models;
using MediatR;

namespace LexiCraft.Services.Practice.PracticeTasks.Features.GeneratePracticeTasks;

/// <summary>
/// Command to generate practice tasks for a user
/// </summary>
public class GeneratePracticeTasksCommand : IRequest<GeneratePracticeTasksResponse>
{
    /// <summary>
    /// The ID of the user requesting practice tasks
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// List of word IDs to generate practice tasks for
    /// </summary>
    public List<long> WordIds { get; set; } = new();

    /// <summary>
    /// Type of practice exercise to generate
    /// </summary>
    public PracticeType PracticeType { get; set; }

    /// <summary>
    /// Number of tasks to generate (optional, defaults to all words)
    /// </summary>
    public int? Count { get; set; }
}

/// <summary>
/// Response containing the generated practice tasks
/// </summary>
public class GeneratePracticeTasksResponse
{
    /// <summary>
    /// List of generated practice tasks
    /// </summary>
    public List<PracticeTask> Tasks { get; set; } = new();

    /// <summary>
    /// Total number of tasks generated
    /// </summary>
    public int TotalGenerated { get; set; }

    /// <summary>
    /// Success indicator
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if generation failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Validator for GeneratePracticeTasksCommand
/// </summary>
public class GeneratePracticeTasksCommandValidator : AbstractValidator<GeneratePracticeTasksCommand>
{
    public GeneratePracticeTasksCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.WordIds)
            .NotEmpty()
            .WithMessage("At least one word ID is required")
            .Must(wordIds => wordIds.All(id => id > 0))
            .WithMessage("All word IDs must be positive numbers");

        RuleFor(x => x.PracticeType)
            .IsInEnum()
            .WithMessage("Practice type must be a valid enum value");

        RuleFor(x => x.Count)
            .GreaterThan(0)
            .When(x => x.Count.HasValue)
            .WithMessage("Count must be greater than 0 when specified");
    }
}