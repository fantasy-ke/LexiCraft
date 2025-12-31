using BuildingBlocks.EntityFrameworkCore;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Services.Identity.Shared.Contracts;
using LexiCraft.Services.Identity.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Services.Identity.Identity.Data.Repositories;

public class UserPermissionRepository(IdentityDbContext dbContext)
    : Repository<IdentityDbContext, UserPermission>(dbContext), IUserPermissionRepository
{
    public async Task<List<string>> GetUserPermissionsAsync(Guid userId)
    {
        return await QueryNoTracking<UserPermission>()
            .Where(up => up.UserId == userId)
            .Select(up => up.PermissionName)
            .ToListAsync();
    }

    public async Task AddUserPermissionAsync(Guid userId, string permissionName)
    {
        var existingPermission = await QueryNoTracking<UserPermission>()
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionName == permissionName);

        if (existingPermission == null)
        {
            var userPermission = new UserPermission(userId, permissionName);
            await InsertAsync(userPermission);
            await SaveChangesAsync();
        }
    }

    public async Task AddUserPermissionsAsync(Guid userId, IEnumerable<string> permissionNames)
    {
        var existingPermissions = await QueryNoTracking<UserPermission>()
            .Where(up => up.UserId == userId && permissionNames.Contains(up.PermissionName))
            .Select(up => up.PermissionName)
            .ToListAsync();

        var permissionsToAdd = permissionNames
            .Except(existingPermissions)
            .Select(permissionName => new UserPermission(userId, permissionName))
            .ToList();

        if (permissionsToAdd.Any())
        {
            await InsertAsync(permissionsToAdd);
            await SaveChangesAsync();
        }
    }

    public async Task RemoveUserPermissionAsync(Guid userId, string permissionName)
    {
        await DeleteAsync(up => up.UserId == userId && up.PermissionName == permissionName);
        await SaveChangesAsync();
    }

    public async Task RemoveAllUserPermissionsAsync(Guid userId)
    {
        await DeleteAsync(up => up.UserId == userId);
        await SaveChangesAsync();
    }
}