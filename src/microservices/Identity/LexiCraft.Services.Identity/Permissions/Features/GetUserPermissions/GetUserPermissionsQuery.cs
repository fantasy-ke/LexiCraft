using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Mediator;
using LexiCraft.Services.Identity.Shared.Contracts;

namespace LexiCraft.Services.Identity.Permissions.Features.GetUserPermissions;

public record GetUserPermissionsQuery(Guid UserId) : IQuery<GetUserPermissionsResult>;

public record GetUserPermissionsResult(
    Guid UserId,
    List<string> Permissions
);

public class GetUserPermissionsQueryHandler(
    IUserPermissionRepository userPermissionRepository,
    IPermissionCache permissionCache)
    : IQueryHandler<GetUserPermissionsQuery, GetUserPermissionsResult>
{
    public async Task<GetUserPermissionsResult> Handle(GetUserPermissionsQuery query,
        CancellationToken cancellationToken)
    {
        // 先查询缓存
        var cachedPermissions = await permissionCache.GetUserPermissionsAsync(query.UserId);
        if (cachedPermissions != null)
            return new GetUserPermissionsResult(
                query.UserId,
                cachedPermissions.ToList()
            );

        // 缓存未命中，查询数据库
        var permissions = await userPermissionRepository.GetUserPermissionsAsync(query.UserId);

        // 回写到缓存
        if (permissions.Count > 0)
            await permissionCache.SetUserPermissionsAsync(
                query.UserId,
                permissions.ToHashSet()
            );

        return new GetUserPermissionsResult(
            query.UserId,
            permissions
        );
    }
}