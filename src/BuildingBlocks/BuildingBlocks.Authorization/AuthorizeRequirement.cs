using Microsoft.AspNetCore.Authorization;

namespace BuildingBlocks.Authentication;

public sealed class AuthorizeRequirement(params string[] authorizeName) : IAuthorizationRequirement
{
    public string[] AuthorizeName { get; } = authorizeName
        .Where(name => !string.IsNullOrWhiteSpace(name))
        .Distinct(StringComparer.Ordinal)
        .ToArray();
}
