using BuildingBlocks.Authentication;
using BuildingBlocks.Mediator;
using BuildingBlocks.Caching.Abstractions;
using FluentValidation;

namespace LexiCraft.Services.Identity.Identity.Features.Logout;

public record LogoutCommand(Guid UserId) : ICommand<bool>;

public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("用户ID不能为空");
    }
}

public class LogoutCommandHandler(
    ICacheService cacheService) 
    : ICommandHandler<LogoutCommand, bool>
{
    public async Task<bool> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var cacheKey = string.Format(UserInfoConst.RedisTokenKey, command.UserId.ToString("N"));
        await cacheService.RemoveAsync(cacheKey, cancellationToken: cancellationToken);
        return true;
    }
}