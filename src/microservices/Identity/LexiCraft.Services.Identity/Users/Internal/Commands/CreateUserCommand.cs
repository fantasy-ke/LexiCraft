using BuildingBlocks.Mediator;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Identity.Models.Enum;
using LexiCraft.Services.Identity.Shared.Contracts;
using LexiCraft.Shared.Permissions;

namespace LexiCraft.Services.Identity.Users.Internal.Commands;

/// <summary>
/// 创建用户命令
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
    IUserRepository userRepository)
    : ICommandHandler<CreateUserCommand, User>
{
    public async Task<User> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        // 检查用户账号是否已存在（双重检查，虽然上层可能查过）
        var any = await userRepository.AnyAsync(p => p.UserAccount == command.UserAccount);
        if (any)
        {
            throw new InvalidOperationException("当前用户名已存在");
        }

        // 创建用户
        var user = new User(command.UserAccount, command.Email, command.Source);
        if (!string.IsNullOrEmpty(command.Password))
        {
            user.SetPassword(command.Password);
        }
        user.UpdateAvatar(command.Avatar ?? "🦜");
        user.AddRole(PermissionConstant.User);
        user.UpdateLastLoginTime();
        
        // 为用户分配默认权限
        var defaultPermissions = PermissionConstant.DefaultUserPermissions.Permissions;
        foreach (var permission in defaultPermissions)
        {
            user.AddPermission(permission);
        }

        var afterUser = await userRepository.InsertAsync(user);

        await userRepository.SaveChangesAsync();

        return afterUser;
    }
}