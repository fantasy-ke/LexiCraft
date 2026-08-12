using BuildingBlocks.Authentication.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Authentication;

public sealed class AuthorizationPolicyProvider(
    IOptions<Microsoft.AspNetCore.Authorization.AuthorizationOptions> authorizationOptions,
    IPermissionDefinitionManager permissionDefinitionManager) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _defaultProvider = new(authorizationOptions);

    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var configuredPolicy = await _defaultProvider.GetPolicyAsync(policyName);
        if (configuredPolicy != null)
            return AddSessionValidation(configuredPolicy);

        var permissionNames = policyName
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (permissionNames.Length == 0 ||
            permissionNames.Any(permission => !permissionDefinitionManager.TryGetPermission(permission, out _)))
            return null;

        var defaultPolicy = await _defaultProvider.GetDefaultPolicyAsync();
        return new AuthorizationPolicyBuilder(defaultPolicy)
            .AddRequirements(new AuthorizeRequirement(permissionNames))
            .Build();
    }

    public async Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return AddSessionValidation(await _defaultProvider.GetDefaultPolicyAsync());
    }

    public async Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        var policy = await _defaultProvider.GetFallbackPolicyAsync();
        return policy == null ? null : AddSessionValidation(policy);
    }

    private static AuthorizationPolicy AddSessionValidation(AuthorizationPolicy policy)
    {
        return policy.Requirements.OfType<AuthorizeRequirement>().Any()
            ? policy
            : new AuthorizationPolicyBuilder(policy)
                .AddRequirements(new AuthorizeRequirement())
                .Build();
    }
}