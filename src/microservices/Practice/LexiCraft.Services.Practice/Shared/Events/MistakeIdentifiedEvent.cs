using BuildingBlocks.EventBus.Abstractions;
using LexiCraft.Services.Practice.AnswerEvaluation.Models;
using LexiCraft.Services.Practice.MistakeAnalysis.Models;

namespace LexiCraft.Services.Practice.Shared.Events;

/// <summary>
/// Event published when a mistake is identified during practice
/// </summary>
public record MistakeIdentifiedEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public long WordId { get; init; }
    public string WordSpelling { get; init; } = string.Empty;
    public string UserAnswer { get; init; } = string.Empty;
    public string ExpectedAnswer { get; init; } = string.Empty;
    public ErrorType ErrorType { get; init; }
    public List<ErrorDetail> ErrorDetails { get; init; } = new();
    public DateTime OccurredAt { get; init; }
    public string PracticeType { get; init; } = string.Empty;
}