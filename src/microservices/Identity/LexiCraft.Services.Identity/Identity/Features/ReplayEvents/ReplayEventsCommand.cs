using BuildingBlocks.MassTransit.EventSourcing.Abstractions;
using BuildingBlocks.Mediator;
using FluentValidation;

namespace LexiCraft.Services.Identity.Identity.Features.ReplayEvents;

public enum ReplayTarget
{
    IntegrationEvent, // 发送到消息总线
    DomainEvent       // 发送到本地 MediatR
}

public record ReplayEventsCommand(
    string StreamId, 
    long FromVersion = 0, 
    long? ToVersion = null,
    ReplayTarget Target = ReplayTarget.IntegrationEvent) : ICommand;

public class ReplayEventsCommandValidator : AbstractValidator<ReplayEventsCommand>
{
    public ReplayEventsCommandValidator()
    {
        RuleFor(x => x.StreamId).NotEmpty().WithMessage("事件流ID不能为空");
        RuleFor(x => x.FromVersion).GreaterThanOrEqualTo(0).WithMessage("起始版本号不能小于0");
        RuleFor(x => x.ToVersion)
            .GreaterThanOrEqualTo(x => x.FromVersion)
            .When(x => x.ToVersion.HasValue)
            .WithMessage("截止版本号不能小于起始版本号");
    }
}

public class ReplayEventsCommandHandler(
    IEventReplayer eventReplayer, 
    IDomainEventReplayer domainEventReplayer) : ICommandHandler<ReplayEventsCommand>
{
    public async Task Handle(ReplayEventsCommand command, CancellationToken cancellationToken)
    {
        if (command.Target == ReplayTarget.DomainEvent)
        {
            await domainEventReplayer.ReplayAsync(command.StreamId, command.FromVersion, command.ToVersion, cancellationToken);
        }
        else
        {
            await eventReplayer.ReplayAsync(command.StreamId, command.FromVersion, command.ToVersion, cancellationToken);
        }
    }
}
