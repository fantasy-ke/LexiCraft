using System.Security.Claims;
using BuildingBlocks.Authentication.Contract;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Authentication;

/// <summary>
///     从当前 HTTP 请求的 <see cref="ClaimsPrincipal"/> 提取用户标识、账号和角色。
/// </summary>
public sealed class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    /// <summary>获取当前用户标识；缺少有效标识 Claim 时返回 <see cref="Guid.Empty"/>。</summary>
    public Guid UserId => FindGuidClaim(ClaimTypes.Sid, "sid", UserInfoConst.UserId);

    /// <summary>获取当前用户名；Claim 不存在时返回空字符串。</summary>
    public string UserName => FindClaimValue(UserInfoConst.UserName) ?? string.Empty;

    /// <summary>获取当前用户账号；Claim 不存在时返回空字符串。</summary>
    public string UserAccount => FindClaimValue(UserInfoConst.UserAccount) ?? string.Empty;

    /// <summary>获取当前请求是否已经通过身份认证。</summary>
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    /// <summary>获取去重后的角色 Claim，并兼容逗号分隔的角色值。</summary>
    public string[] Roles => Principal?.Claims
        .Where(claim => claim.Type is ClaimTypes.Role or "role")
        .SelectMany(claim => claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray() ?? [];

    private Guid FindGuidClaim(params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = FindClaimValue(claimType);
            if (Guid.TryParse(value, out var result))
                return result;
        }

        return Guid.Empty;
    }

    private string? FindClaimValue(string claimType)
    {
        return Principal?.FindFirst(claim => string.Equals(claim.Type, claimType, StringComparison.Ordinal))?.Value;
    }
}
