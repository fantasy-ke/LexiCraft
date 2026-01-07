using FluentValidation;
using LexiCraft.Services.Practice.AnswerEvaluation.Models;
using MediatR;

namespace LexiCraft.Services.Practice.AnswerEvaluation.Features.GetPracticeHistory;

/// <summary>
/// Query to retrieve user's practice history with filtering and pagination
/// </summary>
public class GetPracticeHistoryQuery : IRequest<GetPracticeHistoryResponse>
{
    /// <summary>
    /// The ID of the user whose history to retrieve
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Start date for filtering (optional)
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// End date for filtering (optional)
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// List of word IDs to filter by (optional)
    /// </summary>
    public List<long>? WordIds { get; set; }

    /// <summary>
    /// Page index for pagination (0-based)
    /// </summary>
    public int PageIndex { get; set; } = 0;

    /// <summary>
    /// Page size for pagination
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Response containing the practice history
/// </summary>
public class GetPracticeHistoryResponse
{
    /// <summary>
    /// List of answer records
    /// </summary>
    public List<AnswerRecord> AnswerRecords { get; set; } = new();

    /// <summary>
    /// Total number of records matching the filter
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page index
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// Page size used
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there are more pages
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Whether there are previous pages
    /// </summary>
    public bool HasPreviousPage { get; set; }

    /// <summary>
    /// Success indicator
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if retrieval failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Validator for GetPracticeHistoryQuery
/// </summary>
public class GetPracticeHistoryQueryValidator : AbstractValidator<GetPracticeHistoryQuery>
{
    public GetPracticeHistoryQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.PageIndex)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Page index must be non-negative");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100)
            .WithMessage("Page size must be between 1 and 100");

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(x => x.ToDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("From date must be less than or equal to to date");

        RuleFor(x => x.WordIds)
            .Must(wordIds => wordIds == null || wordIds.All(id => id > 0))
            .WithMessage("All word IDs must be positive numbers when specified");
    }
}