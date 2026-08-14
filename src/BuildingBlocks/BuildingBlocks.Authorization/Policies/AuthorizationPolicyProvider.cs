using BuildingBlocks.Authentication.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Authentication.Policies;

/// <summary>
///     将逗号分隔且已注册的权限名称解析为动态授权策略，并为默认策略补充当前会话验证。
/// </summary>
internal sealed class AuthorizationPolicyProvider(
    IOptions<Microsoft.AspNetCore.Authorization.AuthorizationOptions> authorizationOptions,
    IPermissionDefinitionManager permissionDefinitionManager) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _defaultProvider = new(authorizationOptions);

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return AddSessionValidation(await _defaultProvider.GetDefaultPolicyAsync());
    }

    /// <inheritdoc />
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
