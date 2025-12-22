using BuildingBlocks.EntityFrameworkCore;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Identity.Models.Enum;

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
            var user = new User("admin", "one@fatnasyke.fun");
            user.SetPassword("bb123456");
            user.Avatar = "🦜";
            user.Roles.Add(RoleConstant.Admin);
            user.UpdateLastLogin();
            user.UpdateSource(SourceEnum.Register);
            user.CreateAt  = DateTime.Now;
            await context.Users.AddAsync(user);
        }
        
    }
}
