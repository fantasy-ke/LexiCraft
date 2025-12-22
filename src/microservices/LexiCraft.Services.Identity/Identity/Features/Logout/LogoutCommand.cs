using BuildingBlocks.Authentication;
using BuildingBlocks.Mediator;
using BuildingBlocks.Redis;

namespace LexiCraft.Services.Identity.Identity.Features.Logout;

public record LogoutCommand(Guid UserId) : ICommand<bool>;

public class LogoutCommandHandler(
    ICacheManager redisManager) 
    : ICommandHandler<LogoutCommand, bool>
{
    public async Task<bool> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var cacheKey = string.Format(UserInfoConst.RedisTokenKey, command.UserId.ToString("N"));
        await redisManager.DelAsync(cacheKey);
        return true;
    }
}