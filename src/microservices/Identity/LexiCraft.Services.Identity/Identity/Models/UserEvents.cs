using BuildingBlocks.MassTransit.EventSourcing.Abstractions;
using BuildingBlocks.Mediator;
using LexiCraft.Services.Identity.Identity.Models.Enum;
using LexiCraft.Shared.Models;

namespace LexiCraft.Services.Identity.Identity.Models;

public record UserCreatedEvent(UserId UserId, string Username, string UserAccount, string Email, SourceEnum Source)
    : IDomainEvent, IEventSourced
{
    public string GetStreamId() => $"identity:user:{UserId.Value}";
}

public record UserLoginSuccessEvent(UserId UserId, DateTime LoginTime) : IDomainEvent, IEventSourced
{
    public string GetStreamId() => $"identity:user:{UserId.Value}";
}

public record UserLoginFailedEvent(UserId UserId, int AccessFailedCount, DateTimeOffset? LockoutEnd) : IDomainEvent, IEventSourced
{
    public string GetStreamId() => $"identity:user:{UserId.Value}";
}
