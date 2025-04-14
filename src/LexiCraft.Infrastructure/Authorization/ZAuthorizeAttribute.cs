using LexiCraft.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;

namespace LexiCraft.Infrastructure.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ZAuthorizeAttribute: AuthorizeAttribute
{
    public string[] AuthorizeName { get; }
    public ZAuthorizeAttribute(params string[] policyNames)
    {
        AuthorizeName = policyNames;
        Policy = AuthorizeName.Length > 0 ? AuthorizeName.JoinAsString(","): null;
    }
}