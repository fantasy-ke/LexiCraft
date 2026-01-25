using Microsoft.AspNetCore.Authorization;

namespace BuildingBlocks.Authentication;

public class AuthorizeRequirement : IAuthorizationRequirement
{
    public AuthorizeRequirement(params string[] authorizeName)
    {
        AuthorizeName = authorizeName;
    }

    public virtual string[] AuthorizeName { get; private set; }
}