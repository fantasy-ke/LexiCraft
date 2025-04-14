using LexiCraft.Infrastructure.Contract;
using LexiCraft.Infrastructure.Extensions;

namespace LexiCraft.Infrastructure.Authorization;

public class PermissionCheck(IUserContext userContext) : IPermissionCheck
{
    
    public Task<bool> IsGranted(string authorizationNames)
    {
        if (userContext.UserAllPermissions.Any() && authorizationNames.IsNullEmpty())
        {
            return Task.FromResult(userContext.UserAllPermissions.Contains(authorizationNames));
        }
        return Task.FromResult(false);
    }
}