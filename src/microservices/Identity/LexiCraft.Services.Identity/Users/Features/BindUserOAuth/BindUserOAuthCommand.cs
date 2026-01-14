using BuildingBlocks.Domain;
using BuildingBlocks.Mediator;
using LexiCraft.Services.Identity.Identity.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Services.Identity.Users.Features.BindUserOAuth;

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
    IRepository<UserOAuth> userOAuthRepository)
    : ICommandHandler<BindUserOAuthCommand, UserOAuth>
{
    public async Task<UserOAuth> Handle(BindUserOAuthCommand command, CancellationToken cancellationToken)
    {
        // 检查是否已经存在绑定
        var existing = await userOAuthRepository.QueryNoTracking()
            .FirstOrDefaultAsync(x => x.Provider == command.Provider && x.ProviderUserId == command.ProviderUserId, cancellationToken);
            
        if (existing != null)
        {
            return existing; 
        }

        var userOAuth = new UserOAuth
        {
            UserId = command.UserId,
            Provider = command.Provider,
            ProviderUserId = command.ProviderUserId,
            AccessToken = string.Empty,
            RefreshToken = string.Empty
        };

        await userOAuthRepository.InsertAsync(userOAuth);
        await userOAuthRepository.SaveChangesAsync();

        return userOAuth;
    }
}
