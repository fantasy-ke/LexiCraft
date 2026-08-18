using BuildingBlocks.Persistence.Abstractions.Repositories;
using Fantasy.Services.Identity.Identity.Models;
using Fantasy.Shared.Models;

namespace Fantasy.Services.Identity.Shared.Contracts;

public interface IUserPermissionRepository : IQueryRepository<UserPermission>
{
    Task<List<string>> GetUserPermissionsAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}
