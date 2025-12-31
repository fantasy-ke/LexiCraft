using BuildingBlocks.EntityFrameworkCore;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Identity.Models.Enum;
using LexiCraft.Services.Identity.Shared;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Services.Identity.Shared.Data;

/// <summary>
///   Data seeder for IdentityDbContext
/// </summary>
public class IdentityDbDataSeeder
    : IDataSeeder<IdentityDbContext>
{   
    public async Task SeedAsync(IdentityDbContext context)
    {
        await SeedUsers(context);
    }

    private async Task SeedUsers(IdentityDbContext context)
    {
        if (context.Users.FirstOrDefault(b=>b.UserAccount == "admin") == null)
        {
            var adduser = new User("admin", "one@fatnasyke.fun");
            adduser.SetPassword("bb123456");
            adduser.Avatar = "🦜";
            adduser.Roles.Add(PermissionConstant.Admin);
            adduser.UpdateLastLogin();
            adduser.UpdateSource(SourceEnum.Register);
            adduser.CreateById = Guid.Empty;
            adduser.CreateByName = "admin";
            await context.Users.AddAsync(adduser);
            await context.SaveChangesAsync(); // 保存用户以获取ID
            
            // 重新加载用户以确保获取到数据库生成的ID
            var addedUser = await context.Users.FirstOrDefaultAsync(u => u.UserAccount == "admin");
            if (addedUser != null)
            {
                // 为新创建的用户添加权限
                await SeedUserPermissions(context, addedUser);
            }
        }
        
        await context.SaveChangesAsync();
    }
    
    private async Task SeedUserPermissions(IdentityDbContext context, User user)
    {
        // 为管理员用户添加默认权限
        if (user.Roles.Contains(PermissionConstant.Admin))
        {
            var defaultPermissions = PermissionConstant.DefaultUserPermissions.Permissions;
            foreach (var permissionName in defaultPermissions)
            {
                if (context.UserPermissions.Any(up => up.UserId == user.Id && up.PermissionName == permissionName))
                    continue;
                var userPermission = new UserPermission(user.Id, permissionName)
                {
                    CreateById = Guid.Empty,
                    CreateByName = "admin"
                };
                await context.UserPermissions.AddAsync(userPermission);
            }
        }
    }
}
