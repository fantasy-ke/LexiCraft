using BuildingBlocks.EntityFrameworkCore.Repositories;
using Fantasy.Services.Identity.Identity.Models;
using Fantasy.Services.Identity.Shared.Contracts;
using Fantasy.Services.Identity.Shared.Data;
using Fantasy.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Fantasy.Services.Identity.Identity.Data.Repositories;

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
