using BuildingBlocks.EventBus.Abstractions;

namespace LexiCraft.Services.Practice.Shared.Events;

/// <summary>
/// Event published with performance data for statistics service
/// </summary>
public record PerformanceDataEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public string SessionId { get; init; } = string.Empty;
    public List<TaskPerformance> TaskPerformances { get; init; } = new();
    public DateTime SessionStartTime { get; init; }
    public DateTime SessionEndTime { get; init; }
    public double OverallAccuracy { get; init; }
    public int TotalTasks { get; init; }
    public int CorrectAnswers { get; init; }
    public int IncorrectAnswers { get; init; }
    public TimeSpan TotalResponseTime { get; init; }
    public TimeSpan AverageResponseTime { get; init; }
}

/// <summary>
/// Performance data for a single task
/// </summary>
public record TaskPerformance
{
    public string TaskId { get; init; } = string.Empty;
    public long WordId { get; init; }
    public string WordSpelling { get; init; } = string.Empty;
    public bool IsCorrect { get; init; }
    public double Score { get; init; }
    public TimeSpan ResponseTime { get; init; }
    public string PracticeType { get; init; } = string.Empty;
    public DateTime CompletedAt { get; init; }
}