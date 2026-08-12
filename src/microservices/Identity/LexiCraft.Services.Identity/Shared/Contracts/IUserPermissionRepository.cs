using BuildingBlocks.Domain;
using LexiCraft.Services.Identity.Identity.Models;
using LexiCraft.Shared.Models;

namespace LexiCraft.Services.Identity.Shared.Contracts;

public interface IUserPermissionRepository : IQueryRepository<UserPermission>
{
    Task<List<string>> GetUserPermissionsAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}
