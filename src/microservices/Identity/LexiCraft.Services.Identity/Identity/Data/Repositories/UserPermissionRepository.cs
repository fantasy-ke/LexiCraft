using BuildingBlocks.EntityFrameworkCore;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Shared.Contracts;
using LexiCraft.Services.Identity.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Services.Identity.Identity.Data.Repositories;

public class UserPermissionRepository(IdentityDbContext dbContext)
    : QueryRepository<IdentityDbContext, UserPermission>(dbContext), IUserPermissionRepository
{
    public async Task<List<string>> GetUserPermissionsAsync(Guid userId)
    {
        return await QueryNoTracking()
            .Where(up => up.UserId == userId)
            .Select(up => up.PermissionName)
            .ToListAsync();
    }
}