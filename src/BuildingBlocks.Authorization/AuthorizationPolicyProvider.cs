using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace BuildingBlocks.Authentication;

public class AuthorizationPolicyProvider(IAuthenticationSchemeProvider authenticationSchemeProvider):IAuthorizationPolicyProvider
{
    /// <summary>
    /// 返回给定名称的授权策略
    /// </summary>
    /// <param name="policyName"></param>
    /// <returns></returns>
    public async Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
    {
        var policy = new AuthorizationPolicyBuilder();

        await SetScheme(policy);

        SetPolicy(policy, policyName);

        return policy.Build();
    }

    /// <summary>
    /// 会返回默认授权策略（在未指定策略的情况下用于 [Authorize] 属性的策略）
    /// </summary>
    /// <returns></returns>
    public async Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        var policy = new AuthorizationPolicyBuilder();

        await SetScheme(policy);

        SetPolicy(policy);

        return policy.Build();
    }

    /// <summary>
    /// 返回回退授权策略（在未指定策略时由授权中间件使用的策略
    /// </summary>
    /// <returns></returns>
    public async Task<AuthorizationPolicy> GetFallbackPolicyAsync()
    {
        var policy = new AuthorizationPolicyBuilder();

        await SetScheme(policy);

        SetPolicy(policy);

        return policy.Build();
    }

    private void SetPolicy(AuthorizationPolicyBuilder policyBuilder, string policyName = null)
    {
        if (!string.IsNullOrEmpty(policyName))
        {
            var authorizations = policyName.Split(',');
            if (authorizations.Any())
            {
                policyBuilder.AddRequirements(new AuthorizeRequirement(authorizations));
            }
        }
        else
        {
            policyBuilder.AddRequirements(new AuthorizeRequirement());
        }
    }

    private async Task SetScheme(AuthorizationPolicyBuilder policyBuilder,string policyName = null)
    {
        var schemes = await authenticationSchemeProvider.GetAllSchemesAsync();

        foreach (var scheme in schemes)
        {
            policyBuilder.AuthenticationSchemes.Add(scheme.Name);
        }
    }
}