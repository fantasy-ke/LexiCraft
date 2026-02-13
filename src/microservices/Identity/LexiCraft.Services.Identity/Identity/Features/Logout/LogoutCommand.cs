using System.Globalization;
using BuildingBlocks.Authentication;
using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Caching.Abstractions;
using BuildingBlocks.Mediator;
using FluentValidation;
using LexiCraft.Services.Identity.Shared.Dtos;
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
        var cacheKey = string.Format(
            CultureInfo.InvariantCulture,
            UserInfoConst.RedisTokenKey,
            command.UserId.Value.ToString("N"));

        var oldToken = await cacheService.GetAsync<TokenResponse>(cacheKey, cancellationToken: cancellationToken);
        if (oldToken != null)
        {
            if (!string.IsNullOrEmpty(oldToken.RefreshToken))
            {
                var oldRefreshTokenKey = string.Format(
                    CultureInfo.InvariantCulture,
                    UserInfoConst.RedisRefreshTokenKey,
                    oldToken.RefreshToken);
                await cacheService.RemoveAsync(oldRefreshTokenKey, cancellationToken: cancellationToken);
            }

            await cacheService.RemoveAsync(cacheKey, cancellationToken: cancellationToken);
        }

        return true;
    }
}