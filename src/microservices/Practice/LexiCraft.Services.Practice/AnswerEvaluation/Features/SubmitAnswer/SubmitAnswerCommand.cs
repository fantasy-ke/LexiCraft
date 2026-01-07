using FluentValidation;
using LexiCraft.Services.Practice.AnswerEvaluation.Models;
using LexiCraft.Services.Practice.MistakeAnalysis.Models;
using MediatR;

namespace LexiCraft.Services.Practice.AnswerEvaluation.Features.SubmitAnswer;

/// <summary>
/// Command to submit and evaluate an answer to a practice task
/// </summary>
public class SubmitAnswerCommand : IRequest<SubmitAnswerResponse>
{
    /// <summary>
    /// The ID of the practice task being answered
    /// </summary>
    public string PracticeTaskId { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the user submitting the answer
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The answer provided by the user
    /// </summary>
    public string UserAnswer { get; set; } = string.Empty;

    /// <summary>
    /// Time taken to respond (in milliseconds)
    /// </summary>
    public long ResponseTimeMs { get; set; }
}

/// <summary>
/// Response containing the evaluation results
/// </summary>
public class SubmitAnswerResponse
{
    /// <summary>
    /// The created answer record
    /// </summary>
    public AnswerRecord AnswerRecord { get; set; } = new();

    /// <summary>
    /// Mistake item created if answer was incorrect
    /// </summary>
    public MistakeItem? MistakeItem { get; set; }

    /// <summary>
    /// Whether the answer was correct
    /// </summary>
    public bool IsCorrect { get; set; }

    /// <summary>
    /// Accuracy score (0.0 to 1.0)
    /// </summary>
    public double Accuracy { get; set; }

    /// <summary>
    /// Feedback message for the user
    /// </summary>
    public string Feedback { get; set; } = string.Empty;

    /// <summary>
    /// Success indicator
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if submission failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Validator for SubmitAnswerCommand
/// </summary>
public class SubmitAnswerCommandValidator : AbstractValidator<SubmitAnswerCommand>
{
    public SubmitAnswerCommandValidator()
    {
        RuleFor(x => x.PracticeTaskId)
            .NotEmpty()
            .WithMessage("Practice task ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.UserAnswer)
            .NotNull()
            .WithMessage("User answer is required (can be empty string for no answer)");

        RuleFor(x => x.ResponseTimeMs)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Response time must be non-negative");
    }
}