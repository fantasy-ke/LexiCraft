using BuildingBlocks.Mediator;
using FluentValidation;
using LexiCraft.Services.Vocabulary.Shared.Contracts;
using LexiCraft.Services.Vocabulary.UserStates.Models;
using LexiCraft.Services.Vocabulary.UserStates.Models.Enum;

namespace LexiCraft.Services.Vocabulary.UserStates.Features.UpdateState;

public record UpdateStateCommand(
    Guid UserId,
    long WordId,
    WordState State,
    bool? IsInWordBook,
    int? MasteryScore) : ICommand<bool>;

public class UpdateStateCommandValidator : AbstractValidator<UpdateStateCommand>
{
    public UpdateStateCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.WordId).GreaterThan(0);
        RuleFor(x => x.MasteryScore).InclusiveBetween(0, 100).When(x => x.MasteryScore.HasValue);
    }
}

public class UpdateStateCommandHandler(IUserWordStateRepository repository) 
    : ICommandHandler<UpdateStateCommand, bool>
{
    public async Task<bool> Handle(UpdateStateCommand command, CancellationToken cancellationToken)
    {
        var state = await repository.GetAsync(command.UserId, command.WordId);
        
        if (state == null)
        {
            state = new UserWordState(command.UserId, command.WordId);
        }

        state.State = command.State;
        if (command.IsInWordBook.HasValue)
            state.IsInWordBook = command.IsInWordBook.Value;
        if (command.MasteryScore.HasValue)
            state.MasteryScore = command.MasteryScore.Value;
        
        state.LastReviewedAt = DateTime.UtcNow;

        await repository.AddOrUpdateAsync(state);
        
        return true;
    }
}
