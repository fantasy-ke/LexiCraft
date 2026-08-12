using BuildingBlocks.EntityFrameworkCore;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Shared.Contracts;
using LexiCraft.Services.Identity.Shared.Data;
using LexiCraft.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Services.Identity.Identity.Data.Repositories;

public sealed class UserPermissionRepository(IdentityDbContext dbContext)
    : QueryRepository<IdentityDbContext, UserPermission>(dbContext), IUserPermissionRepository
{
    public Task<List<string>> GetUserPermissionsAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        return QueryNoTracking()
            .Where(permission => permission.UserId == userId)
            .Select(permission => permission.PermissionName)
            .ToListAsync(cancellationToken);
    }
}
