using Microsoft.AspNetCore.Authorization;

namespace BuildingBlocks.Authentication.Policies;

/// <summary>
///     表示一次授权检查要求满足的权限集合；空集合仅验证当前会话。
/// </summary>
/// <param name="authorizeName">需要验证的权限名称。</param>
public sealed class AuthorizeRequirement(params string[] authorizeName) : IAuthorizationRequirement
{
    /// <summary>
    ///     获取已去除空值和重复项的权限名称。
    /// </summary>
    public string[] AuthorizeName { get; } = authorizeName
        .Where(name => !string.IsNullOrWhiteSpace(name))
        .Distinct(StringComparer.Ordinal)
        .ToArray();
}
