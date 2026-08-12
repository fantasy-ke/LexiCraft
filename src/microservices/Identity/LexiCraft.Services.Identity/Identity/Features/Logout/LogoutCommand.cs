using System.Globalization;
using BuildingBlocks.Authentication;
using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Mediator;
using FluentValidation;
using LexiCraft.Shared.Models;
using Microsoft.Extensions.Logging;

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
    IAuthorizationCache authorizationCache,
    IAuthorizationSynchronization authorizationSynchronization,
    ILogger<LogoutCommandHandler> logger)
    : ICommandHandler<LogoutCommand, bool>
{
    public Task<bool> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        return authorizationSynchronization.ExecuteAsync(
            $"session:{command.UserId.Value:N}",
            async token =>
            {
                var sessionKey = string.Format(
                    CultureInfo.InvariantCulture,
                    UserInfoConst.RedisAuthorizationSessionKey,
                    command.UserId.Value.ToString("N"));

                var oldSession = await authorizationCache.GetAsync<AccessTokenCacheEntry>(sessionKey, token);

                // 先移除当前会话。此后旧刷新令牌即使残留，也无法通过会话指针校验。
                await authorizationCache.RemoveAsync(sessionKey, token);
                if (!string.IsNullOrEmpty(oldSession?.RefreshTokenHash))
                    await TryRemoveRefreshTokenAsync(oldSession.RefreshTokenHash, token);

                return true;
            },
            cancellationToken);
    }

    private async Task TryRemoveRefreshTokenAsync(string refreshTokenHash, CancellationToken cancellationToken)
    {
        try
        {
            var refreshTokenKey = string.Format(
                CultureInfo.InvariantCulture,
                UserInfoConst.RedisAuthorizationRefreshTokenKey,
                refreshTokenHash);
            await authorizationCache.RemoveAsync(refreshTokenKey, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to remove the refresh token during logout");
        }
    }
}
