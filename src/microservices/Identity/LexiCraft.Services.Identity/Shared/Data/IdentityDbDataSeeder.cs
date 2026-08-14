using BuildingBlocks.EntityFrameworkCore.Abstractions;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Shared.Permissions;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Services.Identity.Shared.Data;

/// <summary>
///     Data seeder for IdentityDbContext
/// </summary>
public class IdentityDbDataSeeder
    : IDataSeeder<IdentityDbContext>
{
    public async Task SeedAsync(IdentityDbContext context, CancellationToken cancellationToken = default)
    {
        await SeedUsers(context, cancellationToken);
    }

    private async Task SeedUsers(IdentityDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Users.FirstOrDefaultAsync(b => b.UserAccount == "admin", cancellationToken) == null)
        {
            var adduser = new User("admin", "one@fatnasyke.fun");
            adduser.SetPassword("bb123456");
            adduser.UpdateAvatar("🦜");
            adduser.AddRole(PermissionConstant.Admin);
            adduser.UpdateLastLoginTime();
            adduser.CreateById = Guid.Empty;
            adduser.CreateByName = "admin";
            await context.Users.AddAsync(adduser, cancellationToken);
            await context.SaveChangesAsync(cancellationToken); // 保存用户以获取ID

            // 重新加载用户以确保获取到数据库生成的ID
            var addedUser = await context.Users.FirstOrDefaultAsync(
                u => u.UserAccount == "admin",
                cancellationToken);
            if (addedUser != null)
                // 为新创建的用户添加权限
                await SeedUserPermissions(context, addedUser, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedUserPermissions(
        IdentityDbContext context,
        User user,
        CancellationToken cancellationToken)
    {
        // 为管理员用户添加默认权限
        if (user.Roles.Contains(PermissionConstant.Admin))
        {
            var defaultPermissions = PermissionConstant.DefaultUserPermissions.Permissions;
            var existingPermissions = await context.UserPermissions
                .Where(permission =>
                    permission.UserId == user.Id && defaultPermissions.Contains(permission.PermissionName))
                .Select(permission => permission.PermissionName)
                .ToHashSetAsync(cancellationToken);

            foreach (var permissionName in defaultPermissions)
            {
                if (!existingPermissions.Add(permissionName)) continue;

                var userPermission = new UserPermission(user.Id, permissionName)
                {
                    CreateById = Guid.Empty,
                    CreateByName = "admin"
                };
                await context.UserPermissions.AddAsync(userPermission, cancellationToken);
            }
        }
    }
}