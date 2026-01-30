using BuildingBlocks.Authentication;
using BuildingBlocks.Caching.Abstractions;
using BuildingBlocks.Mediator;
using FluentValidation;
using LexiCraft.Shared.Models;

namespace LexiCraft.Services.Identity.Identity.Features.Logout;

public record LogoutCommand(UserId UserId) : ICommand<bool>;

public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.UserId)
            .Must(x => x.Value != Guid.Empty).WithMessage("用户ID不能为空");
    }
}

public class LogoutCommandHandler(
    ICacheService cacheService)
    : ICommandHandler<LogoutCommand, bool>
{
    public async Task<bool> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var cacheKey = string.Format(UserInfoConst.RedisTokenKey, command.UserId.Value.ToString("N"));
        await cacheService.RemoveAsync(cacheKey, cancellationToken: cancellationToken);
        return true;
    }
}