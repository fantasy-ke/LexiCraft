using BuildingBlocks.EventBus.Abstractions;

namespace LexiCraft.Services.Practice.Shared.Events.IntegrationEvents;

public record PracticeCompletedIntegrationEvent(
    string UserId,
    string TaskId,
    int TotalItems,
    int CorrectCount,
    int WrongCount,
    long DurationSeconds,
    DateTime CompletedAt
) : IntegrationEvent;

public record WordMistakeOccurredIntegrationEvent(
    string UserId,
    string WordId,
    string MistakeType,
    string UserInput,
    string CorrectAnswer,
    DateTime OccurredAt
) : IntegrationEvent;