using BuildingBlocks.MassTransit.Abstractions;
using BuildingBlocks.MassTransit.EventSourcing.Abstractions;
using BuildingBlocks.Mediator;

namespace LexiCraft.Services.Practice.Shared.Events.IntegrationEvents;

public record PracticeCompletedIntegrationEvent(
    Guid UserId,
    string TaskId,
    int TotalItems,
    int CorrectCount,
    int WrongCount,
    long DurationSeconds,
    DateTime CompletedAt
) : IntegrationEvent, IEventSourced
{
    public string GetStreamId() => $"practice:task:{TaskId}";
}

public record WordMistakeOccurredIntegrationEvent(
    Guid UserId,
    string WordId,
    string MistakeType,
    string UserInput,
    string CorrectAnswer,
    DateTime OccurredAt
) : IntegrationEvent, IEventSourced
{
    public string GetStreamId() => $"practice:word_mistake:{UserId}:{WordId}";
}