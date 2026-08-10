using System.Security.Cryptography;
using BuildingBlocks.Mediator;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Identity.Models.Enum;
using LexiCraft.Services.Identity.Shared.Contracts;
using LexiCraft.Shared.Permissions;
using Microsoft.Extensions.Logging;

namespace LexiCraft.Services.Identity.Users.Internal.Commands;

/// <summary>
///     创建用户命令
/// </summary>
/// <param name="UserAccount">账号</param>
/// <param name="Email">邮箱</param>
/// <param name="Password">密码（可选，OAuth注册可能不需要）</param>
/// <param name="Source">来源</param>
/// <param name="Avatar">头像</param>
public record CreateUserCommand(
    string UserAccount,
    string Email,
    string? Password,
    SourceEnum Source,
    string? Avatar = "🦜") : ICommand<User>;

public class CreateUserCommandHandler(
    IUserRepository userRepository,
    ILogger<CreateUserCommandHandler> logger)
    : ICommandHandler<CreateUserCommand, User>
{
    public async Task<User> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("开始创建用户，账户: {UserAccount}, 来源: {Source}", command.UserAccount, command.Source);

        // 检查用户账号是否已存在（双重检查，虽然上层可能查过）
        var any = await userRepository.AnyAsync(p => p.UserAccount == command.UserAccount);
        if (any)
        {
            logger.LogWarning("用户账号已存在: {UserAccount}", command.UserAccount);
            throw new InvalidOperationException("当前用户名已存在");
        }

        // 创建用户
        var user = new User(command.UserAccount, command.Email, command.Source);
        var password = string.IsNullOrEmpty(command.Password)
            ? Convert.ToHexString(RandomNumberGenerator.GetBytes(32))
            : command.Password;
        user.SetPassword(password);
        user.UpdateAvatar(command.Avatar ?? "🦜");
        user.AddRole(PermissionConstant.User);
        user.UpdateLastLoginTime();

        // 为用户分配默认权限
        var defaultPermissions = PermissionConstant.DefaultUserPermissions.Permissions;
        await userRepository.InsertAsync(user);
        user.AddPermissions(PermissionConstant.DefaultUserPermissions.Permissions);

        logger.LogInformation("用户创建成功，ID: {UserId}, 分配了 {Count} 个默认权限", user.Id, defaultPermissions.Count());

        return user;
    }
}