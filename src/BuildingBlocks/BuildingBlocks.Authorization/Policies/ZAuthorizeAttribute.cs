using BuildingBlocks.Extensions.System;
using Microsoft.AspNetCore.Authorization;

namespace BuildingBlocks.Authentication.Policies;

/// <summary>
///     声明端点必须同时满足的一个或多个已注册权限。
/// </summary>
/// <remarks>
///     权限名称以逗号拼接为动态策略名，并使用 <see cref="StringComparer.Ordinal"/> 精确匹配。
///     权限树中的父节点只用于分组，声明父权限不会隐式满足任何子权限。重复应用特性时遵循 ASP.NET Core
///     授权策略的合并规则；无参数时仅要求身份认证和当前会话有效。
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class ZAuthorizeAttribute : AuthorizeAttribute
{
    /// <summary>
    ///     创建权限授权特性。
    /// </summary>
    /// <param name="permissions">必须全部满足的已注册权限完整名称；空数组表示只验证当前会话。</param>
    public ZAuthorizeAttribute(params string[] permissions)
    {
        Permissions = permissions;
        Policy = permissions.Length > 0 ? permissions.JoinAsString(",") : null;
    }

    /// <summary>
    ///     获取或设置本特性要求的权限完整名称。
    /// </summary>
    public string[] Permissions { get; set; }
}
