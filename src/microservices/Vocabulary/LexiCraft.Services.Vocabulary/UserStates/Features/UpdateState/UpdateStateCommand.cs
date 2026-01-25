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

        if (state == null) state = new UserWordState(command.UserId, command.WordId);

        state.UpdateState(command.State);

        if (command.IsInWordBook.HasValue)
            state.ToggleWordBook(command.IsInWordBook.Value);

        if (command.MasteryScore.HasValue)
            state.UpdateScore(command.MasteryScore.Value);

        // Ensure reviewed time is updated (UpdateState and UpdateScore do it, but to be safe/consistent with original intent)
        state.MarkReviewed();

        await repository.AddOrUpdateAsync(state);

        return true;
    }
}