using BuildingBlocks.Extensions.System;
using LexiCraft.Infrastructure.Contract;

namespace LexiCraft.Infrastructure.Authorization;

public class PermissionCheck(IUserContext userContext) : IPermissionCheck
{
    
    public Task<bool> IsGranted(string authorizationNames)
    {
        if (userContext.UserAllPermissions == null || userContext.UserAllPermissions?.Length == 0)
            return Task.FromResult(false);
        if (authorizationNames.IsNullWhiteSpace())
            return Task.FromResult(true);
        var authArr = authorizationNames.Split(',');
        var intersectArr = userContext.UserAllPermissions?.Intersect(authArr);
        return Task.FromResult(intersectArr?.Any() ?? false);
    }
}