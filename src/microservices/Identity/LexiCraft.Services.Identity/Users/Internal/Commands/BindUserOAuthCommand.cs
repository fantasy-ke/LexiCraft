using BuildingBlocks.Domain;
using BuildingBlocks.Mediator;
using LexiCraft.Services.Identity.Identity.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Services.Identity.Users.Internal.Commands;

/// <summary>
/// 绑定用户OAuth信息命令
/// </summary>
/// <param name="UserId">用户ID</param>
/// <param name="Provider">提供者</param>
/// <param name="ProviderUserId">提供者用户ID</param>
public record BindUserOAuthCommand(
    Guid UserId,
    string Provider,
    string ProviderUserId) : ICommand<UserOAuth>;

public class BindUserOAuthCommandHandler(
    IQueryRepository<UserOAuth> userOAuthQueryRepository,
    IRepository<User> userRepository)
    : ICommandHandler<BindUserOAuthCommand, UserOAuth>
{
    public async Task<UserOAuth> Handle(BindUserOAuthCommand command, CancellationToken cancellationToken)
    {
        // 检查是否已经存在绑定
        var existing = await userOAuthQueryRepository.QueryNoTracking()
            .FirstOrDefaultAsync(x => x.Provider == command.Provider && x.ProviderUserId == command.ProviderUserId, cancellationToken);
            
        if (existing != null)
        {
            return existing; 
        }

        var user = await userRepository.GetAsync(x => x.Id == command.UserId)
            ?? throw new InvalidOperationException("User not found");

        user.BindOAuth(
            command.Provider,
            command.ProviderUserId,
            string.Empty,
            DateTimeOffset.MinValue,
            string.Empty
        );

        await userRepository.UpdateAsync(user);
        await userRepository.SaveChangesAsync();

        return user.OAuths.First(x => x.Provider == command.Provider);
    }
}