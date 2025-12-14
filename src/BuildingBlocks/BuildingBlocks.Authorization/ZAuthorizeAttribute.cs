using BuildingBlocks.Extensions.System;
using Microsoft.AspNetCore.Authorization;

namespace BuildingBlocks.Authentication;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class ZAuthorizeAttribute : AuthorizeAttribute
{
    public string[] Permissions { get; set; }
    
    public ZAuthorizeAttribute(params string[] permissions)
    {
        Permissions = permissions;
        Policy = permissions.Length > 0 ? permissions.JoinAsString(",") : null;
    }
}