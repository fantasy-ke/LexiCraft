using BuildingBlocks.EventBus.Abstractions;

namespace LexiCraft.Services.Practice.Shared.Events;

/// <summary>
/// Event published when a practice session is completed
/// </summary>
public record PracticeCompletionEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public string PracticeTaskId { get; init; } = string.Empty;
    public long WordId { get; init; }
    public string WordSpelling { get; init; } = string.Empty;
    public bool IsCorrect { get; init; }
    public double Score { get; init; }
    public TimeSpan ResponseTime { get; init; }
    public DateTime CompletedAt { get; init; }
    public string PracticeType { get; init; } = string.Empty;
}