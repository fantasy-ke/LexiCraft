using BuildingBlocks.Domain;
using BuildingBlocks.Mediator;
using LexiCraft.Services.Identity.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LexiCraft.Shared.Models;

namespace LexiCraft.Services.Identity.Users.Internal.Commands;

/// <summary>
///     绑定用户OAuth信息命令
/// </summary>
/// <param name="UserId">用户ID</param>
/// <param name="Provider">提供者</param>
/// <param name="ProviderUserId">提供者用户ID</param>
public record BindUserOAuthCommand(
    UserId UserId,
    string Provider,
    string ProviderUserId,
    User? TrackedUser = null) : ICommand<UserOAuth>;

public class BindUserOAuthCommandHandler(
    IQueryRepository<UserOAuth> userOAuthQueryRepository,
    IRepository<User> userRepository,
    ILogger<BindUserOAuthCommandHandler> logger)
    : ICommandHandler<BindUserOAuthCommand, UserOAuth>
{
    public async Task<UserOAuth> Handle(BindUserOAuthCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("开始处理OAuth绑定，UserId: {UserId}, Provider: {Provider}", command.UserId, command.Provider);

        // 检查是否已经存在绑定
        var existing = await userOAuthQueryRepository.QueryNoTracking()
            .FirstOrDefaultAsync(x => x.Provider == command.Provider && x.ProviderUserId == command.ProviderUserId,
                cancellationToken);

        if (existing != null)
        {
            logger.LogInformation("OAuth绑定已存在，UserId: {UserId}, Provider: {Provider}", existing.UserId, command.Provider);
            return existing;
        }

        User user;
        if (command.TrackedUser != null)
        {
            user = command.TrackedUser;
            // 确保 userId 匹配
            if (user.Id != command.UserId)
            {
                logger.LogError("用户ID不匹配，TrackedUser.Id: {TrackedId}, Command.UserId: {CommandId}", user.Id, command.UserId);
                throw new InvalidOperationException("TrackedUser ID does not match Command UserId");
            }
        }
        else
        {
            // 加载聚合根及其关联集合（OAuths）
            user = await userRepository.Query()
                       .Include(u => u.OAuths)
                       .FirstAsync(x => x.Id == command.UserId, cancellationToken);
        }

        user.BindOAuth(
            command.Provider,
            command.ProviderUserId,
            string.Empty,
            DateTimeOffset.MinValue,
            string.Empty
        );

        // [重点] 移除冗余的 UpdateAsync。实体已被跟踪，在主 Handler 的 SaveChangesAsync 中会自动提交。
        
        var oauth = user.OAuths.First(x => x.Provider == command.Provider);
        logger.LogInformation("OAuth绑定成功，UserId: {UserId}, Provider: {Provider}", command.UserId, command.Provider);

        return oauth;
    }
}